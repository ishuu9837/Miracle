import Foundation

struct SubtitleEngine {
    struct Cue: Equatable { let start: String; let end: String; let text: String }
    static func parse(_ source: String) throws -> [Cue] {
        let source = source.replacingOccurrences(of: "\u{FEFF}", with: "").replacingOccurrences(of: "\r\n", with: "\n").replacingOccurrences(of: "\r", with: "\n")
        let blocks = source.components(separatedBy: "\n\n")
        var cues: [Cue] = []
        let regex = try NSRegularExpression(pattern: #"^(?:(\d{2,}):)?(\d{2}):(\d{2})[.,](\d{3})$"#)
        func timestamp(_ text: String) throws -> String {
            guard let match = regex.firstMatch(in: text, range: NSRange(text.startIndex..., in: text)) else { throw AppError.input("A subtitle has an invalid timestamp.") }
            func part(_ index: Int) -> String { Range(match.range(at: index), in: text).map { String(text[$0]) } ?? "00" }
            guard (Int(part(2)) ?? 60) < 60, (Int(part(3)) ?? 60) < 60 else { throw AppError.input("A subtitle timestamp is out of range.") }
            return "\(part(1)):\(part(2)):\(part(3)).\(part(4))"
        }
        for block in blocks {
            let lines = block.components(separatedBy: "\n")
            guard let index = lines.firstIndex(where: { $0.contains("-->") }) else { continue }
            let times = lines[index].components(separatedBy: "-->")
            guard times.count == 2 else { throw AppError.input("A subtitle timestamp is invalid.") }
            let start = try timestamp(times[0].trimmingCharacters(in: .whitespaces))
            let endToken = times[1].trimmingCharacters(in: .whitespaces).split(separator: " ").first.map(String.init) ?? ""
            let end = try timestamp(endToken)
            guard start <= end else { throw AppError.input("A subtitle ends before it starts.") }
            let text = lines.dropFirst(index + 1).joined(separator: "\n").trimmingCharacters(in: .whitespacesAndNewlines)
            if !text.isEmpty { cues.append(.init(start: start, end: end, text: text)) }
        }
        guard !cues.isEmpty else { throw AppError.input("This file contains no valid subtitle cues.") }
        return cues
    }
    static func convert(_ input: URL, to output: URL, format: String) throws {
        let cues = try parse(String(contentsOf: input, encoding: .utf8))
        let content: String
        switch format {
        case "srt": content = cues.enumerated().map { "\($0.offset + 1)\n\($0.element.start.replacingOccurrences(of: ".", with: ",")) --> \($0.element.end.replacingOccurrences(of: ".", with: ","))\n\($0.element.text)" }.joined(separator: "\n\n") + "\n"
        case "vtt": content = "WEBVTT\n\n" + cues.map { "\($0.start) --> \($0.end)\n\($0.text)" }.joined(separator: "\n\n") + "\n"
        case "txt": content = cues.map(\.text).joined(separator: "\n\n") + "\n"
        default: throw AppError.unsupported("Choose SRT, VTT, or TXT.")
        }
        try Task.checkCancellation()
        try content.write(to: output, atomically: true, encoding: .utf8)
    }
}
