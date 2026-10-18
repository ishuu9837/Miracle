import SwiftUI

enum DesignTokens {
    static let spacing: CGFloat = 16
    static let radius: CGFloat = 14
    static let wheelRadius: CGFloat = 126
    static let wheelSize: CGFloat = 400
    static let panelWidth: CGFloat = 500
    static let caption: Font = .system(size: 11)
    static let label: Font = .system(size: 12, weight: .medium)
    static let title: Font = .system(size: 20, weight: .semibold)
}

enum AppError: LocalizedError {
    case input(String), unsupported(String), engine(String), permission(String)
    var errorDescription: String? {
        switch self {
        case .input(let text), .unsupported(let text), .engine(let text), .permission(let text): return text
        }
    }
}
