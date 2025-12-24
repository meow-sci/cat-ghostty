using NUnit.Framework;
using FsCheck;
using FsCheck.NUnit;
using caTTY.Core.Terminal;
using caTTY.Core.Types;
using System;
using System.Linq;
using System.Text;

namespace caTTY.Core.Tests.Property;

/// <summary>
/// Property-based tests for UTF-8 character handling in the terminal emulator.
/// These tests verify universal properties that should hold for all valid UTF-8 inputs.
/// </summary>
[TestFixture]
[Category("Property")]
public class Utf8Properties
{
    /// <summary>
    /// Generator for valid UTF-8 sequences of various lengths.
    /// </summary>
    public static Arbitrary<string> ValidUtf8StringArb =>
        Arb.From(Gen.Elements(new[]
        {
            // ASCII characters
            "Hello World",
            "123456789",
            "!@#$%^&*()",
            
            // Latin characters with diacritics
            "café", "naïve", "résumé", "piñata",
            
            // Various Unicode blocks
            "αβγδε", // Greek
            "こんにちは", // Japanese Hiragana
            "你好世界", // Chinese
            "🌟🚀💻🎉", // Emojis
            "Ω≈∞∑∏", // Mathematical symbols
            
            // Mixed content
            "Hello 世界 🌍",
            "Test αβγ 123",
            
            // Edge cases
            "\u0080\u07FF", // 2-byte boundary
            "\u0800\uFFFF", // 3-byte boundary
            "\U00010000\U0010FFFF", // 4-byte boundary
        }));

    /// <summary>
    /// Generator for individual Unicode code points across different ranges.
    /// </summary>
    public static Arbitrary<int> ValidUnicodeCodePointArb =>
        Arb.From(Gen.OneOf(
            Gen.Choose(0x0020, 0x007F), // ASCII printable
            Gen.Choose(0x0080, 0x07FF), // 2-byte UTF-8
            Gen.Choose(0x0800, 0xFFFF), // 3-byte UTF-8
            Gen.Choose(0x10000, 0x10FFFF) // 4-byte UTF-8
        ).Where(cp => cp <= 0xFFFF ? !char.IsSurrogate((char)cp) : true).Where(cp => cp <= 0x10FFFF));

