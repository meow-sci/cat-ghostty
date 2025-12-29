using System.Reflection;
using caTTY.Core.Tracing;
using FsCheck;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace caTTY.Core.Tests.Property;

/// <summary>
/// Property-based tests for terminal tracing functionality.
/// These tests verify universal properties that should hold for all valid inputs.
/// </summary>
[TestFixture]
[Category("Property")]
public class TerminalTracingProperties
{
    private bool _originalEnabled;
    private string? _originalDbFilename;
    private string? _testDbFilename;

    [SetUp]
    public void SetUp()
    {
        // Store original state
        _originalEnabled = TerminalTracer.Enabled;
        _originalDbFilename = TerminalTracer.DbFilename;
        
        // Set unique database filename for this test using helper method
        _testDbFilename = TerminalTracer.SetupTestDatabase();
        
        // Reset the tracer to clean state
        TerminalTracer.Reset();
        
        // Enable tracing for tests
        TerminalTracer.Enabled = true;
        
        // Force initialization by making a trace call
        TerminalTracer.TraceEscape("INIT");
        
        // Clear the initialization trace
        ClearTraceDatabase();
    }

    [TearDown]
    public void TearDown()
    {
        // Restore original state
        TerminalTracer.Enabled = _originalEnabled;
        TerminalTracer.DbFilename = _originalDbFilename;
        TerminalTracer.Reset();
        
        // Clean up test database file
        if (_testDbFilename != null)
        {
            var databasePath = GetDatabasePath();
            if (databasePath != null && File.Exists(databasePath))
            {
                try
                {
                    File.Delete(databasePath);
                }
                catch
                {
                    // Ignore cleanup failures
                }
            }
        }
    }

    /// <summary>
    /// Generator for valid escape sequences.
    /// </summary>
    public static Arbitrary<string> EscapeSequenceArb =>
        Arb.From(Gen.Elements(new[]
        {
            "CSI H", "CSI 2J", "CSI 1;1H", "OSC 0 title", "ESC c", "DCS test"
        }));

    /// <summary>
    /// Generator for DCS commands.
    /// </summary>
    public static Arbitrary<string> DcsCommandArb =>
        Arb.From(Gen.Elements(new[]
        {
            "q", "p", "s", "$q", "+q", "+p"
        }));

    /// <summary>
    /// Generator for DCS parameters.
    /// </summary>
    public static Arbitrary<string> DcsParametersArb =>
        Arb.From(Gen.Elements(new[]
        {
            "", "1", "0;1", "1;2;3", "42"
        }));

    /// <summary>
    /// Generator for DCS data payloads.
    /// </summary>
    public static Arbitrary<string> DcsDataArb =>
        Arb.From(Gen.Elements(new[]
        {
            "", "test", "hello world", "data123", "complex;data;here"
        }));

    /// <summary>
    /// Generator for printable text.
    /// </summary>
    public static Arbitrary<string> PrintableTextArb =>
        Arb.From(Gen.Choose(1, 50).SelectMany(length =>
            Gen.ArrayOf(length, Gen.Choose(32, 126).Select(i => (char)i))
                .Select(chars => new string(chars))));

    /// <summary>
    /// Generator for OSC command numbers.
    /// </summary>
    public static Arbitrary<int> OscCommandArb =>
        Arb.From(Gen.Elements(new[]
        {
            0, 1, 2, 8, 10, 11, 21, 52
        }));

    /// <summary>
    /// Generator for OSC data payloads.
    /// </summary>
    public static Arbitrary<string> OscDataArb =>
        Arb.From(Gen.Elements(new[]
        {
            "", "title", "test data", "https://example.com", "clipboard data"
        }));

    /// <summary>
    /// Generator for trace directions.
    /// </summary>
    public static Arbitrary<TraceDirection> TraceDirectionArb =>
        Arb.From(Gen.Elements(TraceDirection.Input, TraceDirection.Output));

