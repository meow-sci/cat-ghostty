using System.Text;
using caTTY.Core.Terminal;
using caTTY.Core.Types;
using NUnit.Framework;

namespace caTTY.Core.Tests.Unit;

/// <summary>
///     Debugging tests for specific UTF-8 failure cases.
/// </summary>
[TestFixture]
[Category("Unit")]
public class Utf8SpecificDebuggingTests
{
    [Test]
    public void Debug_InvalidUtf8Bytes192_219()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        (int Row, int Col) initialCursor = (terminal.Cursor.Row, terminal.Cursor.Col);

        // Act - Process invalid UTF-8 bytes 192, 219 (0xC0, 0xDB)
        byte[] invalidSequence = { 192, 219 };
        terminal.Write(invalidSequence);

        // Debug output
        ICursor cursor = terminal.Cursor;
        (int Row, int Col) cursorAfterInvalid = (cursor.Row, cursor.Col); // Capture values, not reference
        TestContext.WriteLine($"Initial cursor: ({initialCursor.Row}, {initialCursor.Col})");
        TestContext.WriteLine($"Final cursor: ({cursorAfterInvalid.Row}, {cursorAfterInvalid.Col})");
        TestContext.WriteLine($"Bytes: {string.Join(", ", invalidSequence)}");
        TestContext.WriteLine(
            $"Cursor valid: {cursorAfterInvalid.Row >= 0 && cursorAfterInvalid.Row < terminal.Height && cursorAfterInvalid.Col >= 0 && cursorAfterInvalid.Col < terminal.Width}");

        // Try recovery
        terminal.Write("RECOVERY_TEST");
        ICursor recoveryCursor = terminal.Cursor;
        TestContext.WriteLine($"Recovery cursor: ({recoveryCursor.Row}, {recoveryCursor.Col})");
        TestContext.WriteLine(
            $"Detailed comparison: {recoveryCursor.Col} > {cursorAfterInvalid.Col} = {recoveryCursor.Col > cursorAfterInvalid.Col}");
        TestContext.WriteLine(
            $"Row comparison: {recoveryCursor.Row} > {cursorAfterInvalid.Row} = {recoveryCursor.Row > cursorAfterInvalid.Row}");
        TestContext.WriteLine(
            $"Recovery successful: {recoveryCursor.Col > cursorAfterInvalid.Col || recoveryCursor.Row > cursorAfterInvalid.Row}");