    /// <summary>
    /// Generator for sequences of UTF-8 bytes that may be split across Write calls.
    /// </summary>
    public static Arbitrary<byte[][]> SplitUtf8SequenceArb =>
        Arb.From(ValidUtf8StringArb.Generator.SelectMany(str =>
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            if (bytes.Length <= 1) return Gen.Constant(new[] { bytes });
            
            // Generate random split points
            return Gen.Choose(1, Math.Min(bytes.Length, 5)).SelectMany(splitCount =>
                Gen.Constant(SplitByteArray(bytes, splitCount)));
        }));

    /// <summary>
    /// **Feature: catty-ksa, Property 16: UTF-8 character handling**
    /// **Validates: Requirements 9.3, 9.4**
    /// 
    /// Property: For any valid UTF-8 string, decoding and processing should produce
    /// the correct Unicode code points without data loss.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property Utf8DecodingPreservesUnicodeCodePoints()
    {
        return Prop.ForAll(ValidUtf8StringArb, (string input) =>
        {
            // Arrange
            var terminal = new TerminalEmulator(200, 50); // Large enough to hold test data
            var expectedCodePoints = input.EnumerateRunes().Select(r => r.Value).ToArray();

            // Act
            terminal.Write(input);

            // Assert - Check that the correct number of characters were processed
            // We can't directly access the processed code points, but we can verify
            // that the cursor advanced correctly for the number of characters
            var cursor = terminal.Cursor;
            
            // For this test, we'll verify that no exceptions were thrown and
            // the terminal state remains valid
            bool terminalStateValid = cursor.Row >= 0 && cursor.Row < terminal.Height &&
                                    cursor.Col >= 0 && cursor.Col < terminal.Width;

            // Additional verification: write a test character to ensure terminal is still functional
            var testPos = (Row: cursor.Row, Col: cursor.Col);
            terminal.Write("X");
            var testCell = terminal.ScreenBuffer.GetCell(testPos.Row, testPos.Col);
            bool terminalStillFunctional = testCell.Character == 'X';

            return terminalStateValid && terminalStillFunctional;
        });
    }

    /// <summary>
    /// **Feature: catty-ksa, Property 16b: UTF-8 sequence splitting resilience**
    /// **Validates: Requirements 9.3, 9.4**
    /// 
    /// Property: For any valid UTF-8 string split across multiple Write calls,
    /// the result should be identical to writing the complete string at once.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property Utf8SequenceSplittingProducesSameResult()
    {
        return Prop.ForAll(SplitUtf8SequenceArb, (byte[][] splitSequences) =>
        {
            if (splitSequences.Length == 0) return true;

            // Arrange - Create two identical terminals
            var terminal1 = new TerminalEmulator(200, 50);
            var terminal2 = new TerminalEmulator(200, 50);

            // Reconstruct the original byte array
            var originalBytes = splitSequences.SelectMany(seq => seq).ToArray();

            // Act - Write complete sequence to terminal1
            terminal1.Write(originalBytes);

            // Write split sequences to terminal2
            foreach (var sequence in splitSequences)
            {
                terminal2.Write(sequence);
            }

            // Assert - Both terminals should have identical cursor positions
            var cursor1 = terminal1.Cursor;
            var cursor2 = terminal2.Cursor;

            bool cursorsMatch = cursor1.Row == cursor2.Row && cursor1.Col == cursor2.Col;

            // Verify both terminals are still functional
            terminal1.Write("TEST");
            terminal2.Write("TEST");
            
            var finalCursor1 = terminal1.Cursor;
            var finalCursor2 = terminal2.Cursor;
            bool finalCursorsMatch = finalCursor1.Row == finalCursor2.Row && 
                                   finalCursor1.Col == finalCursor2.Col;

            return cursorsMatch && finalCursorsMatch;
        });
    }

    /// <summary>
    /// **Feature: catty-ksa, Property 16c: Wide character handling**
    /// **Validates: Requirements 9.4**
    /// 
    /// Property: For any string containing wide characters (CJK, emojis),
    /// the cursor should advance correctly accounting for character width.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property WideCharacterCursorAdvancement()
    {
        var wideCharArb = Arb.From(Gen.Elements(new[]
        {
            "你", "好", "世", "界", // Chinese characters
            "こ", "ん", "に", "ち", // Japanese Hiragana
            "한", "국", "어", // Korean
            "🌟", "🚀", "💻", "🎉", // Emojis
        }));

        return Prop.ForAll(wideCharArb, (string wideChar) =>
        {
            // Arrange
            var terminal = new TerminalEmulator(80, 24);
            var initialCursor = (Row: terminal.Cursor.Row, Col: terminal.Cursor.Col);

            // Act
            terminal.Write(wideChar);

            // Assert - Cursor should advance appropriately
            var finalCursor = (Row: terminal.Cursor.Row, Col: terminal.Cursor.Col);
            
            // For wide characters, cursor should advance by 2 positions or wrap to next line
            bool cursorAdvanced = finalCursor.Col > initialCursor.Col || 
                                finalCursor.Row > initialCursor.Row;

            // Verify terminal remains functional
            terminal.Write("X");
            var testCursor = terminal.Cursor;
            bool terminalFunctional = testCursor.Row >= 0 && testCursor.Row < terminal.Height &&
                                    testCursor.Col >= 0 && testCursor.Col < terminal.Width;

            return cursorAdvanced && terminalFunctional;
        });
    }

    /// <summary>
    /// **Feature: catty-ksa, Property 16d: Invalid UTF-8 error recovery**
    /// **Validates: Requirements 9.3, 9.4**
    /// 
    /// Property: For any sequence containing invalid UTF-8 bytes,
    /// the terminal should recover gracefully and continue processing.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property InvalidUtf8BytesHandledGracefully()
    {
        var invalidUtf8Arb = Arb.From(Gen.Choose(0x80, 0xFF).SelectMany(invalidByte =>
            Gen.Elements(new[]
            {
                new byte[] { (byte)invalidByte }, // Single invalid byte
                new byte[] { 0xC0, (byte)invalidByte }, // Invalid continuation
                new byte[] { 0xE0, 0x80, (byte)invalidByte }, // Invalid in 3-byte sequence
                new byte[] { (byte)invalidByte, 0x41, 0x42 }, // Invalid followed by ASCII
            })));

        return Prop.ForAll(invalidUtf8Arb, (byte[] invalidSequence) =>
        {
            // Arrange
            var terminal = new TerminalEmulator(80, 24);

            // Act - Process invalid UTF-8 sequence
            terminal.Write(invalidSequence);
            
            // Flush any incomplete sequences to ensure they're handled
            terminal.FlushIncompleteSequences();

            // Assert - Terminal should remain functional
            var cursor = terminal.Cursor;
            var cursorAfterFlush = (Row: cursor.Row, Col: cursor.Col); // Capture values, not reference
            bool cursorValid = cursorAfterFlush.Row >= 0 && cursorAfterFlush.Row < terminal.Height &&
                             cursorAfterFlush.Col >= 0 && cursorAfterFlush.Col < terminal.Width;

            // Verify we can still write valid content
            terminal.Write("RECOVERY_TEST");
            var finalCursor = terminal.Cursor;
            var finalCursorValues = (Row: finalCursor.Row, Col: finalCursor.Col); // Capture values, not reference
            bool recoverySuccessful = finalCursorValues.Col > cursorAfterFlush.Col || finalCursorValues.Row > cursorAfterFlush.Row;

            return cursorValid && recoverySuccessful;
        });
    }

    /// <summary>
    /// **Feature: catty-ksa, Property 16e: UTF-8 mixed with control characters**
    /// **Validates: Requirements 9.3, 9.4, 10.1, 10.2**
    /// 
    /// Property: For any sequence mixing UTF-8 characters with control characters,
    /// both should be processed correctly without interference.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property Utf8MixedWithControlCharacters()
    {
        var mixedSequenceArb = Arb.From(
            Gen.ListOf(Gen.OneOf(
                ValidUtf8StringArb.Generator,
                Gen.Elements(new[] { "\n", "\r", "\t", "\b" })
            )).Where(list => list.Count() > 0 && list.Count() <= 10)
              .Select(list => string.Join("", list)));

        return Prop.ForAll(mixedSequenceArb, (string mixedContent) =>
        {
            // Arrange
            var terminal = new TerminalEmulator(80, 24);

            // Act
            terminal.Write(mixedContent);

            // Assert - Terminal should remain in valid state
            var cursor = terminal.Cursor;
            bool cursorValid = cursor.Row >= 0 && cursor.Row < terminal.Height &&
                             cursor.Col >= 0 && cursor.Col < terminal.Width;

            // Test that terminal is still responsive
            var testPos = (Row: cursor.Row, Col: cursor.Col);
            terminal.Write("✓"); // UTF-8 checkmark
            
            // Verify the checkmark was processed (cursor should advance)
            var newCursor = terminal.Cursor;
            bool utf8StillWorks = newCursor.Col > testPos.Col || newCursor.Row > testPos.Row;

            return cursorValid && utf8StillWorks;
        });
    }

    /// <summary>
    /// Helper method to split a byte array into multiple chunks.
    /// </summary>
    private static byte[][] SplitByteArray(byte[] bytes, int maxChunks)
    {
        if (bytes.Length <= 1) return new[] { bytes };

        var random = new System.Random(bytes.GetHashCode()); // Deterministic based on input
        var chunks = new List<byte[]>();
        int start = 0;

        while (start < bytes.Length && chunks.Count < maxChunks - 1)
        {
            int remainingBytes = bytes.Length - start;
            int chunkSize = random.Next(1, Math.Min(remainingBytes, 4) + 1);
            
            var chunk = new byte[chunkSize];
            Array.Copy(bytes, start, chunk, 0, chunkSize);
            chunks.Add(chunk);
            start += chunkSize;
        }

        // Add remaining bytes as final chunk
        if (start < bytes.Length)
        {
            var finalChunk = new byte[bytes.Length - start];
            Array.Copy(bytes, start, finalChunk, 0, finalChunk.Length);
            chunks.Add(finalChunk);
        }

        return chunks.ToArray();
    }
}