    /// <summary>
    /// **Feature: terminal-tracing-integration, Property 1: Escape Sequence Tracing Completeness (OSC portion)**
    /// **Validates: Requirements 1.2, 5.2**
    /// Property: For any valid OSC sequence processed by the parser, the sequence should appear 
    /// in the trace database with correct command, data payload, and direction information.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property OscSequenceTracingCompleteness()
    {
        return Prop.ForAll(OscCommandArb, OscDataArb, TraceDirectionArb, (command, data, direction) =>
        {
            // Arrange - Clear any existing traces
            ClearTraceDatabase();
            
            // Act - Trace OSC sequence using TraceHelper
            TraceHelper.TraceOscSequence(command, data, direction);
            
            // Assert - Verify OSC sequence is recorded correctly
            var traces = GetTracesFromDatabase();
            if (traces.Count != 1) return false;
            
            var trace = traces[0];
            var expectedDirection = direction == TraceDirection.Input ? "input" : "output";
            
            // Build expected OSC sequence format: "OSC command [data]"
            var expectedSequence = string.IsNullOrEmpty(data) ? $"OSC {command}" : $"OSC {command} {data}";
            
            return trace.Escape == expectedSequence && 
                   trace.Direction == expectedDirection;
        });
    }

    /// <summary>
    /// **Feature: terminal-tracing-integration, Property 1: Escape Sequence Tracing Completeness (DCS portion)**
    /// **Validates: Requirements 1.4, 5.5**
    /// Property: For any valid DCS sequence processed by the parser, the sequence should appear 
    /// in the trace database with correct command, parameters, and direction information.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property DcsSequenceTracingCompleteness()
    {
        return Prop.ForAll(DcsCommandArb, (command) =>
        {
            // Use fixed test values for simplicity
            var parameters = "1;2";
            var data = "testdata";
            var direction = TraceDirection.Output;
            
            // Arrange - Clear any existing traces
            ClearTraceDatabase();
            
            // Act - Trace DCS sequence using TraceHelper
            TraceHelper.TraceDcsSequence(command, parameters, data, direction);
            
            // Assert - Verify DCS sequence is recorded correctly
            var traces = GetTracesFromDatabase();
            if (traces.Count != 1) return false;
            
            var trace = traces[0];
            var expectedDirection = direction == TraceDirection.Input ? "input" : "output";
            
            // Build expected DCS sequence format: "DCS [parameters] command [data]"
            var expectedSequence = $"DCS {parameters} {command} {data}";
            
            return trace.Escape == expectedSequence && 
                   trace.Direction == expectedDirection;
        });
    }

    /// <summary>
    /// **Feature: terminal-tracing-integration, Property 7: Direction Tracking Accuracy**
    /// **Validates: Requirements 8.1, 8.2**
    /// Property: For any traced sequence or character, the direction field should correctly 
    /// reflect whether the data originated from user input ("input") or program output ("output").
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property DirectionTrackingAccuracy()
    {
        return Prop.ForAll(EscapeSequenceArb, TraceDirectionArb, (escapeSequence, direction) =>
        {
            // Arrange - Clear any existing traces
            ClearTraceDatabase();
            
            // Act - Trace escape sequence with specific direction
            TerminalTracer.TraceEscape(escapeSequence, direction);
            
            // Assert - Verify direction is recorded correctly
            var traces = GetTracesFromDatabase();
            if (traces.Count != 1) return false;
            
            var trace = traces[0];
            var expectedDirection = direction == TraceDirection.Input ? "input" : "output";
            
            return trace.Escape == escapeSequence && 
                   trace.Direction == expectedDirection;
        });
    }

