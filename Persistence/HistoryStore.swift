import Foundation
import SQLite3

struct HistoryEntry: Identifiable, Sendable {
    let id: String
    let operation: String
    let name: String
    let status: String
    let date: Date
}

private final class SQLiteConnection {
    var handle: OpaquePointer?
    deinit { sqlite3_close(handle) }
}

/// History contains display names only. Output URLs remain in memory for the current session.
@MainActor final class HistoryStore {
    private let connection = SQLiteConnection()
    private var database: OpaquePointer? { connection.handle }
    init(location: URL? = nil) throws {
        let folder = try location ?? FileManager.default.url(for: .applicationSupportDirectory, in: .userDomainMask,
                                                            appropriateFor: nil, create: true).appendingPathComponent("Miracle")
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true, attributes: [.posixPermissions: 0o700])
        let url = folder.appendingPathComponent("history.sqlite")
        guard sqlite3_open(url.path, &connection.handle) == SQLITE_OK else { throw AppError.engine("Job history could not be opened.") }
        try FileManager.default.setAttributes([.posixPermissions: 0o600], ofItemAtPath: url.path)
        try execute("PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;")
        try execute("""
            CREATE TABLE IF NOT EXISTS schema_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            INSERT OR IGNORE INTO schema_meta VALUES ('schema_version', '1');
            CREATE TABLE IF NOT EXISTS job (
                id TEXT PRIMARY KEY, operation TEXT NOT NULL, source_display_name TEXT NOT NULL,
                status TEXT NOT NULL CHECK(status IN ('succeeded','failed','cancelled')), created_at REAL NOT NULL
            );
            CREATE INDEX IF NOT EXISTS job_date ON job(created_at DESC);
            """)
    }
    private func execute(_ sql: String) throws {
        guard sqlite3_exec(database, sql, nil, nil, nil) == SQLITE_OK else { throw AppError.engine("Job history could not be updated.") }
    }
    func add(operation: String, name: String, status: String) throws {
        var statement: OpaquePointer?
        defer { sqlite3_finalize(statement) }
        guard sqlite3_prepare_v2(database, "INSERT INTO job VALUES (?, ?, ?, ?, ?)", -1, &statement, nil) == SQLITE_OK else {
            throw AppError.engine("Job history could not be saved.")
        }
        let transient = unsafeBitCast(-1, to: sqlite3_destructor_type.self)
        for (index, value) in [UUID().uuidString, operation, name, status].enumerated() {
            sqlite3_bind_text(statement, Int32(index + 1), value, -1, transient)
        }
        sqlite3_bind_double(statement, 5, Date().timeIntervalSince1970)
        guard sqlite3_step(statement) == SQLITE_DONE else { throw AppError.engine("Job history could not be saved.") }
        try execute("DELETE FROM job WHERE id NOT IN (SELECT id FROM job ORDER BY created_at DESC LIMIT 50)")
    }
    func recent() throws -> [HistoryEntry] {
        var statement: OpaquePointer?
        defer { sqlite3_finalize(statement) }
        guard sqlite3_prepare_v2(database, "SELECT id, operation, source_display_name, status, created_at FROM job ORDER BY created_at DESC LIMIT 50", -1, &statement, nil) == SQLITE_OK else {
            throw AppError.engine("Job history could not be read.")
        }
        var rows: [HistoryEntry] = []
        while sqlite3_step(statement) == SQLITE_ROW {
            func value(_ column: Int32) -> String { sqlite3_column_text(statement, column).map { String(cString: $0) } ?? "" }
            rows.append(.init(id: value(0), operation: value(1), name: value(2), status: value(3), date: Date(timeIntervalSince1970: sqlite3_column_double(statement, 4))))
        }
        return rows
    }
    func clear() throws { try execute("DELETE FROM job; PRAGMA wal_checkpoint(TRUNCATE);") }
}
