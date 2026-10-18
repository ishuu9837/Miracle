import Foundation

struct DocumentEngine {
    static func office(_ input: URL, format: String, directory: URL, filter: String? = nil, inputFilter: String? = nil) async throws -> URL {
        let profile = directory.appendingPathComponent("profile-\(UUID().uuidString)")
        let user = profile.appendingPathComponent("user")
        try FileManager.default.createDirectory(at: user, withIntermediateDirectories: true)
        let config = """
        <?xml version="1.0" encoding="UTF-8"?><oor:items xmlns:oor="http://openoffice.org/2001/registry"><item oor:path="/org.openoffice.Office.Common/Security/Scripting"><prop oor:name="MacroSecurityLevel" oor:op="fuse"><value>3</value></prop><prop oor:name="DisableMacrosExecution" oor:op="fuse"><value>true</value></prop></item></oor:items>
        """
        try config.write(to: user.appendingPathComponent("registrymodifications.xcu"), atomically: true, encoding: .utf8)
        let destination = directory.appendingPathComponent("office-\(UUID().uuidString)")
        try FileManager.default.createDirectory(at: destination, withIntermediateDirectories: true)
        let defaultFilter: String
        switch format {
        case "docx": defaultFilter = "docx:Office Open XML Text"
        case "xlsx": defaultFilter = "xlsx:Calc MS Excel 2007 XML"
        case "ods": defaultFilter = "ods:calc8"
        default: defaultFilter = format
        }
        var args = ["-env:UserInstallation=\(profile.absoluteString)", "--headless", "--nologo", "--nodefault", "--nofirststartwizard", "--norestore", "--convert-to", filter ?? defaultFilter, "--outdir", destination.path, input.path]
        if let inputFilter { args.insert("--infilter=\(inputFilter)", at: 1) }
        else if input.pathExtension.lowercased() == "html" { args.insert("--infilter=HTML (StarWriter)", at: 1) }
        try await ProcessRunner.run(EngineLocator.office, arguments: args, directory: directory)
        let result = destination.appendingPathComponent(input.deletingPathExtension().lastPathComponent).appendingPathExtension(format)
        guard FileManager.default.fileExists(atPath: result.path) else {
            throw AppError.unsupported("This document could not be exported as \(format.uppercased()). Check that it is not encrypted or damaged.")
        }
        return result
    }
    static func text(_ input: URL, directory: URL) async throws -> String {
        switch input.pathExtension.lowercased() {
        case "txt", "md", "markdown":
            guard let text = String(data: try Data(contentsOf: input), encoding: .utf8) else { throw AppError.input("Save this text file as UTF-8 before converting it.") }
            return text
        case "rtf", "rtfd", "textclipping": return try RichTextReader.read(input)
        case "docx":
            let entries = try ArchiveEngine.readEntries(input)
            guard let xml = entries["word/document.xml"] else { throw AppError.input("This is not a valid DOCX document.") }
            return try MarkupEngine.plainXML(xml)
        case "epub":
            let entries = try ArchiveEngine.readEntries(input)
            let pages = EPUBEngine.readingOrder(entries)
            guard !pages.isEmpty else { throw AppError.input("This EPUB has no readable chapters, or is protected by DRM.") }
            return try pages.compactMap { entries[$0] }.map(MarkupEngine.plainXML).joined(separator: "\n\n")
        default:
            let result = try await office(input, format: "txt", directory: directory, filter: "txt:Text (encoded):UTF8")
            return try String(contentsOf: result, encoding: .utf8)
        }
    }
    static func convert(_ input: URL, to output: URL, format: String, category: FileCategory, directory: URL) async throws {
        if category == .spreadsheet || category == .slides {
            var filter: String?
            if format == "tsv" { filter = "csv:Text - txt - csv (StarCalc):9,34,76,1,,0,false,true,true" }
            if format == "csv" { filter = "csv:Text - txt - csv (StarCalc):44,34,76,1,,0,false,true,true" }
            let result = try await office(input, format: format == "tsv" ? "csv" : format, directory: directory, filter: filter)
            try FileManager.default.copyItem(at: result, to: output)
            return
        }
        if category == .document && format == "pdf" {
            let result = try await office(input, format: "pdf", directory: directory)
            try FileManager.default.copyItem(at: result, to: output)
            return
        }
        if format == "pages" {
            let docx = directory.appendingPathComponent("pages-input-\(UUID().uuidString).docx")
            try await convert(input, to: docx, format: "docx", category: category, directory: directory)
            try await PagesService.convert(docx, to: output)
            return
        }
        let contents = try await text(input, directory: directory)
        if format == "txt" || format == "md" {
            try contents.write(to: output, atomically: true, encoding: .utf8)
        } else if format == "epub" {
            try EPUBEngine.write(text: contents, title: input.deletingPathExtension().lastPathComponent, output: output, directory: directory)
        } else {
            let html = directory.appendingPathComponent("document-\(UUID().uuidString).html")
            try MarkupEngine.html(contents, markdown: ["md", "markdown"].contains(input.pathExtension.lowercased())).write(to: html, atomically: true, encoding: .utf8)
            if format == "html" { try FileManager.default.copyItem(at: html, to: output) }
            else {
                let result = try await office(html, format: format, directory: directory)
                try FileManager.default.copyItem(at: result, to: output)
            }
        }
    }
}