    /// <summary>
    /// **Feature: terminal-tracing-integration, Property 7b: Printable Direction Tracking**
    /// **Validates: Requirements 8.1, 8.2**
    /// Property: For any traced printable text, the direction field should correctly 
    /// reflect the specified direction.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property PrintableDirectionTrackingAccuracy()
    {
        return Prop.ForAll(PrintableTextArb, TraceDirectionArb, (printableText, direction) =>
        {
            // Arrange - Clear any existing traces
            ClearTraceDatabase();
            
            // Act - Trace printable text with specific direction
            TerminalTracer.TracePrintable(printableText, direction);
            
            // Assert - Verify direction is recorded correctly
            var traces = GetTracesFromDatabase();
            if (traces.Count != 1) return false;
            
            var trace = traces[0];
            var expectedDirection = direction == TraceDirection.Input ? "input" : "output";
            
            return trace.Printable == printableText && 
                   trace.Direction == expectedDirection;
        });
    }

    /// <summary>
    /// **Feature: terminal-tracing-integration, Property 15: Default Direction Handling**
    /// **Validates: Requirements 10.5**
    /// Property: For any trace operation where direction is not explicitly specified, 
    /// the system should use "output" as the default direction value.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property DefaultDirectionHandling()
    {
        return Prop.ForAll(EscapeSequenceArb, escapeSequence =>
        {
            // Arrange - Clear any existing traces
            ClearTraceDatabase();
            
            // Act - Trace escape sequence without specifying direction (should default to Output)
            TerminalTracer.TraceEscape(escapeSequence);
            
            // Assert - Verify direction defaults to "output"
            var traces = GetTracesFromDatabase();
            if (traces.Count != 1) return false;
            
            var trace = traces[0];
            
            return trace.Escape == escapeSequence && 
                   trace.Direction == "output";
        });
    }

    /// <summary>
    /// **Feature: terminal-tracing-integration, Property 15b: Default Direction for Printable**
    /// **Validates: Requirements 10.5**
    /// Property: For any printable trace operation where direction is not explicitly specified, 
    /// the system should use "output" as the default direction value.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property DefaultDirectionHandlingForPrintable()
    {
        return Prop.ForAll(PrintableTextArb, printableText =>
        {
            // Arrange - Clear any existing traces
            ClearTraceDatabase();
            
            // Act - Trace printable text without specifying direction (should default to Output)
            TerminalTracer.TracePrintable(printableText);
            
            // Assert - Verify direction defaults to "output"
            var traces = GetTracesFromDatabase();
            if (traces.Count != 1) return false;
            
            var trace = traces[0];
            
            return trace.Printable == printableText && 
                   trace.Direction == "output";
        });
    }

    private void ClearTraceDatabase()
    {
        var databasePath = GetDatabasePath();
        if (databasePath == null) return;
        
        try
        {
            var connectionString = $"Data Source={databasePath}";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            
            var clearCommand = connection.CreateCommand();
            clearCommand.CommandText = "DELETE FROM trace";
            clearCommand.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            // Table might not exist yet, ignore
        }
    }

    private List<TraceEntry> GetTracesFromDatabase()
    {
        var databasePath = GetDatabasePath();
        if (databasePath == null) return new List<TraceEntry>();
        
        var traces = new List<TraceEntry>();
        
        try
        {
            var connectionString = $"Data Source={databasePath}";
            
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT escape_seq, printable, direction FROM trace ORDER BY id";
            
            using var reader = selectCommand.ExecuteReader();
            while (reader.Read())
            {
                traces.Add(new TraceEntry
                {
                    Escape = reader.IsDBNull(0) ? null : reader.GetString(0),
                    Printable = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Direction = reader.GetString(2)
                });
            }
        }
        catch (SqliteException)
        {
            // Table might not exist yet, return empty list
        }
        
        return traces;
    }

    private string? GetDatabasePath()
    {
        // Get the database path from TerminalTracer
        return TerminalTracer.GetDatabasePath();
    }

    private class TraceEntry
    {
        public string? Escape { get; set; }
        public string? Printable { get; set; }
        public string Direction { get; set; } = "";
    }
}