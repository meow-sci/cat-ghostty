using NUnit.Framework;
using caTTY.Core.Terminal;
using caTTY.Core.Types;

namespace caTTY.Core.Tests.Unit;

/// <summary>
/// Unit tests for the TerminalEmulator class.
/// </summary>
[TestFixture]
[Category("Unit")]
public class TerminalEmulatorTests
{
    /// <summary>
    /// Tests that TerminalEmulator constructor creates a terminal with valid dimensions.
    /// </summary>
    [Test]
    public void Constructor_WithValidDimensions_CreatesTerminal()
    {
        // Arrange & Act
        var terminal = new TerminalEmulator(80, 24);

        // Assert
        Assert.That(terminal.Width, Is.EqualTo(80));
        Assert.That(terminal.Height, Is.EqualTo(24));
        Assert.That(terminal.Cursor.Row, Is.EqualTo(0));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that TerminalEmulator constructor throws ArgumentOutOfRangeException for invalid width.
    /// </summary>
    [Test]
    public void Constructor_WithInvalidWidth_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalEmulator(0, 24));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalEmulator(-1, 24));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalEmulator(1001, 24));
    }

    /// <summary>
    /// Tests that TerminalEmulator constructor throws ArgumentOutOfRangeException for invalid height.
    /// </summary>
    [Test]
    public void Constructor_WithInvalidHeight_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalEmulator(80, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalEmulator(80, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalEmulator(80, 1001));
    }

    /// <summary>
    /// Tests that Write with a printable character writes the character at cursor position and advances cursor.
    /// </summary>
    [Test]
    public void Write_WithPrintableCharacter_WritesCharacterAtCursor()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        var data = new byte[] { (byte)'A' };

        // Act
        terminal.Write(data);

        // Assert
        var cell = terminal.ScreenBuffer.GetCell(0, 0);
        Assert.That(cell.Character, Is.EqualTo('A'));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(1)); // Cursor should advance
    }

    /// <summary>
    /// Tests that Write with multiple characters writes all characters sequentially.
    /// </summary>
    [Test]
    public void Write_WithMultipleCharacters_WritesAllCharacters()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        var data = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.ScreenBuffer.GetCell(0, 0).Character, Is.EqualTo('H'));
        Assert.That(terminal.ScreenBuffer.GetCell(0, 1).Character, Is.EqualTo('e'));
        Assert.That(terminal.ScreenBuffer.GetCell(0, 2).Character, Is.EqualTo('l'));
        Assert.That(terminal.ScreenBuffer.GetCell(0, 3).Character, Is.EqualTo('l'));
        Assert.That(terminal.ScreenBuffer.GetCell(0, 4).Character, Is.EqualTo('o'));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(5));
    }

    /// <summary>
    /// Tests that Write with a string writes the string content to the terminal.
    /// </summary>
    [Test]
    public void Write_WithString_WritesStringContent()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);

        // Act
        terminal.Write("Test");

        // Assert
        Assert.That(terminal.ScreenBuffer.GetCell(0, 0).Character, Is.EqualTo('T'));
        Assert.That(terminal.ScreenBuffer.GetCell(0, 1).Character, Is.EqualTo('e'));
        Assert.That(terminal.ScreenBuffer.GetCell(0, 2).Character, Is.EqualTo('s'));
        Assert.That(terminal.ScreenBuffer.GetCell(0, 3).Character, Is.EqualTo('t'));
    }

    /// <summary>
    /// Tests that Write with line feed (LF) moves cursor down one row.
    /// </summary>
    [Test]
    public void Write_WithLineFeed_MovesCursorDown()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("A");
        var data = new byte[] { 0x0A }; // LF

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.Cursor.Row, Is.EqualTo(1));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(1)); // Column should stay the same
    }

    /// <summary>
    /// Tests that Write with carriage return (CR) moves cursor to column zero.
    /// </summary>
    [Test]
    public void Write_WithCarriageReturn_MovesCursorToColumnZero()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("Hello");
        var data = new byte[] { 0x0D }; // CR

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.Cursor.Row, Is.EqualTo(0));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that Write with CRLF sequence moves cursor to the beginning of the next line.
    /// </summary>
    [Test]
    public void Write_WithCRLF_MovesCursorToNextLineStart()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("Hello");
        var data = new byte[] { 0x0D, 0x0A }; // CR LF

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.Cursor.Row, Is.EqualTo(1));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that Write with tab character moves cursor to the next tab stop.
    /// </summary>
    [Test]
    public void Write_WithTab_MovesCursorToNextTabStop()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        var data = new byte[] { 0x09 }; // TAB

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.Cursor.Col, Is.EqualTo(8)); // Next tab stop at column 8
    }

    /// <summary>
    /// Tests that Write with DEL character ignores the character and doesn't move cursor.
    /// </summary>
    [Test]
    public void Write_WithDEL_IgnoresCharacter()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("A");
        var data = new byte[] { 0x7F }; // DEL

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.Cursor.Col, Is.EqualTo(1)); // Cursor should not move
        Assert.That(terminal.ScreenBuffer.GetCell(0, 1).Character, Is.EqualTo(' ')); // Should remain empty
    }

    /// <summary>
    /// Tests that Write at the right edge of terminal wraps to the next line.
    /// </summary>
    [Test]
    public void Write_AtRightEdge_WrapsToNextLine()
    {
        // Arrange
        var terminal = new TerminalEmulator(5, 24); // Small width for testing
        terminal.Write("12345"); // Fill first line

        // Act
        terminal.Write("6"); // Should wrap to next line

        // Assert
        Assert.That(terminal.Cursor.Row, Is.EqualTo(1));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(1));
        Assert.That(terminal.ScreenBuffer.GetCell(1, 0).Character, Is.EqualTo('6'));
    }

    /// <summary>
    /// Tests that Write raises ScreenUpdated event when content is written.
    /// </summary>
    [Test]
    public void Write_RaisesScreenUpdatedEvent()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        bool eventRaised = false;
        terminal.ScreenUpdated += (sender, args) => eventRaised = true;

        // Act
        terminal.Write("A");

        // Assert
        Assert.That(eventRaised, Is.True);
    }

    /// <summary>
    /// Tests that Write with empty data does not raise ScreenUpdated event.
    /// </summary>
    [Test]
    public void Write_WithEmptyData_DoesNotRaiseEvent()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        bool eventRaised = false;
        terminal.ScreenUpdated += (sender, args) => eventRaised = true;

        // Act
        terminal.Write(ReadOnlySpan<byte>.Empty);

        // Assert
        Assert.That(eventRaised, Is.False);
    }

    /// <summary>
    /// Tests that Dispose can be called multiple times without throwing.
    /// </summary>
    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            terminal.Dispose();
            terminal.Dispose();
        });
    }

    /// <summary>
    /// Tests that Write throws ObjectDisposedException after the terminal is disposed.
    /// </summary>
    [Test]
    public void Write_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => terminal.Write("Test"));
    }
}