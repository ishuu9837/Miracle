import Foundation

struct EPUBEngine {
    static func readingOrder(_ entries: [String: Data]) -> [String] {
        if let package = entries.keys.sorted().first(where: { $0.hasSuffix(".opf") }), let data = entries[package] {
            let delegate = PackageCollector()
            let parser = XMLParser(data: data); parser.shouldResolveExternalEntities = false; parser.delegate = delegate
            if parser.parse() {
                let folder = (package as NSString).deletingLastPathComponent
                let paths = delegate.spine.compactMap { delegate.manifest[$0] }.map { folder.isEmpty ? $0 : folder + "/" + $0 }
                if !paths.isEmpty { return paths.filter { entries[$0] != nil } }
            }
        }
        return entries.keys.filter { $0.hasSuffix(".xhtml") || $0.hasSuffix(".html") }.sorted()
    }
    static func write(text: String, title: String, output: URL, directory: URL) throws {
        let root = directory.appendingPathComponent("epub-\(UUID().uuidString)")
        func file(_ name: String, _ content: String) throws {
            let url = root.appendingPathComponent(name)
            try FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
            try content.write(to: url, atomically: true, encoding: .utf8)
        }
        let title = MarkupEngine.escape(title), id = UUID().uuidString
        try file("mimetype", "application/epub+zip")
        try file("META-INF/container.xml", """
        <?xml version="1.0"?><container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container"><rootfiles><rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/></rootfiles></container>
        """)
        let paragraphs = text.components(separatedBy: "\n").map { "<p>\(MarkupEngine.escape($0))</p>" }.joined()
        try file("OEBPS/chapter.xhtml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><html xmlns=\"http://www.w3.org/1999/xhtml\"><head><title>\(title)</title></head><body><h1>\(title)</h1>\(paragraphs)</body></html>")
        try file("OEBPS/nav.xhtml", "<?xml version=\"1.0\"?><html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\"><head><title>Contents</title></head><body><nav epub:type=\"toc\"><ol><li><a href=\"chapter.xhtml\">\(title)</a></li></ol></nav></body></html>")
        try file("OEBPS/content.opf", """
        <?xml version="1.0" encoding="UTF-8"?><package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="id"><metadata xmlns:dc="http://purl.org/dc/elements/1.1/"><dc:identifier id="id">urn:uuid:\(id)</dc:identifier><dc:title>\(title)</dc:title><dc:language>en</dc:language><meta property="dcterms:modified">2000-01-01T00:00:00Z</meta></metadata><manifest><item id="chapter" href="chapter.xhtml" media-type="application/xhtml+xml"/><item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav"/></manifest><spine><itemref idref="chapter"/></spine></package>
        """)
        try ArchiveEngine.zipDirectory(root, to: output, epub: true)
    }
}
private final class PackageCollector: NSObject, XMLParserDelegate {
    var manifest: [String: String] = [:]
    var spine: [String] = []
    func parser(_ parser: XMLParser, didStartElement elementName: String, namespaceURI: String?, qualifiedName qName: String?, attributes: [String: String]) {
        if elementName == "item", let id = attributes["id"], let href = attributes["href"] { manifest[id] = href.removingPercentEncoding ?? href }
        if elementName == "itemref", let id = attributes["idref"] { spine.append(id) }
    }
}