        // Assert - This should pass if the terminal handles invalid UTF-8 gracefully
        Assert.That(cursorAfterInvalid.Row >= 0 && cursorAfterInvalid.Row < terminal.Height, Is.True,
            "Cursor row should be valid");
        Assert.That(cursorAfterInvalid.Col >= 0 && cursorAfterInvalid.Col < terminal.Width, Is.True,
            "Cursor col should be valid");
        Assert.That(recoveryCursor.Col > cursorAfterInvalid.Col || recoveryCursor.Row > cursorAfterInvalid.Row, Is.True,
            "Recovery should work");
    }

    [Test]
    public void Debug_Utf8Byte192Analysis()
    {
        // Byte 192 (0xC0) is 11000000 in binary
        // This looks like a 2-byte UTF-8 start byte (110xxxxx pattern)
        // But 0xC0 is actually an invalid UTF-8 start byte (overlong encoding)

        TestContext.WriteLine($"Byte 192 (0xC0) binary: {Convert.ToString(192, 2).PadLeft(8, '0')}");
        TestContext.WriteLine($"Byte 219 (0xDB) binary: {Convert.ToString(219, 2).PadLeft(8, '0')}");

        // Check if 219 looks like a continuation byte
        TestContext.WriteLine($"219 & 0xC0 = {219 & 0xC0} (should be 128 for continuation byte)");
        TestContext.WriteLine($"Is 219 a valid continuation byte: {(219 & 0xC0) == 0x80}");

        // Try to decode with .NET
        try
        {
            string decoded = Encoding.UTF8.GetString(new byte[] { 192, 219 });
            TestContext.WriteLine($"UTF-8 decoded: '{decoded}' (length: {decoded.Length})");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"UTF-8 decoding failed: {ex.Message}");
        }
    }

    [Test]
    public void Debug_InvalidUtf8Byte243()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        (int Row, int Col) initialCursor = (terminal.Cursor.Row, terminal.Cursor.Col);

        // Act - Process byte 243 (0xF3) alone (incomplete 4-byte sequence)
        byte[] incompleteSequence = { 243 };
        terminal.Write(incompleteSequence);

        // Flush incomplete sequences to ensure they're handled
        terminal.FlushIncompleteSequences();

        // Debug output
        ICursor cursor = terminal.Cursor;
        (int Row, int Col) cursorAfterInvalid = (cursor.Row, cursor.Col);
        TestContext.WriteLine($"Initial cursor: ({initialCursor.Row}, {initialCursor.Col})");
        TestContext.WriteLine($"Final cursor: ({cursorAfterInvalid.Row}, {cursorAfterInvalid.Col})");
        TestContext.WriteLine($"Byte 243 (0xF3) binary: {Convert.ToString(243, 2).PadLeft(8, '0')}");
        TestContext.WriteLine($"Is 4-byte start: {(243 & 0xF8) == 0xF0}");
        TestContext.WriteLine(
            $"Cursor valid: {cursorAfterInvalid.Row >= 0 && cursorAfterInvalid.Row < terminal.Height && cursorAfterInvalid.Col >= 0 && cursorAfterInvalid.Col < terminal.Width}");

        // Try recovery
        terminal.Write("RECOVERY_TEST");
        ICursor recoveryCursor = terminal.Cursor;
        TestContext.WriteLine($"Recovery cursor: ({recoveryCursor.Row}, {recoveryCursor.Col})");
        TestContext.WriteLine(
            $"Recovery successful: {recoveryCursor.Col > cursorAfterInvalid.Col || recoveryCursor.Row > cursorAfterInvalid.Row}");

        // Assert - This should pass if the terminal handles incomplete UTF-8 gracefully
        Assert.That(cursorAfterInvalid.Row >= 0 && cursorAfterInvalid.Row < terminal.Height, Is.True,
            "Cursor row should be valid");
        Assert.That(cursorAfterInvalid.Col >= 0 && cursorAfterInvalid.Col < terminal.Width, Is.True,
            "Cursor col should be valid");
        Assert.That(recoveryCursor.Col > cursorAfterInvalid.Col || recoveryCursor.Row > cursorAfterInvalid.Row, Is.True,
            "Recovery should work");
    }

    [Test]
    public void Debug_InvalidUtf8Bytes192_133()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        (int Row, int Col) initialCursor = (terminal.Cursor.Row, terminal.Cursor.Col);

        // Act - Process invalid UTF-8 bytes 192, 133 (0xC0, 0x85)
        byte[] invalidSequence = { 192, 133 };
        terminal.Write(invalidSequence);
        terminal.FlushIncompleteSequences();

        // Debug output
        ICursor cursor = terminal.Cursor;
        (int Row, int Col) cursorAfterInvalid = (cursor.Row, cursor.Col);
        TestContext.WriteLine($"Initial cursor: ({initialCursor.Row}, {initialCursor.Col})");
        TestContext.WriteLine($"Final cursor: ({cursorAfterInvalid.Row}, {cursorAfterInvalid.Col})");
        TestContext.WriteLine($"Bytes: {string.Join(", ", invalidSequence)}");
        TestContext.WriteLine($"Byte 192 (0xC0) binary: {Convert.ToString(192, 2).PadLeft(8, '0')}");
        TestContext.WriteLine($"Byte 133 (0x85) binary: {Convert.ToString(133, 2).PadLeft(8, '0')}");
        TestContext.WriteLine($"Is 133 continuation: {(133 & 0xC0) == 0x80}");
        TestContext.WriteLine(
            $"Cursor valid: {cursorAfterInvalid.Row >= 0 && cursorAfterInvalid.Row < terminal.Height && cursorAfterInvalid.Col >= 0 && cursorAfterInvalid.Col < terminal.Width}");

        // Try recovery
        terminal.Write("RECOVERY_TEST");
        ICursor recoveryCursor = terminal.Cursor;
        TestContext.WriteLine($"Recovery cursor: ({recoveryCursor.Row}, {recoveryCursor.Col})");
        TestContext.WriteLine(
            $"Recovery successful: {recoveryCursor.Col > cursorAfterInvalid.Col || recoveryCursor.Row > cursorAfterInvalid.Row}");

        // Assert - This should pass if the terminal handles invalid UTF-8 gracefully
        Assert.That(cursorAfterInvalid.Row >= 0 && cursorAfterInvalid.Row < terminal.Height, Is.True,
            "Cursor row should be valid");
        Assert.That(cursorAfterInvalid.Col >= 0 && cursorAfterInvalid.Col < terminal.Width, Is.True,
            "Cursor col should be valid");
        Assert.That(recoveryCursor.Col > cursorAfterInvalid.Col || recoveryCursor.Row > cursorAfterInvalid.Row, Is.True,
            "Recovery should work");
    }

    [Test]
    public void Debug_InvalidUtf8Bytes158_65_66()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        (int Row, int Col) initialCursor = (terminal.Cursor.Row, terminal.Cursor.Col);

        // Act - Process bytes [158, 65, 66] (orphaned continuation + ASCII)
        byte[] sequence = { 158, 65, 66 };
        terminal.Write(sequence);
        terminal.FlushIncompleteSequences();

        // Debug output
        ICursor cursor = terminal.Cursor;
        (int Row, int Col) cursorAfterSequence = (cursor.Row, cursor.Col);
        TestContext.WriteLine($"Initial cursor: ({initialCursor.Row}, {initialCursor.Col})");
        TestContext.WriteLine($"Final cursor: ({cursorAfterSequence.Row}, {cursorAfterSequence.Col})");
        TestContext.WriteLine($"Bytes: {string.Join(", ", sequence)}");
        TestContext.WriteLine($"Byte 158 (0x9E) binary: {Convert.ToString(158, 2).PadLeft(8, '0')}");
        TestContext.WriteLine($"Is 158 continuation: {(158 & 0xC0) == 0x80}");
        TestContext.WriteLine("Expected: 3 characters (158 as invalid + A + B)");
        TestContext.WriteLine(
            $"Cursor valid: {cursorAfterSequence.Row >= 0 && cursorAfterSequence.Row < terminal.Height && cursorAfterSequence.Col >= 0 && cursorAfterSequence.Col < terminal.Width}");

        // Try recovery
        terminal.Write("RECOVERY_TEST");
        ICursor recoveryCursor = terminal.Cursor;
        TestContext.WriteLine($"Recovery cursor: ({recoveryCursor.Row}, {recoveryCursor.Col})");
        TestContext.WriteLine(
            $"Recovery successful: {recoveryCursor.Col > cursorAfterSequence.Col || recoveryCursor.Row > cursorAfterSequence.Row}");

        // Assert - This should pass if the terminal handles invalid UTF-8 gracefully
        Assert.That(cursorAfterSequence.Row >= 0 && cursorAfterSequence.Row < terminal.Height, Is.True,
            "Cursor row should be valid");
        Assert.That(cursorAfterSequence.Col >= 0 && cursorAfterSequence.Col < terminal.Width, Is.True,
            "Cursor col should be valid");
        Assert.That(recoveryCursor.Col > cursorAfterSequence.Col || recoveryCursor.Row > cursorAfterSequence.Row,
            Is.True, "Recovery should work");
    }

    [Test]
    public void Debug_InvalidUtf8Bytes192_176()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        (int Row, int Col) initialCursor = (terminal.Cursor.Row, terminal.Cursor.Col);

        // Act - Process bytes [192, 176] (0xC0, 0xB0)
        byte[] sequence = { 192, 176 };
        terminal.Write(sequence);
        terminal.FlushIncompleteSequences();

        // Debug output
        ICursor cursor = terminal.Cursor;
        (int Row, int Col) cursorAfterSequence = (cursor.Row, cursor.Col);
        TestContext.WriteLine($"Initial cursor: ({initialCursor.Row}, {initialCursor.Col})");
        TestContext.WriteLine($"Final cursor: ({cursorAfterSequence.Row}, {cursorAfterSequence.Col})");
        TestContext.WriteLine($"Bytes: {string.Join(", ", sequence)}");
        TestContext.WriteLine($"Byte 192 (0xC0) binary: {Convert.ToString(192, 2).PadLeft(8, '0')}");
        TestContext.WriteLine($"Byte 176 (0xB0) binary: {Convert.ToString(176, 2).PadLeft(8, '0')}");
        TestContext.WriteLine($"Is 176 continuation: {(176 & 0xC0) == 0x80}");

        // Try recovery
        (int Row, int Col) beforeRecovery = (cursor.Row, cursor.Col);
        terminal.Write("RECOVERY_TEST");
        ICursor recoveryCursor = terminal.Cursor;
        TestContext.WriteLine($"Before recovery: ({beforeRecovery.Row}, {beforeRecovery.Col})");
        TestContext.WriteLine($"Recovery cursor: ({recoveryCursor.Row}, {recoveryCursor.Col})");
        TestContext.WriteLine(
            $"Recovery successful: {recoveryCursor.Col > beforeRecovery.Col || recoveryCursor.Row > beforeRecovery.Row}");

        // Assert - This should pass if the terminal handles invalid UTF-8 gracefully
        Assert.That(cursorAfterSequence.Row >= 0 && cursorAfterSequence.Row < terminal.Height, Is.True,
            "Cursor row should be valid");
        Assert.That(cursorAfterSequence.Col >= 0 && cursorAfterSequence.Col < terminal.Width, Is.True,
            "Cursor col should be valid");
        Assert.That(recoveryCursor.Col > beforeRecovery.Col || recoveryCursor.Row > beforeRecovery.Row, Is.True,
            "Recovery should work");
    }

    [Test]
    public void Debug_InvalidUtf8Byte194()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        (int Row, int Col) initialCursor = (terminal.Cursor.Row, terminal.Cursor.Col);

        // Act - Process byte 194 (0xC2) alone (incomplete 2-byte sequence)
        byte[] incompleteSequence = { 194 };
        terminal.Write(incompleteSequence);
        terminal.FlushIncompleteSequences();

        // Debug output
        ICursor cursor = terminal.Cursor;
        (int Row, int Col) cursorAfterSequence = (cursor.Row, cursor.Col);
        TestContext.WriteLine($"Initial cursor: ({initialCursor.Row}, {initialCursor.Col})");
        TestContext.WriteLine($"Final cursor: ({cursorAfterSequence.Row}, {cursorAfterSequence.Col})");
        TestContext.WriteLine($"Byte 194 (0xC2) binary: {Convert.ToString(194, 2).PadLeft(8, '0')}");
        TestContext.WriteLine($"Is 2-byte start: {(194 & 0xE0) == 0xC0}");
        TestContext.WriteLine(
            $"Cursor valid: {cursorAfterSequence.Row >= 0 && cursorAfterSequence.Row < terminal.Height && cursorAfterSequence.Col >= 0 && cursorAfterSequence.Col < terminal.Width}");

        // Try recovery
        (int Row, int Col) beforeRecovery = (cursor.Row, cursor.Col);
        terminal.Write("RECOVERY_TEST");
        ICursor recoveryCursor = terminal.Cursor;
        TestContext.WriteLine($"Before recovery: ({beforeRecovery.Row}, {beforeRecovery.Col})");
        TestContext.WriteLine($"Recovery cursor: ({recoveryCursor.Row}, {recoveryCursor.Col})");
        TestContext.WriteLine(
            $"Recovery successful: {recoveryCursor.Col > beforeRecovery.Col || recoveryCursor.Row > beforeRecovery.Row}");

        // Assert - This should pass if the terminal handles incomplete UTF-8 gracefully
        Assert.That(cursorAfterSequence.Row >= 0 && cursorAfterSequence.Row < terminal.Height, Is.True,
            "Cursor row should be valid");
        Assert.That(cursorAfterSequence.Col >= 0 && cursorAfterSequence.Col < terminal.Width, Is.True,
            "Cursor col should be valid");
        Assert.That(recoveryCursor.Col > beforeRecovery.Col || recoveryCursor.Row > beforeRecovery.Row, Is.True,
            "Recovery should work");
    }

    [Test]
    public void Debug_PropertyTestScenario_Byte194()
    {
        // Arrange - Replicate the exact property test scenario
        var terminal = new TerminalEmulator(80, 24);
        byte[] invalidSequence = { 194 }; // This is what the property test generates

        // Act - Process invalid UTF-8 sequence (exactly like property test)
        terminal.Write(invalidSequence);

        // Flush any incomplete sequences to ensure they're handled
        terminal.FlushIncompleteSequences();

        // Assert - Terminal should remain functional (exactly like property test)
        ICursor cursor = terminal.Cursor;
        (int Row, int Col) cursorAfterFlush = (cursor.Row, cursor.Col); // Capture values, not reference
        bool cursorValid = cursorAfterFlush.Row >= 0 && cursorAfterFlush.Row < terminal.Height &&
                           cursorAfterFlush.Col >= 0 && cursorAfterFlush.Col < terminal.Width;

        TestContext.WriteLine($"After flush cursor: ({cursorAfterFlush.Row}, {cursorAfterFlush.Col})");
        TestContext.WriteLine($"Cursor valid: {cursorValid}");

        // Verify we can still write valid content (exactly like property test)
        terminal.Write("RECOVERY_TEST");
        ICursor finalCursor = terminal.Cursor;
        (int Row, int Col) finalCursorValues = (finalCursor.Row, finalCursor.Col); // Capture values, not reference

        // Debug the comparison in detail
        TestContext.WriteLine($"Final cursor: ({finalCursorValues.Row}, {finalCursorValues.Col})");
        TestContext.WriteLine(
            $"Cursor col comparison: {finalCursorValues.Col} > {cursorAfterFlush.Col} = {finalCursorValues.Col > cursorAfterFlush.Col}");
        TestContext.WriteLine(
            $"Cursor row comparison: {finalCursorValues.Row} > {cursorAfterFlush.Row} = {finalCursorValues.Row > cursorAfterFlush.Row}");

        bool recoverySuccessful =
            finalCursorValues.Col > cursorAfterFlush.Col || finalCursorValues.Row > cursorAfterFlush.Row;

        TestContext.WriteLine($"Final cursor: ({finalCursor.Row}, {finalCursor.Col})");
        TestContext.WriteLine($"Recovery successful: {recoverySuccessful}");
        TestContext.WriteLine($"Overall result: {cursorValid && recoverySuccessful}");

        // This should match the property test logic exactly
        Assert.That(cursorValid, Is.True, "Cursor should be valid");
        Assert.That(recoverySuccessful, Is.True, "Recovery should be successful");
        Assert.That(cursorValid && recoverySuccessful, Is.True, "Property test should pass");
    }
}
