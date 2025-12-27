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

    /// <summary>
    ///     Tests that Resize with valid dimensions updates terminal dimensions.
    /// </summary>
    [Test]
    public void Resize_WithValidDimensions_UpdatesDimensions()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);

        // Act
        terminal.Resize(100, 30);

        // Assert
        Assert.That(terminal.Width, Is.EqualTo(100));
        Assert.That(terminal.Height, Is.EqualTo(30));
    }

    /// <summary>
    ///     Tests that Resize preserves cursor position when possible.
    /// </summary>
    [Test]
    public void Resize_PreservesCursorPosition()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("Hello");
        // Cursor should be at (0, 5)

        // Act - resize to larger dimensions
        terminal.Resize(120, 40);

        // Assert - cursor position preserved
        Assert.That(terminal.Cursor.Row, Is.EqualTo(0));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(5));
    }

    /// <summary>
    ///     Tests that Resize clamps cursor position when dimensions shrink.
    /// </summary>
    [Test]
    public void Resize_ClampsCursorPosition()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        // Move cursor to position that will be out of bounds after resize
        terminal.Write(new string(' ', 70)); // Move cursor to column 70
        
        // Act - resize to smaller width
        terminal.Resize(50, 24);

        // Assert - cursor clamped to new bounds
        Assert.That(terminal.Width, Is.EqualTo(50));
        Assert.That(terminal.Cursor.Col, Is.LessThan(50));
    }

    /// <summary>
    ///     Tests that Resize preserves content within new bounds.
    /// </summary>
    [Test]
    public void Resize_PreservesContentWithinBounds()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("Test content");

        // Act - resize to larger dimensions
        terminal.Resize(120, 40);

        // Assert - content preserved
        Assert.That(terminal.ScreenBuffer.GetCell(0, 0).Character, Is.EqualTo('T'));
        Assert.That(terminal.ScreenBuffer.GetCell(0, 1).Character, Is.EqualTo('e'));
        Assert.That(terminal.ScreenBuffer.GetCell(0, 2).Character, Is.EqualTo('s'));
        Assert.That(terminal.ScreenBuffer.GetCell(0, 3).Character, Is.EqualTo('t'));
    }

    /// <summary>
    ///     Tests that Resize with same dimensions does nothing.
    /// </summary>
    [Test]
    public void Resize_WithSameDimensions_DoesNothing()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("Test");
        int originalCursorCol = terminal.Cursor.Col;

        // Act
        terminal.Resize(80, 24);

        // Assert - no change
        Assert.That(terminal.Width, Is.EqualTo(80));
        Assert.That(terminal.Height, Is.EqualTo(24));
        Assert.That(terminal.Cursor.Col, Is.EqualTo(originalCursorCol));
        Assert.That(terminal.ScreenBuffer.GetCell(0, 0).Character, Is.EqualTo('T'));
    }

    /// <summary>
    ///     Tests that Resize with invalid dimensions throws ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public void Resize_WithInvalidDimensions_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => terminal.Resize(0, 24));
        Assert.Throws<ArgumentOutOfRangeException>(() => terminal.Resize(80, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => terminal.Resize(1001, 24));
        Assert.Throws<ArgumentOutOfRangeException>(() => terminal.Resize(80, 1001));
    }

    /// <summary>
    ///     Tests that Resize after disposal throws ObjectDisposedException.
    /// </summary>
    [Test]
    public void Resize_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => terminal.Resize(100, 30));
    }

    /// <summary>
    ///     Tests that CSI save/restore private mode sequences work correctly.
    /// </summary>
    [Test]
    public void Write_CsiSaveRestorePrivateModes_WorksCorrectly()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);

        // Set some initial modes
        terminal.Write("\x1b[?1h");    // Application cursor keys on
        terminal.Write("\x1b[?7l");    // Auto-wrap off
        terminal.Write("\x1b[?25l");   // Cursor invisible

        // Verify initial state
        Assert.That(terminal.ModeManager.ApplicationCursorKeys, Is.True);
        Assert.That(terminal.ModeManager.AutoWrapMode, Is.False);
        Assert.That(terminal.ModeManager.CursorVisible, Is.False);

        // Save modes 1 and 25
        terminal.Write("\x1b[?1;25s");

        // Change the modes
        terminal.Write("\x1b[?1l");    // Application cursor keys off
        terminal.Write("\x1b[?25h");   // Cursor visible

        // Verify changed state
        Assert.That(terminal.ModeManager.ApplicationCursorKeys, Is.False);
        Assert.That(terminal.ModeManager.CursorVisible, Is.True);

        // Restore the saved modes
        terminal.Write("\x1b[?1;25r");

        // Verify restored state (only modes 1 and 25 should be restored)
        Assert.That(terminal.ModeManager.ApplicationCursorKeys, Is.True);  // Restored
        Assert.That(terminal.ModeManager.CursorVisible, Is.False);         // Restored
        Assert.That(terminal.ModeManager.AutoWrapMode, Is.False);          // Not saved/restored, kept current value

        terminal.Dispose();
    }

    /// <summary>
    ///     Tests that CSI cursor style sequence (DECSCUSR) works correctly.
    /// </summary>
    [Test]
    public void Write_CsiSetCursorStyle_WorksCorrectly()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);

        // Test different cursor styles
        terminal.Write("\x1b[2 q");    // Steady block
        Assert.That(terminal.State.CursorStyle, Is.EqualTo(2));

        terminal.Write("\x1b[4 q");    // Steady underline
        Assert.That(terminal.State.CursorStyle, Is.EqualTo(4));

        terminal.Write("\x1b[6 q");    // Steady bar
        Assert.That(terminal.State.CursorStyle, Is.EqualTo(6));

        // Test invalid style (should default to 1)
        terminal.Write("\x1b[10 q");   // Invalid style
        Assert.That(terminal.State.CursorStyle, Is.EqualTo(1));

        // Test style 0 (should map to 1)
        terminal.Write("\x1b[0 q");    // Style 0 maps to 1
        Assert.That(terminal.State.CursorStyle, Is.EqualTo(1));

        terminal.Dispose();
    }

    /// <summary>
    ///     Tests that bracketed paste mode is properly tracked.
    /// </summary>
    [Test]
    public void Write_BracketedPasteMode_WorksCorrectly()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);

        // Initially should be off
        Assert.That(terminal.ModeManager.BracketedPasteMode, Is.False);
        Assert.That(terminal.State.BracketedPasteMode, Is.False);

        // Enable bracketed paste mode
        terminal.Write("\x1b[?2004h");
        Assert.That(terminal.ModeManager.BracketedPasteMode, Is.True);
        Assert.That(terminal.State.BracketedPasteMode, Is.True);

        // Disable bracketed paste mode
        terminal.Write("\x1b[?2004l");
        Assert.That(terminal.ModeManager.BracketedPasteMode, Is.False);
        Assert.That(terminal.State.BracketedPasteMode, Is.False);

        terminal.Dispose();
    }

    /// <summary>
    ///     Tests that paste content is wrapped with escape sequences when bracketed paste mode is enabled.
    /// </summary>
    [Test]
    public void WrapPasteContent_WithBracketedPasteModeEnabled_WrapsContent()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("\x1b[?2004h"); // Enable bracketed paste mode

        // Act
        string result = terminal.WrapPasteContent("hello world");

        // Assert
        Assert.That(result, Is.EqualTo("\x1b[200~hello world\x1b[201~"));

        terminal.Dispose();
    }

    /// <summary>
    ///     Tests that paste content is not wrapped when bracketed paste mode is disabled.
    /// </summary>
    [Test]
    public void WrapPasteContent_WithBracketedPasteModeDisabled_DoesNotWrapContent()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        // Bracketed paste mode is disabled by default

        // Act
        string result = terminal.WrapPasteContent("hello world");

        // Assert
        Assert.That(result, Is.EqualTo("hello world"));

        terminal.Dispose();
    }

    /// <summary>
    ///     Tests that empty paste content is handled correctly.
    /// </summary>
    [Test]
    public void WrapPasteContent_WithEmptyContent_HandlesCorrectly()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("\x1b[?2004h"); // Enable bracketed paste mode

        // Act & Assert
        Assert.That(terminal.WrapPasteContent(""), Is.EqualTo(""));
        Assert.That(terminal.WrapPasteContent(null!), Is.EqualTo(null));

        terminal.Dispose();
    }

    /// <summary>
    ///     Tests that the ReadOnlySpan overload works correctly.
    /// </summary>
    [Test]
    public void WrapPasteContent_WithReadOnlySpan_WorksCorrectly()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);
        terminal.Write("\x1b[?2004h"); // Enable bracketed paste mode

        // Act
        ReadOnlySpan<char> content = "test content".AsSpan();
        string result = terminal.WrapPasteContent(content);

        // Assert
        Assert.That(result, Is.EqualTo("\x1b[200~test content\x1b[201~"));

        terminal.Dispose();
    }

    /// <summary>
    ///     Tests that the bracketed paste mode state check method works correctly.
    /// </summary>
    [Test]
    public void IsBracketedPasteModeEnabled_ReflectsCurrentState()
    {
        // Arrange
        var terminal = new TerminalEmulator(80, 24);

        // Initially should be false
        Assert.That(terminal.IsBracketedPasteModeEnabled(), Is.False);

        // Enable bracketed paste mode
        terminal.Write("\x1b[?2004h");
        Assert.That(terminal.IsBracketedPasteModeEnabled(), Is.True);

        // Disable bracketed paste mode
        terminal.Write("\x1b[?2004l");
        Assert.That(terminal.IsBracketedPasteModeEnabled(), Is.False);

        terminal.Dispose();
    }
}
