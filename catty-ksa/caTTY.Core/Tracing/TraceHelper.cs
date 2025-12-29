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
  public static void TraceEscapeBytes(ReadOnlySpan<byte> bytes)
  {
    if (!TerminalTracer.Enabled || bytes.IsEmpty)
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

    TerminalTracer.TraceEscape(sb.ToString());
  }

  /// <summary>
  /// Trace a single character as printable text.
  /// </summary>
  /// <param name="character">Character to trace</param>
  public static void TracePrintableChar(char character)
  {
    if (!TerminalTracer.Enabled)
      return;

    TerminalTracer.TracePrintable(character.ToString());
  }

  /// <summary>
  /// Trace a control character with its name for better readability.
  /// </summary>
  /// <param name="controlByte">Control character byte (0-31)</param>
  public static void TraceControlChar(byte controlByte)
  {
    if (!TerminalTracer.Enabled)
      return;
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

    TerminalTracer.TraceEscape($"<{controlName}>");
  }

  /// <summary>
  /// Trace a CSI sequence with parameters for better readability.
  /// </summary>
  /// <param name="command">CSI command character</param>
  /// <param name="parameters">CSI parameters (can be null)</param>
  /// <param name="prefix">CSI prefix character (can be null)</param>
  public static void TraceCsiSequence(char command, string? parameters = null, char? prefix = null)
  {
    if (!TerminalTracer.Enabled)
      return;
    var sequence = new StringBuilder("CSI");

    if (prefix.HasValue)
      sequence.Append($" {prefix}");

    if (!string.IsNullOrEmpty(parameters))
      sequence.Append($" {parameters}");

    sequence.Append($" {command}");

    TerminalTracer.TraceEscape(sequence.ToString());
  }

  /// <summary>
  /// Trace an OSC sequence with command and data.
  /// </summary>
  /// <param name="command">OSC command number</param>
  /// <param name="data">OSC data string</param>
  public static void TraceOscSequence(int command, string? data = null)
  {
    if (!TerminalTracer.Enabled)
      return;

    var sequence = $"OSC {command}";
    if (!string.IsNullOrEmpty(data))
      sequence += $" {data}";

    TerminalTracer.TraceEscape(sequence);
  }

  /// <summary>
  /// Trace an ESC sequence (non-CSI).
  /// </summary>
  /// <param name="sequence">The escape sequence characters after ESC</param>
  public static void TraceEscSequence(string sequence)
  {
    if (!TerminalTracer.Enabled)
      return;

    TerminalTracer.TraceEscape($"ESC {sequence}");
  }

  /// <summary>
  /// Trace UTF-8 decoded text as printable content.
  /// </summary>
  /// <param name="text">UTF-8 decoded text</param>
  public static void TraceUtf8Text(string text)
  {
    if (!TerminalTracer.Enabled || string.IsNullOrEmpty(text))
      return;

    TerminalTracer.TracePrintable(text);
  }

  /// <summary>
  /// Trace parser state transitions for debugging.
  /// </summary>
  /// <param name="fromState">Previous parser state</param>
  /// <param name="toState">New parser state</param>
  /// <param name="trigger">What triggered the transition</param>
  public static void TraceStateTransition(string fromState, string toState, string trigger)
  {
    if (!TerminalTracer.Enabled)
      return;

    TerminalTracer.TraceEscape($"STATE: {fromState} -> {toState} ({trigger})");
  }
}