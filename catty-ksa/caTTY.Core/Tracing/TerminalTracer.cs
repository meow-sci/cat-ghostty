using System.Reflection;
using Microsoft.Data.Sqlite;

namespace caTTY.Core.Tracing;

/// <summary>
/// Represents the direction of data flow in terminal tracing.
/// </summary>
public enum TraceDirection
{
  /// <summary>
  /// Data flowing from user/application into the terminal (keyboard input, paste operations).
  /// </summary>
  Input,

  /// <summary>
  /// Data flowing from the running program to the terminal display (program output, escape sequences).
  /// </summary>
  Output
}

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
  /// Gets or sets the database path override. If set, this path will be used instead of the assembly directory.
  /// </summary>
  public static string? DbPath { get; set; } = null;

  /// <summary>
  /// Gets or sets the database filename override. If set, this filename will be used instead of "catty_trace.db".
  /// </summary>
  public static string? DbFilename { get; set; } = null;

  /// <summary>
  /// Helper method for test cases to set up a unique database filename and return it.
  /// This is a convenience method that generates a UUID-based filename and sets DbFilename.
  /// </summary>
  /// <returns>The generated unique database filename</returns>
  public static string SetupTestDatabase()
  {
    var testFilename = $"{Guid.NewGuid():N}.db";
    DbFilename = testFilename;
    return testFilename;
  }

  /// <summary>
  /// Reset the tracing system to a clean state. This will close any existing connections,
  /// reset all state variables, and prepare the tracer for reuse.
  /// </summary>
  public static void Reset()
  {
    lock (_lock)
    {
      try
      {
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Failed to close connection during reset: {ex.Message}");
      }

      _initialized = false;
      _disposed = false;
    }
  }

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
        string dllDir = DbPath ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        string filename = DbFilename ?? "catty_trace.db";

        var dbPath = Path.Combine(dllDir, filename);
        var connectionString = $"Data Source={dbPath}";

        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        // Create trace table if it doesn't exist
        var createTableCommand = _connection.CreateCommand();
        createTableCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS trace (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        time INTEGER NOT NULL,
                        escape_seq TEXT,
                        printable TEXT,
                        direction TEXT NOT NULL DEFAULT 'output'
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
  /// <param name="direction">The direction of data flow (default: Output)</param>
  public static void TraceEscape(string escapeSequence, TraceDirection direction = TraceDirection.Output)
  {
    if (!Enabled || string.IsNullOrEmpty(escapeSequence))
      return;

    TraceInternal(escapeSequence, null, direction);
  }

  /// <summary>
  /// Log printable characters to the trace database.
  /// </summary>
  /// <param name="printableText">The printable text to log</param>
  /// <param name="direction">The direction of data flow (default: Output)</param>
  public static void TracePrintable(string printableText, TraceDirection direction = TraceDirection.Output)
  {
    if (!Enabled || string.IsNullOrEmpty(printableText))
      return;

    TraceInternal(null, printableText, direction);
  }

  /// <summary>
  /// Log both escape sequence and printable text (for combined entries).
  /// </summary>
  /// <param name="escapeSequence">The escape sequence to log</param>
  /// <param name="printableText">The printable text to log</param>
  /// <param name="direction">The direction of data flow (default: Output)</param>
  public static void Trace(string? escapeSequence, string? printableText, TraceDirection direction = TraceDirection.Output)
  {
    if (!Enabled || (string.IsNullOrEmpty(escapeSequence) && string.IsNullOrEmpty(printableText)))
      return;

    TraceInternal(escapeSequence, printableText, direction);
  }

  private static void TraceInternal(string? escapeSequence, string? printableText, TraceDirection direction)
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
        var directionString = direction == TraceDirection.Input ? "input" : "output";

        var command = _connection.CreateCommand();
        command.CommandText = @"
                    INSERT INTO trace (time, escape_seq, printable, direction) 
                    VALUES (@time, @escape, @printable, @direction)";

        command.Parameters.AddWithValue("@time", timestamp);
        command.Parameters.AddWithValue("@escape", escapeSequence ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@printable", printableText ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@direction", directionString);

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
  public static string GetDatabasePath()
  {
    lock (_lock)
    {
      string dllDir = DbPath ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
      string filename = DbFilename ?? "catty_trace.db";
      return Path.Combine(dllDir, filename);
    }
  }

  /// <summary>
  /// Check if the tracer is currently active and ready to log.
  /// </summary>
  public static bool IsActive => Enabled && _initialized && !_disposed && _connection != null;
}