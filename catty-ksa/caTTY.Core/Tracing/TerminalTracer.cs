using System.Reflection;
using Microsoft.Data.Sqlite;

namespace caTTY.Core.Tracing;

/// <summary>
/// Terminal tracing system that logs escape sequences and printable characters to SQLite database.
/// </summary>
public static class TerminalTracer
{
  private static SqliteConnection? _connection;
  private static readonly object _lock = new();
  private static bool _initialized = false;
  private static bool _disposed = false;

  /// <summary>
  /// Gets or sets whether tracing is enabled. When false, all tracing operations are no-ops.
  /// Default is false for performance.
  /// </summary>
  public static bool Enabled { get; set; } = false;

  /// <summary>
  /// Initialize the tracing database. Called automatically on first trace.
  /// </summary>
  public static void Initialize()
  {
    lock (_lock)
    {
      if (_initialized || _disposed)
        return;

      try
      {
        string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";

        var dbPath = Path.Combine(dllDir, "catty_trace.db");
        var connectionString = $"Data Source={dbPath}";

        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        // Create trace table if it doesn't exist
        var createTableCommand = _connection.CreateCommand();
        createTableCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS trace (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        time INTEGER NOT NULL,
                        escape TEXT,
                        printable TEXT
                    )";
        createTableCommand.ExecuteNonQuery();

        _initialized = true;
      }
      catch (Exception ex)
      {
        // Silently fail initialization to avoid breaking terminal functionality
        Console.Error.WriteLine($"Failed to initialize terminal tracer: {ex.Message}");
      }
    }
  }

  /// <summary>
  /// Log an escape sequence to the trace database.
  /// </summary>
  /// <param name="escapeSequence">The escape sequence to log</param>
  public static void TraceEscape(string escapeSequence)
  {
    if (!Enabled || string.IsNullOrEmpty(escapeSequence))
      return;

    TraceInternal(escapeSequence, null);
  }

  /// <summary>
  /// Log printable characters to the trace database.
  /// </summary>
  /// <param name="printableText">The printable text to log</param>
  public static void TracePrintable(string printableText)
  {
    if (!Enabled || string.IsNullOrEmpty(printableText))
      return;

    TraceInternal(null, printableText);
  }

  /// <summary>
  /// Log both escape sequence and printable text (for combined entries).
  /// </summary>
  /// <param name="escapeSequence">The escape sequence to log</param>
  /// <param name="printableText">The printable text to log</param>
  public static void Trace(string? escapeSequence, string? printableText)
  {
    if (!Enabled || (string.IsNullOrEmpty(escapeSequence) && string.IsNullOrEmpty(printableText)))
      return;

    TraceInternal(escapeSequence, printableText);
  }

  private static void TraceInternal(string? escapeSequence, string? printableText)
  {
    lock (_lock)
    {
      if (_disposed)
        return;

      if (!_initialized)
        Initialize();

      if (_connection == null || !_initialized)
        return;

      try
      {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var command = _connection.CreateCommand();
        command.CommandText = @"
                    INSERT INTO trace (time, escape, printable) 
                    VALUES (@time, @escape, @printable)";

        command.Parameters.AddWithValue("@time", timestamp);
        command.Parameters.AddWithValue("@escape", escapeSequence ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@printable", printableText ?? (object)DBNull.Value);

        command.ExecuteNonQuery();
      }
      catch (Exception ex)
      {
        // Silently fail to avoid breaking terminal functionality
        Console.Error.WriteLine($"Failed to write trace: {ex.Message}");
      }
    }
  }

  /// <summary>
  /// Shutdown the tracing system and close database connections.
  /// Call this during application shutdown.
  /// </summary>
  public static void Shutdown()
  {
    lock (_lock)
    {
      if (_disposed)
        return;

      try
      {
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
        _disposed = true;
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Failed to shutdown terminal tracer: {ex.Message}");
      }
    }
  }

  /// <summary>
  /// Get the current database file path for debugging purposes.
  /// </summary>
  /// <returns>The path to the SQLite database file, or null if not initialized</returns>
  public static string? GetDatabasePath()
  {
    lock (_lock)
    {
      if (!_initialized || _connection == null)
        return null;

      return Path.Combine(Path.GetTempPath(), "catty_trace.db");
    }
  }

  /// <summary>
  /// Check if the tracer is currently active and ready to log.
  /// </summary>
  public static bool IsActive => Enabled && _initialized && !_disposed && _connection != null;
}