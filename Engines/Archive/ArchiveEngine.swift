import Foundation
import Darwin
#if canImport(CArchive)
import CArchive
#endif

struct ArchiveEngine {
    static let byteLimit: Int64 = 4 * 1024 * 1024 * 1024
    static func safePath(_ path: String) -> Bool {
        !path.isEmpty && !path.hasPrefix("/") && !path.contains("\\") && !path.contains(":") &&
        !path.split(separator: "/").contains("..") && !path.contains("\0")
    }
    private static func writer(_ output: URL, format: String) throws -> OpaquePointer {
        guard let writer = archive_write_new() else { throw AppError.engine("The archive engine could not start.") }
        let status: Int32
        switch format {
        case "zip":
            status = archive_write_set_format_zip(writer)
            archive_write_set_format_option(writer, "zip", "compression", "store")
        case "tar": status = archive_write_set_format_pax_restricted(writer)
        case "gz", "gzip":
            _ = archive_write_add_filter_gzip(writer)
            status = archive_write_set_format_pax_restricted(writer)
        default: archive_write_free(writer); throw AppError.unsupported("Choose ZIP, TAR, or GZIP.")
        }
        guard status == ARCHIVE_OK, archive_write_open_filename(writer, output.path) == ARCHIVE_OK else {
            archive_write_free(writer); throw AppError.engine("The archive could not be created.")
        }
        return writer
    }
    static func repack(_ input: URL, to output: URL, format: String) throws {
        guard let reader = archive_read_new() else { throw AppError.engine("The archive could not be opened.") }
        defer { archive_read_free(reader) }
        archive_read_support_filter_all(reader); archive_read_support_format_all(reader)
        if ["gz", "gzip"].contains(input.pathExtension.lowercased()) { archive_read_support_format_raw(reader) }
        guard archive_read_open_filename(reader, input.path, 65536) == ARCHIVE_OK else { throw AppError.input("This archive is damaged, encrypted, or unsupported.") }
        let outputArchive = try writer(output, format: format)
        defer { archive_write_free(outputArchive) }
        var entry: OpaquePointer?
        var total: Int64 = 0
        var count = 0
        var buffer = [UInt8](repeating: 0, count: 65536)
        while true {
            try Task.checkCancellation()
            let status = archive_read_next_header(reader, &entry)
            if status == ARCHIVE_EOF { break }
            guard status == ARCHIVE_OK, let entry, let rawName = archive_entry_pathname(entry) else { throw AppError.input("An archive entry could not be read.") }
            if archive_format(reader) == ARCHIVE_FORMAT_RAW, archive_filter_code(reader, 0) != ARCHIVE_FILTER_GZIP { throw AppError.input("This is not a valid GZIP archive.") }
            let name = String(cString: rawName)
            count += 1
            let type = archive_entry_filetype(entry)
            guard safePath(name), count <= 100000, archive_entry_symlink(entry) == nil, archive_entry_hardlink(entry) == nil,
                  type == mode_t(S_IFREG) || type == mode_t(S_IFDIR) else {
                throw AppError.input("This archive contains unsafe paths, links, or too many entries.")
            }
            guard archive_entry_size(entry) <= byteLimit else { throw AppError.input("An archive entry exceeds the 4 GB safety limit.") }
            archive_entry_set_perm(entry, type == mode_t(S_IFDIR) ? 0o755 : 0o644)
            guard archive_write_header(outputArchive, entry) == ARCHIVE_OK else { throw AppError.engine("An archive entry could not be written.") }
            while true {
                try Task.checkCancellation()
                let read = archive_read_data(reader, &buffer, buffer.count)
                if read == 0 { break }
                guard read > 0 else { throw AppError.input("This archive could not be fully decoded.") }
                total += Int64(read)
                guard total <= byteLimit else { throw AppError.input("The expanded archive exceeds the 4 GB safety limit.") }
                guard archive_write_data(outputArchive, buffer, read) == read else { throw AppError.engine("The archive could not be saved. Check available space.") }
            }
            guard archive_write_finish_entry(outputArchive) == ARCHIVE_OK else { throw AppError.engine("The archive entry could not be completed.") }
        }
        guard count > 0, archive_write_close(outputArchive) == ARCHIVE_OK else { throw AppError.input("This archive contains no supported files.") }
    }
    static func readEntries(_ input: URL) throws -> [String: Data] {
        guard let reader = archive_read_new() else { throw AppError.engine("The document package could not be opened.") }
        defer { archive_read_free(reader) }
        archive_read_support_format_zip(reader); archive_read_support_filter_all(reader)
        guard archive_read_open_filename(reader, input.path, 65536) == ARCHIVE_OK else { throw AppError.input("This document package is damaged or encrypted.") }
        var entry: OpaquePointer?
        var result: [String: Data] = [:]
        var total = 0
        var buffer = [UInt8](repeating: 0, count: 65536)
        while true {
            try Task.checkCancellation()
            let status = archive_read_next_header(reader, &entry)
            if status == ARCHIVE_EOF { break }
            guard status == ARCHIVE_OK, let entry, let name = archive_entry_pathname(entry) else { throw AppError.input("A document package entry is invalid.") }
            let path = String(cString: name)
            guard safePath(path), archive_entry_symlink(entry) == nil, result.count < 10000 else { throw AppError.input("The document package has unsafe entries.") }
            if archive_entry_filetype(entry) == mode_t(S_IFDIR) { continue }
            var data = Data()
            while true {
                let count = archive_read_data(reader, &buffer, buffer.count)
                if count == 0 { break }
                guard count > 0 else { throw AppError.input("The document package could not be decoded.") }
                total += count
                guard total <= 256 * 1024 * 1024, data.count + count <= 64 * 1024 * 1024 else { throw AppError.input("This document package exceeds the safety limit.") }
                data.append(contentsOf: buffer.prefix(count))
            }
            result[path] = data
        }
        return result
    }
    static func zipDirectory(_ directory: URL, to output: URL, epub: Bool = false) throws {
        try packDirectory(directory, to: output, format: "zip", epub: epub)
    }
    static func packDirectory(_ directory: URL, to output: URL, format: String, epub: Bool = false) throws {
        let writer = try writer(output, format: format)
        defer { archive_write_free(writer) }
        // Stored entries make the EPUB mimetype entry conformant and keep packaging deterministic.
        let resolvedDirectory = directory.resolvingSymlinksInPath()
        guard let enumerator = FileManager.default.enumerator(at: resolvedDirectory, includingPropertiesForKeys: [.isRegularFileKey]) else { throw AppError.engine("The document package could not be created.") }
        var files: [URL] = []
        var totalSize: Int64 = 0
        for case let file as URL in enumerator {
            let values = try file.resourceValues(forKeys: [.isSymbolicLinkKey, .isRegularFileKey, .fileSizeKey])
            guard values.isSymbolicLink != true else { throw AppError.input("This folder contains symbolic links. Remove them before creating an archive.") }
            if values.isRegularFile == true {
                totalSize += Int64(values.fileSize ?? 0)
                guard files.count < 100000, totalSize <= byteLimit else { throw AppError.input("This folder exceeds the archive safety limit.") }
                files.append(file)
            }
        }
        files.sort { $0.path < $1.path }
        if epub, let index = files.firstIndex(where: { $0.lastPathComponent == "mimetype" }) { files.insert(files.remove(at: index), at: 0) }
        for file in files {
            try Task.checkCancellation()
            let canonical = file.resolvingSymlinksInPath()
            guard canonical.path.hasPrefix(resolvedDirectory.path + "/") else { throw AppError.input("A folder entry points outside the selected folder.") }
            let name = String(canonical.path.dropFirst(resolvedDirectory.path.count + 1))
            guard safePath(name), let entry = archive_entry_new() else { throw AppError.engine("A document package entry could not be created.") }
            defer { archive_entry_free(entry) }
            let size = try file.resourceValues(forKeys: [.fileSizeKey]).fileSize ?? 0
            archive_entry_set_pathname(entry, name); archive_entry_set_size(entry, Int64(size))
            archive_entry_set_filetype(entry, UInt32(S_IFREG)); archive_entry_set_perm(entry, 0o644)
            guard archive_write_header(writer, entry) == ARCHIVE_OK else { throw AppError.engine("The document package header could not be written.") }
            let handle = try FileHandle(forReadingFrom: file)
            defer { try? handle.close() }
            while let data = try handle.read(upToCount: 65536), !data.isEmpty {
                try Task.checkCancellation()
                let written = data.withUnsafeBytes { archive_write_data(writer, $0.baseAddress, $0.count) }
                guard written == data.count else { throw AppError.engine("The document package could not be saved.") }
            }
            guard archive_write_finish_entry(writer) == ARCHIVE_OK else { throw AppError.engine("The archive entry could not be completed.") }
        }
        guard archive_write_close(writer) == ARCHIVE_OK else { throw AppError.engine("The document package could not be finalized.") }
    }
}
