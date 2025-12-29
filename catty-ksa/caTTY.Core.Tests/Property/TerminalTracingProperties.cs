using System.Reflection;
using caTTY.Core.Parsing;
using caTTY.Core.Terminal;
using caTTY.Core.Tests.Unit;
using caTTY.Core.Tracing;
using FsCheck;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
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
    /// Generator for ESC sequences (non-CSI escape sequences).
    /// </summary>
    public static Arbitrary<string> EscSequenceArb =>
        Arb.From(Gen.Elements(new[]
        {
            "7", "8", "M", "D", "E", "H", "c", // Single-byte ESC sequences
            "(A", "(B", "(0", ")A", ")B", ")0", // Character set designation
            "*A", "*B", "+A", "+B", // More character set designations
            "=", ">", "N", "O" // Other ESC sequences
        }));

    /// <summary>
    /// Generator for valid escape sequences.
    /// </summary>
    public static Arbitrary<string> EscapeSequenceArb =>
        Arb.From(Gen.Elements(new[]
        {
            "ESC[H", "ESC[2J", "ESC[1;1H", "ESC]0;title\\x07", "ESC7", "ESCPtest ESC\\"
        }));

    /// <summary>
    /// Generator for DCS commands.
    /// </summary>
    public static Arbitrary<string> DcsCommandArb =>
        Arb.From(Gen.Elements(new[]
        {
            "q", "p", "s", "$q", "+q", "+p"
        }).Where(s => !string.IsNullOrEmpty(s)));

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
    /// Generator for control characters (0x00-0x1F, 0x7F).
    /// </summary>
    public static Arbitrary<byte> ControlCharacterArb =>
        Arb.From(Gen.Elements(new byte[]
        {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, // NUL, SOH, STX, ETX, EOT, ENQ, ACK, BEL
            0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, // BS, HT, LF, VT, FF, CR, SO, SI
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, // DLE, DC1, DC2, DC3, DC4, NAK, SYN, ETB
            0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, // CAN, EM, SUB, ESC, FS, GS, RS, US
            0x7F // DEL
        }));

    /// <summary>
    /// **Feature: terminal-tracing-integration, Property 2: Control Character Tracing**
    /// **Validates: Requirements 1.5**
    /// Property: For any control character (0x00-0x1F, 0x7F) processed by the parser, 
    /// the character should be traced using the appropriate control character name.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property ControlCharacterTracing()
    {
        return Prop.ForAll(ControlCharacterArb, TraceDirectionArb, (controlByte, direction) =>
        {
            // Arrange - Clear any existing traces
            ClearTraceDatabase();
            
            // Act - Trace control character using TraceHelper
            TraceHelper.TraceControlChar(controlByte, direction);
            
            // Assert - Verify control character is recorded correctly
            var traces = GetTracesFromDatabase();
            if (traces.Count != 1) return false;
            
            var trace = traces[0];
            var expectedDirection = direction == TraceDirection.Input ? "input" : "output";
            
            // Build expected control character format based on TraceHelper.TraceControlChar implementation
            var expectedControlName = controlByte switch
            {
                0x00 => "NUL",
                0x07 => "BEL",
                0x08 => "BS",
                0x09 => "HT",
                0x0A => "LF",
                0x0B => "VT",
                0x0C => "FF",
                0x0D => "CR",
                0x0E => "SO",
                0x0F => "SI",
                0x1B => "ESC",
                0x7F => "DEL",
                _ => $"C{controlByte:X2}"
            };
            
            var expectedSequence = $"<{expectedControlName}>";
            
            return trace.Escape == expectedSequence && 
                   trace.Direction == expectedDirection;
        });
    }

    /// <summary>
    /// **Feature: terminal-tracing-integration, Property 1: Escape Sequence Tracing Completeness (ESC portion)**
    /// **Validates: Requirements 1.3, 5.4**
    /// Property: For any valid ESC sequence processed by the parser, the sequence should appear 
    /// in the trace database with correct sequence characters and direction information.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property EscSequenceTracingCompleteness()
    {
        return Prop.ForAll(EscSequenceArb, TraceDirectionArb, (escSequence, direction) =>
        {
            // Arrange - Clear any existing traces
            ClearTraceDatabase();
            
            // Act - Trace ESC sequence using TraceHelper
            TraceHelper.TraceEscSequence(escSequence, direction);
            
            // Assert - Verify ESC sequence is recorded correctly
            var traces = GetTracesFromDatabase();
            if (traces.Count != 1) return false;
            
            var trace = traces[0];
            var expectedDirection = direction == TraceDirection.Input ? "input" : "output";
            
            // Build expected ESC sequence format: "ESCsequence"
            var expectedSequence = $"ESC{escSequence}";
            
            return trace.Escape == expectedSequence && 
                   trace.Direction == expectedDirection;
        });
    }

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
            
            // Build expected OSC sequence format: "ESC]command;data\x07"
            var expectedSequence = string.IsNullOrEmpty(data) ? $"ESC]{command}\\x07" : $"ESC]{command};{data}\\x07";
            
            return trace.Escape == expectedSequence && 
                   trace.Direction == expectedDirection;
        });
    }

    /// <summary>
    /// **Feature: terminal-tracing-integration, Property 1: Escape Sequence Tracing Completeness (DCS verification)**
    /// **Validates: Requirements 1.4, 5.5**
    /// Property: For any valid DCS sequence processed by the parser, the sequence should appear 
    /// in the trace database with correct command, parameters, and direction information.
    /// </summary>
    [Test]
    public void DcsSequenceTracingCompletion_SimpleTest()
    {
        // Arrange - Clear any existing traces
        ClearTraceDatabase();
        
        // Create a mock handler to capture DCS messages
        var handlers = new TestParserHandlers();
        var options = new ParserOptions
        {
            Handlers = handlers,
            Logger = new TestLogger()
        };
        var parser = new Parser(options);
        
        // Build DCS sequence: ESC P 1 q ST (simple DCS query)
        var dcsSequence = "\x1bP1q\x1b\\";
        var dcsBytes = System.Text.Encoding.UTF8.GetBytes(dcsSequence);
        
        // Act - Process DCS sequence through parser
        parser.PushBytes(dcsBytes);
        
        // Assert - First verify parser processed the DCS
        Assert.That(handlers.DcsMessages, Has.Count.EqualTo(1), "Parser should process DCS sequence");
        
        // Then verify tracing captured it
        var traces = GetTracesFromDatabase();
        Assert.That(traces, Has.Count.EqualTo(1), "Should have exactly one trace entry");
        
        var trace = traces[0];
        
        // Build expected DCS sequence format: "ESCPparameterscommand ESC\"
        var expectedSequence = "ESCP1qESC\\";
        
        Assert.That(trace.Escape, Is.EqualTo(expectedSequence), "Trace should contain correct DCS sequence");
        Assert.That(trace.Direction, Is.EqualTo("output"), "Direction should be output");
    }

    /// <summary>
    /// **Feature: terminal-tracing-integration, Property 1: Escape Sequence Tracing Completeness (DCS verification)**
    /// **Validates: Requirements 1.4, 5.5**
    /// Property: For any valid DCS sequence processed by the parser, the sequence should appear 
    /// in the trace database with correct command, parameters, and direction information.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property DcsSequenceTracingCompletion()
    {
        return Prop.ForAll(DcsCommandArb, DcsParametersArb, TraceDirectionArb, (command, parameters, direction) =>
        {
            // Arrange - Clear any existing traces
            ClearTraceDatabase();
            
            // Act - Trace DCS sequence using TraceHelper (testing the helper method directly)
            TraceHelper.TraceDcsSequence(command, parameters, null, direction);
            
            // Assert - Verify DCS sequence is recorded correctly
            var traces = GetTracesFromDatabase();
            if (traces.Count != 1) return false;
            
            var trace = traces[0];
            var expectedDirection = direction == TraceDirection.Input ? "input" : "output";
            
            // Build expected DCS sequence format: "ESCPparameterscommand ESC\"
            var expectedSequence = string.IsNullOrEmpty(parameters) 
                ? $"ESCP{command}ESC\\" 
                : $"ESCP{parameters}{command}ESC\\";
            
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
    /// **Feature: terminal-tracing-integration, Property 3: Printable Character Tracing**
    /// **Validates: Requirements 2.1, 2.4**
    /// Property: For any printable character written to the screen buffer, the character should appear 
    /// in the trace database with correct position and direction information.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property PrintableCharacterTracing()
    {
        var printableCharArb = Arb.From(Gen.Choose(32, 126).Select(i => (char)i));
        
        return Prop.ForAll(printableCharArb, character =>
        {
            // Arrange - Clear any existing traces and create terminal emulator
            ClearTraceDatabase();
            
            using var terminal = new TerminalEmulator(80, 24);
            
            // Act - Write character at cursor using the terminal emulator
            // This should trigger tracing through WriteCharacterAtCursor
            terminal.WriteCharacterAtCursor(character);
            
            // Assert - Verify character is traced with position information
            var traces = GetTracesFromDatabase();
            if (traces.Count != 1) return false;
            
            var trace = traces[0];
            var expectedDirection = "output"; // WriteCharacterAtCursor always uses Output direction
            
            // The trace should contain the character and position information
            // Format: "character at (row,col)" - cursor starts at (0,0)
            var expectedTrace = $"{character} at (0,0)";
            
            return trace.Printable == expectedTrace && 
                   trace.Direction == expectedDirection;
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