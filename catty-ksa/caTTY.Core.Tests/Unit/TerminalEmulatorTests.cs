using caTTY.Core.Terminal;
using caTTY.Core.Types;
using NUnit.Framework;

namespace caTTY.Core.Tests.Unit;

/// <summary>
///     Unit tests for the TerminalEmulator class.
/// </summary>
[TestFixture]
[Category("Unit")]
public class TerminalEmulatorTests
{
    /// <summary>
    ///     Tests that TerminalEmulator constructor creates a terminal with valid dimensions.
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
    ///     Tests that TerminalEmulator constructor throws ArgumentOutOfRangeException for invalid width.
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
    ///     Tests that TerminalEmulator constructor throws ArgumentOutOfRangeException for invalid height.
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
    ///     Tests that Write with a printable character writes the character at cursor position and advances cursor.
    /// </summary>
    [Test]
    public void Write_WithPrintableCharacter_WritesCharacterAtCursor()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        byte[] data = new[] { (byte)'A' };

        // Act
        terminal.Write(data);

        // Assert
        Cell cell = terminal.ScreenBuffer.GetCell(0, 0);
        Assert.That(cell.Character, Is.EqualTo('A'));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(1)); // Cursor should advance
    }

    /// <summary>
    ///     Tests that Write with multiple characters writes all characters sequentially.
    /// </summary>
    [Test]
    public void Write_WithMultipleCharacters_WritesAllCharacters()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        byte[] data = new[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };

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
    ///     Tests that Write with a string writes the string content to the terminal.
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
    ///     Tests that Write with line feed (LF) moves cursor down one row.
    /// </summary>
    [Test]
    public void Write_WithLineFeed_MovesCursorDown()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("A");
        byte[] data = new byte[] { 0x0A }; // LF

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.Cursor.Row, Is.EqualTo(1));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(1)); // Line feed only moves down, keeps same column
    }

    /// <summary>
    ///     Tests that Write with carriage return (CR) moves cursor to column zero.
    /// </summary>
    [Test]
    public void Write_WithCarriageReturn_MovesCursorToColumnZero()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("Hello");
        byte[] data = new byte[] { 0x0D }; // CR

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.Cursor.Row, Is.EqualTo(0));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(0));
    }

    /// <summary>
    ///     Tests that Write with CRLF sequence moves cursor to the beginning of the next line.
    /// </summary>
    [Test]
    public void Write_WithCRLF_MovesCursorToNextLineStart()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("Hello");
        byte[] data = new byte[] { 0x0D, 0x0A }; // CR LF

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.Cursor.Row, Is.EqualTo(1));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(0));
    }

    /// <summary>
    ///     Tests that Write with tab character moves cursor to the next tab stop.
    /// </summary>
    [Test]
    public void Write_WithTab_MovesCursorToNextTabStop()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        byte[] data = new byte[] { 0x09 }; // TAB

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.Cursor.Col, Is.EqualTo(8)); // Next tab stop at column 8
    }

    /// <summary>
    ///     Tests that Write with bell character raises Bell event.
    /// </summary>
    [Test]
    public void Write_WithBell_RaisesBellEvent()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        bool bellEventRaised = false;
        BellEventArgs? bellEventArgs = null;
        terminal.Bell += (sender, args) =>
        {
            bellEventRaised = true;
            bellEventArgs = args;
        };
        byte[] data = new byte[] { 0x07 }; // BEL

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(bellEventRaised, Is.True);
        Assert.That(bellEventArgs, Is.Not.Null);
        Assert.That(bellEventArgs.Timestamp, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    /// <summary>
    ///     Tests that Write with backspace character moves cursor left if not at column 0.
    /// </summary>
    [Test]
    public void Write_WithBackspace_MovesCursorLeft()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("ABC"); // Move cursor to column 3
        byte[] data = new byte[] { 0x08 }; // BS

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.Cursor.Col, Is.EqualTo(2)); // Should move left from 3 to 2
        Assert.That(terminal.Cursor.Row, Is.EqualTo(0)); // Row should stay the same
    }

    /// <summary>
    ///     Tests that Write with backspace character at column 0 does not move cursor.
    /// </summary>
    [Test]
    public void Write_WithBackspaceAtColumnZero_DoesNotMoveCursor()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        // Cursor is already at (0, 0)
        byte[] data = new byte[] { 0x08 }; // BS

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.Cursor.Col, Is.EqualTo(0)); // Should stay at column 0
        Assert.That(terminal.Cursor.Row, Is.EqualTo(0)); // Row should stay the same
    }

    /// <summary>
    ///     Tests that Write with multiple control characters handles them all correctly.
    /// </summary>
    [Test]
    public void Write_WithMultipleControlCharacters_HandlesAllCorrectly()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        bool bellEventRaised = false;
        int bellCount = 0;
        terminal.Bell += (sender, args) =>
        {
            bellEventRaised = true;
            bellCount++;
        };
        byte[] data = new byte[] { 0x07, 0x07, 0x07 }; // Three BEL characters

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(bellEventRaised, Is.True);
        Assert.That(bellCount, Is.EqualTo(3));
    }

    /// <summary>
    ///     Tests that Write with mixed control characters and text works correctly.
    /// </summary>
    [Test]
    public void Write_WithMixedControlCharactersAndText_WorksCorrectly()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        byte[] data = new byte[] { (byte)'a', 0x07, (byte)'b', 0x08, (byte)'c' }; // a BEL b BS c

        // Act
        terminal.Write(data);

        // Assert
        // Sequence: 'a' at (0,0) → cursor (0,1), BEL → cursor (0,1), 'b' at (0,1) → cursor (0,2), BS → cursor (0,1), 'c' at (0,1) → cursor (0,2)
        Assert.That(terminal.ScreenBuffer.GetCell(0, 0).Character, Is.EqualTo('a')); // 'a' remains at (0,0)
        Assert.That(terminal.ScreenBuffer.GetCell(0, 1).Character, Is.EqualTo('c')); // 'c' overwrote 'b' at (0,1)
        Assert.That(terminal.Cursor.Col, Is.EqualTo(2)); // Cursor at column 2 after writing 'c'
    }

    /// <summary>
    ///     Tests that tab character moves to correct tab stops with default 8-column spacing.
    /// </summary>
    [Test]
    public void Write_WithTabCharacter_MovesToCorrectTabStops()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);

        // Act & Assert - Test multiple tab stops
        terminal.Write("\t"); // Should go to column 8
        Assert.That(terminal.Cursor.Col, Is.EqualTo(8));

        terminal.Write("\t"); // Should go to column 16
        Assert.That(terminal.Cursor.Col, Is.EqualTo(16));

        terminal.Write("\t"); // Should go to column 24
        Assert.That(terminal.Cursor.Col, Is.EqualTo(24));
    }

    /// <summary>
    ///     Tests that tab character at near right edge goes to right edge.
    /// </summary>
    [Test]
    public void Write_WithTabNearRightEdge_GoesToRightEdge()
    {
        // Arrange
        var terminal = new TerminalEmulator(10, 24); // Small width
        terminal.Write("1234567"); // Move to column 7

        // Act
        terminal.Write("\t"); // Should go to column 8 (next tab stop)

        // Assert
        Assert.That(terminal.Cursor.Col, Is.EqualTo(8));

        // Another tab should go to right edge (column 9)
        terminal.Write("\t");
        Assert.That(terminal.Cursor.Col, Is.EqualTo(9));
    }

    /// <summary>
    ///     Tests that backspace clears wrap pending state.
    /// </summary>
    [Test]
    public void Write_WithBackspaceAfterWrapPending_ClearsWrapPending()
    {
        // Arrange
        var terminal = new TerminalEmulator(5, 24); // Small width
        terminal.Write("12345"); // Fill first line, should set wrap pending

        // Act
        terminal.Write("\x08"); // Backspace

        // Assert
        Assert.That(terminal.Cursor.Col, Is.EqualTo(3)); // Should move back from 4 to 3
        Assert.That(terminal.Cursor.Row, Is.EqualTo(0)); // Should stay on same row

        // Writing another character should not wrap
        terminal.Write("X");
        Assert.That(terminal.Cursor.Row, Is.EqualTo(0)); // Should still be on row 0
        Assert.That(terminal.Cursor.Col, Is.EqualTo(4)); // Should be at column 4
    }

    /// <summary>
    ///     Tests that tab clears wrap pending state.
    /// </summary>
    [Test]
    public void Write_WithTabAfterWrapPending_ClearsWrapPending()
    {
        // Arrange
        var terminal = new TerminalEmulator(5, 24); // Small width
        terminal.Write("12345"); // Fill first line, should set wrap pending

        // Act
        terminal.Write("\t"); // Tab

        // Assert
        Assert.That(terminal.Cursor.Col, Is.EqualTo(4)); // Should stay at right edge
        Assert.That(terminal.Cursor.Row, Is.EqualTo(0)); // Should stay on same row
    }

    /// <summary>
    ///     Tests that Write with DEL character ignores the character and doesn't move cursor.
    /// </summary>
    [Test]
    public void Write_WithDEL_IgnoresCharacter()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("A");
        byte[] data = new byte[] { 0x7F }; // DEL

        // Act
        terminal.Write(data);

        // Assert
        Assert.That(terminal.Cursor.Col, Is.EqualTo(1)); // Cursor should not move
        Assert.That(terminal.ScreenBuffer.GetCell(0, 1).Character, Is.EqualTo(' ')); // Should remain empty
    }

    /// <summary>
    ///     Tests that Write at the right edge of terminal wraps to the next line.
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
    ///     Tests that Write raises ScreenUpdated event when content is written.
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
    ///     Tests that Write with empty data does not raise ScreenUpdated event.
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
    ///     Tests that Dispose can be called multiple times without throwing.
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
    ///     Tests that Write throws ObjectDisposedException after the terminal is disposed.
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

    /// <summary>
    ///     Tests that ScrollViewportUp disables auto-scroll and updates viewport offset.
    /// </summary>
    [Test]
    public void ScrollViewportUp_FromBottom_DisablesAutoScroll()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 3, 100); // Small terminal to force scrollback
        
        // Fill the screen and add more content to create scrollback
        for (int i = 0; i < 6; i++) // More lines than screen height
        {
            terminal.Write($"Line {i}\n");
        }
        
        Assert.That(terminal.IsAutoScrollEnabled, Is.True);
        Assert.That(terminal.ViewportOffset, Is.EqualTo(0));

        // Act
        terminal.ScrollViewportUp(2);

        // Assert
        Assert.That(terminal.IsAutoScrollEnabled, Is.False);
        Assert.That(terminal.ViewportOffset, Is.EqualTo(2));
    }

    /// <summary>
    ///     Tests that ScrollViewportDown to bottom re-enables auto-scroll.
    /// </summary>
    [Test]
    public void ScrollViewportDown_ToBottom_EnablesAutoScroll()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 3, 100); // Small terminal to force scrollback
        
        // Fill the screen and add more content to create scrollback
        for (int i = 0; i < 6; i++) // More lines than screen height
        {
            terminal.Write($"Line {i}\n");
        }
        
        terminal.ScrollViewportUp(3); // Scroll up first
        Assert.That(terminal.IsAutoScrollEnabled, Is.False);

        // Act
        terminal.ScrollViewportDown(3); // Scroll back to bottom

        // Assert
        Assert.That(terminal.IsAutoScrollEnabled, Is.True);
        Assert.That(terminal.ViewportOffset, Is.EqualTo(0));
    }

    /// <summary>
    ///     Tests that ScrollViewportToTop scrolls to the top and disables auto-scroll.
    /// </summary>
    [Test]
    public void ScrollViewportToTop_DisablesAutoScroll()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 3, 100); // Small terminal to force scrollback
        
        // Fill the screen and add more content to create scrollback
        for (int i = 0; i < 6; i++) // More lines than screen height
        {
            terminal.Write($"Line {i}\n");
        }

        // Act
        terminal.ScrollViewportToTop();

        // Assert
        Assert.That(terminal.IsAutoScrollEnabled, Is.False);
        Assert.That(terminal.ViewportOffset, Is.GreaterThan(0));
    }

    /// <summary>
    ///     Tests that ScrollViewportToBottom scrolls to bottom and enables auto-scroll.
    /// </summary>
    [Test]
    public void ScrollViewportToBottom_EnablesAutoScroll()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 3, 100); // Small terminal to force scrollback
        
        // Fill the screen and add more content to create scrollback
        for (int i = 0; i < 6; i++) // More lines than screen height
        {
            terminal.Write($"Line {i}\n");
        }
        
        terminal.ScrollViewportToTop(); // Scroll to top first
        Assert.That(terminal.IsAutoScrollEnabled, Is.False);

        // Act
        terminal.ScrollViewportToBottom();

        // Assert
        Assert.That(terminal.IsAutoScrollEnabled, Is.True);
        Assert.That(terminal.ViewportOffset, Is.EqualTo(0));
    }

    /// <summary>
    ///     Tests that viewport methods throw ObjectDisposedException after disposal.
    /// </summary>
    [Test]
    public void ViewportMethods_AfterDispose_ThrowObjectDisposedException()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => terminal.ScrollViewportUp(1));
        Assert.Throws<ObjectDisposedException>(() => terminal.ScrollViewportDown(1));
        Assert.Throws<ObjectDisposedException>(() => terminal.ScrollViewportToTop());
        Assert.Throws<ObjectDisposedException>(() => terminal.ScrollViewportToBottom());
    }
}
