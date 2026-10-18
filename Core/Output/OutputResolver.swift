import Foundation

/// Publishes complete outputs without overwriting existing user files.
struct OutputResolver {
    static func availableURL(in directory: URL, stem: String, extension ext: String) -> URL {
        var index = 0
        while true {
            let suffix = index == 0 ? "" : " \(index + 1)"
            let url = directory.appendingPathComponent(stem + suffix).appendingPathExtension(ext)
            if !FileManager.default.fileExists(atPath: url.path) { return url }
            index += 1
        }
    }

    static func publish(_ source: URL, beside input: URL, folder: URL?, stem: String, extension ext: String) throws -> URL {
        try Task.checkCancellation()
        let directory = folder ?? input.deletingLastPathComponent()
        let destination = availableURL(in: directory, stem: stem, extension: ext)
        let staging = directory.appendingPathComponent(".miracle-\(UUID().uuidString)")
        defer { try? FileManager.default.removeItem(at: staging) }
        try FileManager.default.copyItem(at: source, to: staging)
        let values = try staging.resourceValues(forKeys: [.fileSizeKey, .isDirectoryKey])
        let nonempty = values.isDirectory == true ? !(try FileManager.default.contentsOfDirectory(atPath: staging.path)).isEmpty : (values.fileSize ?? 0) > 0
        guard nonempty else {
            throw AppError.engine("The engine produced an empty file. Try another format.")
        }
        try Task.checkCancellation()
        // moveItem fails if a competing writer claims the destination; it never replaces it.
        try FileManager.default.moveItem(at: staging, to: destination)
        return destination
    }
}
