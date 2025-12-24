using NUnit.Framework;
using caTTY.Core.Terminal;
using caTTY.Core.Types;
using Microsoft.Extensions.Logging.Abstractions;

namespace caTTY.Core.Tests.Unit.Terminal;

/// <summary>
/// Tests for ESC sequence handling in the terminal emulator.
/// Validates the implementation of task 2.11 - essential ESC sequences.
/// </summary>
[TestFixture]
public class EscSequenceTests
{
    private TerminalEmulator _terminal = null!;

    [SetUp]
    public void SetUp()
    {
        _terminal = new TerminalEmulator(80, 24, NullLogger.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _terminal.Dispose();
    }

    [Test]
    public void SaveAndRestoreCursor_ShouldPreserveCursorPosition()
    {
        // Arrange: Move cursor to a specific position
        var csiH = new byte[] { 0x1b, 0x5b, 0x35, 0x3b, 0x31, 0x30, 0x48 }; // ESC[5;10H
        _terminal.Write(csiH.AsSpan());
        var initialRow = _terminal.Cursor.Row;
        var initialCol = _terminal.Cursor.Col;

        // Act: Save cursor, move to different position, then restore
        var esc7 = new byte[] { 0x1b, 0x37 }; // ESC 7
        _terminal.Write(esc7.AsSpan());
        var csiH2 = new byte[] { 0x1b, 0x5b, 0x31, 0x3b, 0x31, 0x48 }; // ESC[1;1H
        _terminal.Write(csiH2.AsSpan());
        var esc8 = new byte[] { 0x1b, 0x38 }; // ESC 8
        _terminal.Write(esc8.AsSpan());

        // Assert: Cursor should be back to original position
        Assert.That(_terminal.Cursor.Row, Is.EqualTo(initialRow), "Cursor row should be restored");
        Assert.That(_terminal.Cursor.Col, Is.EqualTo(initialCol), "Cursor column should be restored");
    }

    [Test]
    public void RestoreCursor_WithoutSave_ShouldNotCrash()
    {
        // Arrange: Start with cursor at origin
        var initialRow = _terminal.Cursor.Row;
        var initialCol = _terminal.Cursor.Col;

        // Act: Try to restore cursor without saving first
        var esc8 = new byte[] { 0x1b, 0x38 }; // ESC 8
        _terminal.Write(esc8.AsSpan());

        // Assert: Should not crash and cursor should remain unchanged
        Assert.That(_terminal.Cursor.Row, Is.EqualTo(initialRow), "Cursor row should be unchanged");
        Assert.That(_terminal.Cursor.Col, Is.EqualTo(initialCol), "Cursor column should be unchanged");
    }

    [Test]
    public void Index_ShouldMoveCursorDownOneLine()
    {
        // Arrange: Start at origin
        var initialRow = _terminal.Cursor.Row;

        // Act: Send index sequence
        var escD = new byte[] { 0x1b, 0x44 }; // ESC D
        _terminal.Write(escD.AsSpan());

        // Assert: Cursor should move down one line
        Assert.That(_terminal.Cursor.Row, Is.EqualTo(initialRow + 1), "Cursor should move down one line");
    }

    [Test]
    public void NextLine_ShouldMoveCursorToBeginningOfNextLine()
    {
        // Arrange: Move cursor to middle of a line
        var csiH = new byte[] { 0x1b, 0x5b, 0x35, 0x3b, 0x31, 0x30, 0x48 }; // ESC[5;10H
        _terminal.Write(csiH.AsSpan());

        // Act: Send next line sequence
        var escE = new byte[] { 0x1b, 0x45 }; // ESC E
        _terminal.Write(escE.AsSpan());

        // Assert: Cursor should be at beginning of next line
        Assert.That(_terminal.Cursor.Row, Is.EqualTo(5), "Cursor should be on next line (row 5, 0-based)");
        Assert.That(_terminal.Cursor.Col, Is.EqualTo(0), "Cursor should be at beginning of line");
    }

    [Test]
    public void HorizontalTabSet_ShouldSetTabStopAtCurrentPosition()
    {
        // Arrange: Move cursor to column 3 (like the TypeScript test)
        var csiH = new byte[] { 0x1b, 0x5b, 0x31, 0x3b, 0x34, 0x48 }; // ESC[1;4H
        _terminal.Write(csiH.AsSpan());

        // Act: Set tab stop at current position
        var escH = new byte[] { 0x1b, 0x48 }; // ESC H
        _terminal.Write(escH.AsSpan());

        // Move to beginning and tab forward
        var csiH2 = new byte[] { 0x1b, 0x5b, 0x31, 0x3b, 0x31, 0x48 }; // ESC[1;1H
        _terminal.Write(csiH2.AsSpan());
        var tab = new byte[] { 0x09 }; // Tab
        _terminal.Write(tab.AsSpan());

        // Assert: Should tab to the set tab stop position (3, 0-based)
        Assert.That(_terminal.Cursor.Col, Is.EqualTo(3), "Cursor should tab to the set tab stop position");
    }

    [Test]
    public void ResetToInitialState_ShouldResetTerminalState()
    {
        // Arrange: Modify terminal state
        var csiH = new byte[] { 0x1b, 0x5b, 0x35, 0x3b, 0x31, 0x30, 0x48 }; // ESC[5;10H
        _terminal.Write(csiH.AsSpan());
        var esc7 = new byte[] { 0x1b, 0x37 }; // ESC 7
        _terminal.Write(esc7.AsSpan());
        var text = System.Text.Encoding.UTF8.GetBytes("Hello World");
        _terminal.Write(text.AsSpan());

        // Act: Reset to initial state
        var escC = new byte[] { 0x1b, 0x63 }; // ESC c
        _terminal.Write(escC.AsSpan());

        // Assert: Terminal should be reset
        Assert.That(_terminal.Cursor.Row, Is.EqualTo(0), "Cursor row should be reset to 0");
        Assert.That(_terminal.Cursor.Col, Is.EqualTo(0), "Cursor column should be reset to 0");
        
        // Check that screen is cleared by verifying a cell is empty
        var cell = _terminal.ScreenBuffer.GetCell(0, 0);
        Assert.That(cell.Character, Is.EqualTo(' '), "Screen should be cleared");
    }

    [Test]
    public void DesignateCharacterSet_ShouldSetCharacterSetForSlot()
    {
        // Act: Designate character sets for different slots
        var escG0 = new byte[] { 0x1b, 0x28, 0x42 }; // ESC ( B - Designate ASCII to G0
        _terminal.Write(escG0.AsSpan());
        var escG1 = new byte[] { 0x1b, 0x29, 0x30 }; // ESC ) 0 - Designate DEC Special Graphics to G1
        _terminal.Write(escG1.AsSpan());
        var escG2 = new byte[] { 0x1b, 0x2a, 0x41 }; // ESC * A - Designate UK to G2
        _terminal.Write(escG2.AsSpan());
        var escG3 = new byte[] { 0x1b, 0x2b, 0x34 }; // ESC + 4 - Designate Dutch to G3
        _terminal.Write(escG3.AsSpan());

        // Assert: Character sets should be designated (we can't easily test the internal state,
        // but we can verify the sequences don't crash the terminal)
        Assert.That(_terminal.Cursor.Row, Is.EqualTo(0), "Terminal should remain functional");
        Assert.That(_terminal.Cursor.Col, Is.EqualTo(0), "Terminal should remain functional");
    }

    [Test]
    public void ReverseIndex_ShouldMoveCursorUpOrScrollDown()
    {
        // Arrange: Move cursor down a few lines
        var csiH = new byte[] { 0x1b, 0x5b, 0x35, 0x3b, 0x31, 0x48 }; // ESC[5;1H
        _terminal.Write(csiH.AsSpan());
        var initialRow = _terminal.Cursor.Row;

        // Act: Send reverse index
        var escM = new byte[] { 0x1b, 0x4d }; // ESC M
        _terminal.Write(escM.AsSpan());

        // Assert: Cursor should move up one line
        Assert.That(_terminal.Cursor.Row, Is.EqualTo(initialRow - 1), "Cursor should move up one line");
    }

    [Test]
    public void ReverseIndex_AtTopOfScreen_ShouldStayAtTop()
    {
        // Arrange: Cursor is already at top
        Assert.That(_terminal.Cursor.Row, Is.EqualTo(0), "Cursor should start at top");

        // Act: Send reverse index
        var escM = new byte[] { 0x1b, 0x4d }; // ESC M
        _terminal.Write(escM.AsSpan());

        // Assert: Cursor should stay at top (scroll region behavior will be implemented later)
        Assert.That(_terminal.Cursor.Row, Is.EqualTo(0), "Cursor should stay at top of screen");
    }
}