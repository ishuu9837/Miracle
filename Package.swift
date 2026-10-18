// swift-tools-version: 6.0
import PackageDescription
let package = Package(
    name: "Miracle", platforms: [.macOS(.v13)],
    products: [.executable(name: "Miracle", targets: ["Miracle"])],
    targets: [
        .systemLibrary(name: "CArchive", path: "Core/CArchive"),
        .executableTarget(name: "Miracle", dependencies: ["CArchive"], path: ".", exclude: ["Core/CArchive", "Miracle.xcodeproj", "Tests", "Resources", "Scripts", "Docs", "README.md", "CHANGELOG.md", "output"], sources: ["App", "Core", "Engines", "Services", "Features", "Persistence", "Shared"], linkerSettings: [.linkedLibrary("sqlite3"), .linkedLibrary("archive")]),
        .testTarget(name: "MiracleTests", dependencies: ["Miracle"], path: "Tests/Unit")
    ], swiftLanguageModes: [.v6]
)
