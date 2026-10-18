import Foundation

/// Executes only fixed engine paths with argument arrays, never through a shell.
/// Output goes to private files so a full pipe cannot deadlock a converter.
struct ProcessRunner {
    static func run(_ executable: URL, arguments: [String], directory: URL, timeout: TimeInterval = 600) async throws {
        try Task.checkCancellation()
        guard FileManager.default.isExecutableFile(atPath: executable.path) else {
            throw AppError.unsupported("The required conversion engine is missing. Reinstall the complete Miracle app.")
        }
        let log = directory.appendingPathComponent("engine-\(UUID().uuidString).log")
        FileManager.default.createFile(atPath: log.path, contents: nil, attributes: [.posixPermissions: 0o600])
        let handle = try FileHandle(forWritingTo: log)
        defer { try? handle.close(); try? FileManager.default.removeItem(at: log) }
        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/sandbox-exec")
        // URL path normalization can retain /var while Seatbelt matches /private/var.
        // realpath supplies the filesystem spelling required by the worker policy.
        guard let resolvedDirectory = realpath(directory.path, nil) else { throw AppError.engine("The temporary conversion folder is unavailable.") }
        defer { free(resolvedDirectory) }
        let safeDirectory = String(cString: resolvedDirectory).replacingOccurrences(of: "\\", with: "\\\\").replacingOccurrences(of: "\"", with: "\\\"")
        let originalDirectory = directory.standardizedFileURL.path.replacingOccurrences(of: "\\", with: "\\\\").replacingOccurrences(of: "\"", with: "\\\"")
        // LibreOffice's upstream IPC socket lives in /tmp independently of TMPDIR.
        // Permit only its per-user socket names; document writes remain job-local.
        let officeIPC = executable == EngineLocator.office
            ? """
              (require-not (literal "/private/tmp"))
              (require-not (literal "/tmp"))
              (require-not (regex #"^/private/tmp/OSL_PIPE_\(getuid())_SingleOfficeIPC_[A-Za-z0-9]+$"))
              (require-not (regex #"^/tmp/OSL_PIPE_\(getuid())_SingleOfficeIPC_[A-Za-z0-9]+$"))
              """ : ""
        let policy = """
        (version 1)
        (allow default)
        (deny network-outbound (remote ip "*:*"))
        (deny file-write*
            (require-all
                (require-not (subpath "\(safeDirectory)"))
                (require-not (subpath "\(originalDirectory)"))
                (require-not (literal "/dev/null"))
                \(officeIPC)
            )
        )
        """
        process.arguments = ["-p", policy, executable.path] + arguments
        process.currentDirectoryURL = directory
        process.standardInput = FileHandle.nullDevice
        process.standardOutput = handle; process.standardError = handle
        process.environment = ["PATH": "/usr/bin:/bin", "HOME": directory.path, "TMPDIR": directory.path,
                               "LANG": "en_US.UTF-8", "LC_ALL": "en_US.UTF-8", "OMP_NUM_THREADS": "2"]
        do { try process.run() } catch { throw AppError.engine("The conversion engine could not start. Reinstall Miracle and try again.") }
        let deadline = Date().addingTimeInterval(timeout)
        while process.isRunning {
            if Task.isCancelled || Date() > deadline {
                process.terminate()
                for _ in 0..<20 where process.isRunning { try? await Task.sleep(for: .milliseconds(50)) }
                if process.isRunning { kill(process.processIdentifier, SIGKILL) }
                while process.isRunning { try? await Task.sleep(for: .milliseconds(20)) }
                if Task.isCancelled { throw CancellationError() }
                throw AppError.engine("This conversion took too long. Try a smaller file.")
            }
            try? await Task.sleep(for: .milliseconds(60))
        }
        try Task.checkCancellation()
        guard process.terminationStatus == 0 else {
            throw AppError.engine("This file could not be converted. It may be damaged, protected, or use an unsupported codec.")
        }
    }
}
