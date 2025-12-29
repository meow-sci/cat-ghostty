using System;
using System.Text;

namespace caTTY.Core.Tracing;

/// <summary>
/// Helper methods for terminal tracing with convenient overloads.
/// </summary>
public static class TraceHelper
{
  /// <summary>
  /// Trace raw bytes as an escape sequence (for debugging parser input).
  /// </summary>
  /// <param name="bytes">Raw bytes to trace as escape sequence</param>
  /// <param name="direction">The direction of data flow (default: Output)</param>
  /// <param name="row">The cursor row position (0-based, nullable)</param>
  /// <param name="col">The cursor column position (0-based, nullable)</param>
  public static void TraceEscapeBytes(ReadOnlySpan<byte> bytes, TraceDirection direction = TraceDirection.Output, int? row = null, int? col = null)
  {
    if (bytes.IsEmpty)
      return;

    var sb = new StringBuilder();
    foreach (var b in bytes)
    {
      if (b >= 32 && b <= 126) // Printable ASCII
      {
        sb.Append((char)b);
      }
      else
      {
        sb.Append($"\\x{b:X2}");
      }
    }

    TerminalTracer.TraceEscape(sb.ToString(), direction, row, col);
  }

  /// <summary>
  /// Trace a single character as printable text.
  /// </summary>
  /// <param name="character">Character to trace</param>
  /// <param name="direction">The direction of data flow (default: Output)</param>
  /// <param name="row">The cursor row position (0-based, nullable)</param>
  /// <param name="col">The cursor column position (0-based, nullable)</param>
  public static void TracePrintableChar(char character, TraceDirection direction = TraceDirection.Output, int? row = null, int? col = null)
  {
    TerminalTracer.TracePrintable(character.ToString(), direction, row, col);
  }

  /// <summary>
  /// Trace a control character with its name for better readability.
  /// </summary>
  /// <param name="controlByte">Control character byte (0-31)</param>
  /// <param name="direction">The direction of data flow (default: Output)</param>
  /// <param name="row">The cursor row position (0-based, nullable)</param>
  /// <param name="col">The cursor column position (0-based, nullable)</param>
  public static void TraceControlChar(byte controlByte, TraceDirection direction = TraceDirection.Output, int? row = null, int? col = null)
  {
    var controlName = controlByte switch
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

    TerminalTracer.TraceEscape($"<{controlName}>", direction, row, col);
  }

  /// <summary>
  /// Trace a CSI sequence with parameters for better readability.
  /// </summary>
  /// <param name="command">CSI command character</param>
  /// <param name="parameters">CSI parameters (can be null)</param>
  /// <param name="prefix">CSI prefix character (can be null)</param>
  /// <param name="direction">The direction of data flow (default: Output)</param>
  /// <param name="row">The cursor row position (0-based, nullable)</param>
  /// <param name="col">The cursor column position (0-based, nullable)</param>
  public static void TraceCsiSequence(char command, string? parameters = null, char? prefix = null, TraceDirection direction = TraceDirection.Output, int? row = null, int? col = null)
  {
    // Format as human-readable escape sequence: ESC[params;command
    var sequence = new StringBuilder("ESC[");

    if (prefix.HasValue)
      sequence.Append(prefix);

    if (!string.IsNullOrEmpty(parameters))
      sequence.Append(parameters);

    sequence.Append(command);

    TerminalTracer.TraceEscape(sequence.ToString(), direction, row, col);
  }

  /// <summary>
  /// Trace an OSC sequence with command and data.
  /// </summary>
  /// <param name="command">OSC command number</param>
  /// <param name="data">OSC data string</param>
  /// <param name="direction">The direction of data flow (default: Output)</param>
  /// <param name="row">The cursor row position (0-based, nullable)</param>
  /// <param name="col">The cursor column position (0-based, nullable)</param>
  public static void TraceOscSequence(int command, string? data = null, TraceDirection direction = TraceDirection.Output, int? row = null, int? col = null)
  {
    // Format as human-readable escape sequence: ESC]command;data\x07 or ESC]command;data\x1b\
    var sequence = $"ESC]{command}";
    if (!string.IsNullOrEmpty(data))
      sequence += $";{data}";
    sequence += "\\x07"; // Show BEL terminator

    TerminalTracer.TraceEscape(sequence, direction, row, col);
  }

