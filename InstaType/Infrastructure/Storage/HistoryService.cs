using InstaType.Models;
using InstaType.Services;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Text;

namespace InstaType.Infrastructure.Storage;

/// <summary>
/// Implements <see cref="IHistoryService"/> using SQLite via Microsoft.Data.Sqlite.
/// Database: %LOCALAPPDATA%\InstaType\history.db
/// Table: transcription_entries — created automatically on first run.
/// </summary>
internal sealed class HistoryService : IHistoryService
{
    private static string DbPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "InstaType", "history.db");

    private static string ConnectionString =>
        new SqliteConnectionStringBuilder
        {
            DataSource = DbPath,
            Mode       = SqliteOpenMode.ReadWriteCreate
        }.ToString();

    // ── Schema init ──────────────────────────────────────────────────────────

    static HistoryService()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DbPath)!);
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS transcription_entries (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                recorded_at TEXT    NOT NULL,
                raw_text    TEXT    NOT NULL,
                ai_text     TEXT,
                injected    TEXT    NOT NULL,
                app_name    TEXT,
                duration    REAL    NOT NULL DEFAULT 0,
                model_name  TEXT    NOT NULL DEFAULT '',
                locale      TEXT    NOT NULL DEFAULT 'en'
            );
            """;
        cmd.ExecuteNonQuery();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public async Task AddAsync(TranscriptionEntry entry, CancellationToken ct = default)
    {
        await using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO transcription_entries
                (recorded_at, raw_text, ai_text, injected, app_name, duration, model_name, locale)
            VALUES
                ($recorded_at, $raw_text, $ai_text, $injected, $app_name, $duration, $model_name, $locale);
            """;
        cmd.Parameters.AddWithValue("$recorded_at", entry.RecordedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$raw_text",    entry.RawText);
        cmd.Parameters.AddWithValue("$ai_text",     entry.AiText ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$injected",    entry.InjectedText);
        cmd.Parameters.AddWithValue("$app_name",    entry.TargetAppName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$duration",    entry.AudioDurationSeconds);
        cmd.Parameters.AddWithValue("$model_name",  entry.ModelName);
        cmd.Parameters.AddWithValue("$locale",      entry.Locale);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<TranscriptionEntry>> GetRecentAsync(
        int count, CancellationToken ct = default)
    {
        await using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT id, recorded_at, raw_text, ai_text, injected, app_name, duration, model_name, locale
            FROM transcription_entries
            ORDER BY id DESC
            LIMIT $count;
            """;
        cmd.Parameters.AddWithValue("$count", count);

        var list = new List<TranscriptionEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));
        return list;
    }

    public async Task<IReadOnlyList<TranscriptionEntry>> SearchAsync(
        string query, CancellationToken ct = default)
    {
        await using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, recorded_at, raw_text, ai_text, injected, app_name, duration, model_name, locale
            FROM transcription_entries
            WHERE raw_text LIKE $q OR injected LIKE $q
            ORDER BY id DESC
            LIMIT 500;
            """;
        cmd.Parameters.AddWithValue("$q", $"%{query}%");

        var list = new List<TranscriptionEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));
        return list;
    }

    public async Task ClearAsync(DateTimeOffset? before = null, CancellationToken ct = default)
    {
        await using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        if (before.HasValue)
        {
            cmd.CommandText = "DELETE FROM transcription_entries WHERE recorded_at < $before;";
            cmd.Parameters.AddWithValue("$before", before.Value.ToString("O"));
        }
        else
        {
            cmd.CommandText = "DELETE FROM transcription_entries;";
        }
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task ExportToCsvAsync(string filePath, CancellationToken ct = default)
    {
        var entries = await GetRecentAsync(int.MaxValue, ct);

        await using var writer = new StreamWriter(filePath, append: false, Encoding.UTF8);
        await writer.WriteLineAsync("Id,RecordedAt,RawText,AiText,InjectedText,AppName,Duration,Model,Locale");
        foreach (var e in entries)
        {
            await writer.WriteLineAsync(
                $"{e.Id},{Csv(e.RecordedAt.ToString("O"))},{Csv(e.RawText)}," +
                $"{Csv(e.AiText ?? "")},{Csv(e.InjectedText)},{Csv(e.TargetAppName ?? "")}," +
                $"{e.AudioDurationSeconds:F2},{Csv(e.ModelName)},{Csv(e.Locale)}");
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // ── Private ──────────────────────────────────────────────────────────────

    private static TranscriptionEntry Map(SqliteDataReader r) => new()
    {
        Id                  = r.GetInt64(0),
        RecordedAt          = DateTimeOffset.Parse(r.GetString(1)),
        RawText             = r.GetString(2),
        AiText              = r.IsDBNull(3) ? null : r.GetString(3),
        InjectedText        = r.GetString(4),
        TargetAppName       = r.IsDBNull(5) ? null : r.GetString(5),
        AudioDurationSeconds = r.GetDouble(6),
        ModelName           = r.GetString(7),
        Locale              = r.GetString(8),
    };

    private static string Csv(string s) =>
        s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;
}
