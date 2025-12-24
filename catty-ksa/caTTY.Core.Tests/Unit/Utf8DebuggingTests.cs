using NUnit.Framework;
using caTTY.Core.Terminal;
using System.Text;

namespace caTTY.Core.Tests.Unit;

/// <summary>
/// Debugging tests to understand UTF-8 handling issues.
/// </summary>
[TestFixture]
[Category("Unit")]
public class Utf8DebuggingTests
{
    [Test]
    public void Debug_InvalidUtf8Byte153()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        var initialCursor = (Row: terminal.Cursor.Row, Col: terminal.Cursor.Col);

        // Act - Process invalid UTF-8 byte 153 (0x99)
        byte[] invalidSequence = { 153 };
        terminal.Write(invalidSequence);

        // Debug output
        var cursor = terminal.Cursor;
        TestContext.WriteLine($"Initial cursor: ({initialCursor.Row}, {initialCursor.Col})");
        TestContext.WriteLine($"Final cursor: ({cursor.Row}, {cursor.Col})");
        TestContext.WriteLine($"Cursor valid: {cursor.Row >= 0 && cursor.Row < terminal.Height && cursor.Col >= 0 && cursor.Col < terminal.Width}");

        // Try recovery
        terminal.Write("RECOVERY_TEST");
        var recoveryCursor = terminal.Cursor;
        TestContext.WriteLine($"Recovery cursor: ({recoveryCursor.Row}, {recoveryCursor.Col})");
        TestContext.WriteLine($"Recovery successful: {recoveryCursor.Col > cursor.Col || recoveryCursor.Row > cursor.Row}");

        // Assert - This should pass if the terminal handles invalid UTF-8 gracefully
        Assert.That(cursor.Row >= 0 && cursor.Row < terminal.Height, Is.True, "Cursor row should be valid");
        Assert.That(cursor.Col >= 0 && cursor.Col < terminal.Width, Is.True, "Cursor col should be valid");
    }

    [Test]
    public void Debug_MixedUtf8AndControl()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        var initialCursor = (Row: terminal.Cursor.Row, Col: terminal.Cursor.Col);

        // Act - Process "Hello Worldpiñata"
        string mixedContent = "Hello Worldpiñata";
        terminal.Write(mixedContent);

        // Debug output
        var cursor = terminal.Cursor;
        TestContext.WriteLine($"Initial cursor: ({initialCursor.Row}, {initialCursor.Col})");
        TestContext.WriteLine($"Final cursor: ({cursor.Row}, {cursor.Col})");
        TestContext.WriteLine($"Content length: {mixedContent.Length}");
        TestContext.WriteLine($"UTF-8 bytes: {string.Join(", ", Encoding.UTF8.GetBytes(mixedContent))}");

        // Try UTF-8 checkmark
        var testPos = (Row: cursor.Row, Col: cursor.Col);
        terminal.Write("✓");
        var newCursor = terminal.Cursor;
        TestContext.WriteLine($"Test cursor: ({newCursor.Row}, {newCursor.Col})");
        TestContext.WriteLine($"UTF-8 still works: {newCursor.Col > testPos.Col || newCursor.Row > testPos.Row}");

        // Assert
        Assert.That(cursor.Row >= 0 && cursor.Row < terminal.Height, Is.True, "Cursor row should be valid");
        Assert.That(cursor.Col >= 0 && cursor.Col < terminal.Width, Is.True, "Cursor col should be valid");
    }

    [Test]
    public void Debug_WideCharacter()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        var initialCursor = (Row: terminal.Cursor.Row, Col: terminal.Cursor.Col);

        // Act - Process Chinese character "好"
        string wideChar = "好";
        terminal.Write(wideChar);

        // Debug output
        var cursor = terminal.Cursor;
        TestContext.WriteLine($"Initial cursor: ({initialCursor.Row}, {initialCursor.Col})");
        TestContext.WriteLine($"Final cursor: ({cursor.Row}, {cursor.Col})");
        TestContext.WriteLine($"Character: {wideChar}");
        TestContext.WriteLine($"UTF-8 bytes: {string.Join(", ", Encoding.UTF8.GetBytes(wideChar))}");
        TestContext.WriteLine($"Cursor advanced: {cursor.Col > initialCursor.Col || cursor.Row > initialCursor.Row}");

        // Test functionality
        terminal.Write("X");
        var testCursor = terminal.Cursor;
        TestContext.WriteLine($"Test cursor: ({testCursor.Row}, {testCursor.Col})");
        TestContext.WriteLine($"Terminal functional: {testCursor.Row >= 0 && testCursor.Row < terminal.Height && testCursor.Col >= 0 && testCursor.Col < terminal.Width}");

        // Assert
        Assert.That(cursor.Col > initialCursor.Col || cursor.Row > initialCursor.Row, Is.True, "Cursor should advance for wide character");
    }
}