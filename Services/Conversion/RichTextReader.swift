import AppKit

struct RichTextReader {
    static func read(_ input: URL) throws -> String {
        if input.pathExtension.lowercased() == "textclipping" {
            let data = try Data(contentsOf: input)
            if let plist = try? PropertyListSerialization.propertyList(from: data, format: nil) as? [String: Any] {
                let values = (plist["UTI-Data"] as? [String: Any]) ?? plist
                for key in ["public.utf8-plain-text", "NSStringPboardType", "com.apple.traditional-mac-plain-text"] {
                    if let string = values[key] as? String { return string }
                    if let data = values[key] as? Data, let string = String(data: data, encoding: key.contains("traditional") ? .macOSRoman : .utf8) { return string }
                }
                if let rtf = values["public.rtf"] as? Data { return try NSAttributedString(data: rtf, options: [.documentType: NSAttributedString.DocumentType.rtf], documentAttributes: nil).string }
            }
            throw AppError.input("This text clipping uses an unsupported legacy format. Paste its text into a TXT file first.")
        }
        let type: NSAttributedString.DocumentType = input.pathExtension.lowercased() == "rtfd" ? .rtfd : .rtf
        return try NSAttributedString(url: input, options: [.documentType: type], documentAttributes: nil).string
    }
}