  /// <summary>
  /// Trace an ESC sequence (non-CSI).
  /// </summary>
  /// <param name="sequence">The escape sequence characters after ESC</param>
  /// <param name="direction">The direction of data flow (default: Output)</param>
  /// <param name="row">The cursor row position (0-based, nullable)</param>
  /// <param name="col">The cursor column position (0-based, nullable)</param>
  public static void TraceEscSequence(string sequence, TraceDirection direction = TraceDirection.Output, int? row = null, int? col = null)
  {
    // Format as human-readable escape sequence: ESC + sequence
    TerminalTracer.TraceEscape($"ESC{sequence}", direction, row, col);
  }

  /// <summary>
  /// Trace a DCS sequence with command, parameters, and data.
  /// </summary>
  /// <param name="command">DCS command string</param>
  /// <param name="parameters">DCS parameters (can be null)</param>
  /// <param name="data">DCS data payload (can be null)</param>
  /// <param name="direction">The direction of data flow (default: Output)</param>
  /// <param name="row">The cursor row position (0-based, nullable)</param>
  /// <param name="col">The cursor column position (0-based, nullable)</param>
  public static void TraceDcsSequence(string command, string? parameters = null, string? data = null, TraceDirection direction = TraceDirection.Output, int? row = null, int? col = null)
  {
    // Format as human-readable escape sequence: ESCP + parameters + command + data + ESC\
    var sequence = new StringBuilder("ESCP");

    if (!string.IsNullOrEmpty(parameters))
      sequence.Append(parameters);

    sequence.Append(command);

    if (!string.IsNullOrEmpty(data))
      sequence.Append(data);
    
    sequence.Append("ESC\\"); // Show ST terminator

    TerminalTracer.TraceEscape(sequence.ToString(), direction, row, col);
  }

  /// <summary>
  /// Trace UTF-8 decoded text as printable content.
  /// </summary>
  /// <param name="text">UTF-8 decoded text</param>
  /// <param name="direction">The direction of data flow (default: Output)</param>
  /// <param name="row">The cursor row position (0-based, nullable)</param>
  /// <param name="col">The cursor column position (0-based, nullable)</param>
  public static void TraceUtf8Text(string text, TraceDirection direction = TraceDirection.Output, int? row = null, int? col = null)
  {
    if (string.IsNullOrEmpty(text))
      return;

    TerminalTracer.TracePrintable(text, direction, row, col);
  }

  /// <summary>
  /// Trace a wide character with width indication for better debugging.
  /// </summary>
  /// <param name="character">Wide character to trace</param>
  /// <param name="direction">The direction of data flow (default: Output)</param>
  /// <param name="row">The cursor row position (0-based, nullable)</param>
  /// <param name="col">The cursor column position (0-based, nullable)</param>
  public static void TraceWideCharacter(char character, TraceDirection direction = TraceDirection.Output, int? row = null, int? col = null)
  {
    TerminalTracer.TracePrintable($"{character} (wide)", direction, row, col);
  }

  /// <summary>
  /// Trace parser state transitions for debugging.
  /// </summary>
  /// <param name="fromState">Previous parser state</param>
  /// <param name="toState">New parser state</param>
  /// <param name="trigger">What triggered the transition</param>
  /// <param name="direction">The direction of data flow (default: Output)</param>
  /// <param name="row">The cursor row position (0-based, nullable)</param>
  /// <param name="col">The cursor column position (0-based, nullable)</param>
  public static void TraceStateTransition(string fromState, string toState, string trigger, TraceDirection direction = TraceDirection.Output, int? row = null, int? col = null)
  {
    TerminalTracer.TraceEscape($"STATE: {fromState} -> {toState} ({trigger})", direction, row, col);
  }
}