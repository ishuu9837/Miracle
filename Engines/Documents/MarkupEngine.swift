import Foundation

struct MarkupEngine {
    static func escape(_ string: String) -> String {
        string.replacingOccurrences(of: "&", with: "&amp;").replacingOccurrences(of: "<", with: "&lt;")
            .replacingOccurrences(of: ">", with: "&gt;").replacingOccurrences(of: "\"", with: "&quot;")
            .filter { $0 == "\n" || $0 == "\t" || $0.unicodeScalars.allSatisfy { $0.value >= 32 } }
    }
    static func html(_ text: String, markdown: Bool = false) -> String {
        let body = text.components(separatedBy: "\n").map { line -> String in
            if markdown {
                let prefix = line.prefix { $0 == "#" }
                if (1...6).contains(prefix.count), line.dropFirst(prefix.count).hasPrefix(" ") {
                    return "<h\(prefix.count)>\(escape(String(line.dropFirst(prefix.count + 1))))</h\(prefix.count)>"
                }
            }
            return "<p>\(escape(line).isEmpty ? "&#160;" : escape(line))</p>"
        }.joined(separator: "\n")
        return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"/><title>Converted document</title></head><body>\(body)</body></html>"
    }
    static func plainXML(_ data: Data) throws -> String {
        let delegate = XMLTextCollector()
        let parser = XMLParser(data: data); parser.shouldResolveExternalEntities = false; parser.delegate = delegate
        guard parser.parse() else { throw AppError.input("The document contains invalid XML.") }
        return delegate.output.trimmingCharacters(in: .whitespacesAndNewlines)
    }
}

private final class XMLTextCollector: NSObject, XMLParserDelegate {
    var output = ""
    private var skipped = 0
    func parser(_ parser: XMLParser, didStartElement elementName: String, namespaceURI: String?, qualifiedName qName: String?, attributes: [String: String]) {
        let name = elementName.components(separatedBy: ":").last ?? elementName
        if ["script", "style", "head"].contains(name) { skipped += 1 }
        if skipped == 0, ["br", "tab"].contains(name) { output += name == "br" ? "\n" : "\t" }
    }
    func parser(_ parser: XMLParser, foundCharacters string: String) { if skipped == 0 { output += string } }
    func parser(_ parser: XMLParser, didEndElement elementName: String, namespaceURI: String?, qualifiedName qName: String?) {
        let name = elementName.components(separatedBy: ":").last ?? elementName
        if ["script", "style", "head"].contains(name) { skipped = max(0, skipped - 1) }
        if skipped == 0, ["p", "div", "h1", "h2", "h3", "li", "tr"].contains(name) { output += "\n" }
    }
}
