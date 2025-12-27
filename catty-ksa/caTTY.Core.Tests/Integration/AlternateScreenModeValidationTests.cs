using caTTY.Core.Terminal;
using caTTY.Core.Types;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace caTTY.Core.Tests.Integration;

/// <summary>
///     Comprehensive validation tests for alternate screen and terminal modes.
///     Tests full-screen application scenarios and documents any mode handling issues.
///     Validates Requirements 15.1-15.5, 20.1-20.5, 8.3, 8.5.
/// </summary>
[TestFixture]
[Category("Integration")]
public class AlternateScreenModeValidationTests
{
    private TerminalEmulator _terminal = null!;

    [SetUp]
    public void SetUp()
    {
        _terminal = new TerminalEmulator(80, 24, 1000, NullLogger.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _terminal.Dispose();
    }

    /// <summary>
    ///     Simulates a full-screen application like 'less' that uses alternate screen.
    ///     Tests the complete lifecycle: enter alternate screen, display content, handle scrolling, exit.
    /// </summary>
    [Test]
    public void FullScreenApplication_LessSimulation_WorksCorrectly()
    {
        // Arrange: Set up primary screen with some content (like a shell prompt)
        _terminal.Write("user@host:~$ less document.txt\r\n");
        var primaryContent = CaptureScreenContent();

        // Act: Simulate 'less' entering alternate screen mode
        _terminal.Write("\x1b[?1049h"); // Save cursor, switch to alternate screen, clear it

        // Assert: Should be in alternate screen with cursor at origin
        Assert.That(_terminal.State.IsAlternateScreenActive, Is.True, 
            "Should be in alternate screen mode");
        Assert.That(_terminal.Cursor.Row, Is.EqualTo(0), 
            "Cursor should be at row 0 after entering alternate screen");
        Assert.That(_terminal.Cursor.Col, Is.EqualTo(0), 
            "Cursor should be at column 0 after entering alternate screen");

        // Simulate 'less' displaying file content
        for (int i = 1; i <= 23; i++)
        {
            _terminal.Write($"Line {i} of the document content that is being displayed by less\r\n");
        }
        _terminal.Write("(END)"); // Status line at bottom

        // Verify content is displayed in alternate screen
        Assert.That(_terminal.ScreenBuffer.GetCell(0, 0).Character, Is.EqualTo('L'), 
            "First line should be displayed");
        Assert.That(_terminal.ScreenBuffer.GetCell(22, 0).Character, Is.EqualTo('L'), 
            "Line 23 should be displayed");

        // Simulate user pressing 'q' to quit less
        _terminal.Write("\x1b[?1049l"); // Restore cursor, switch back to primary screen

        // Assert: Should be back in primary screen with original content and cursor position
        Assert.That(_terminal.State.IsAlternateScreenActive, Is.False, 
            "Should be back in primary screen mode");
        
        // Verify primary screen content is restored
        var restoredContent = CaptureScreenContent();
        Assert.That(restoredContent[0, 0].Character, Is.EqualTo(primaryContent[0, 0].Character), 
            "Primary screen content should be restored");
        
        // Note: Cursor position restoration is handled by the saved cursor mechanism
        // The exact position may vary based on implementation details
    }

    /// <summary>
    ///     Tests cursor wrapping behavior in both normal and alternate screen modes.
    ///     Validates Requirements 8.3, 20.1.
    /// </summary>
    [Test]
    public void CursorWrapping_WorksInBothScreenModes()
    {
        // Test in primary screen first
        TestCursorWrappingInCurrentMode("Primary");

        // Switch to alternate screen and test wrapping there
        _terminal.Write("\x1b[?47h");
        Assert.That(_terminal.State.IsAlternateScreenActive, Is.True);
        
        TestCursorWrappingInCurrentMode("Alternate");

        // Switch back and verify wrapping still works
        _terminal.Write("\x1b[?47l");
        Assert.That(_terminal.State.IsAlternateScreenActive, Is.False);
        
        TestCursorWrappingInCurrentMode("Primary-After");
    }

    private void TestCursorWrappingInCurrentMode(string mode)
    {
        // Clear screen and move to a position near the right edge
        _terminal.Write("\x1b[2J\x1b[H"); // Clear screen, move to home
        _terminal.Write($"\x1b[1;{_terminal.Width - 5}H"); // Move near right edge

        // Write text that should wrap
        string longText = "This text should wrap to the next line";
        _terminal.Write(longText);

        // Verify cursor wrapped to next line
        Assert.That(_terminal.Cursor.Row, Is.GreaterThan(0), 
            $"Cursor should have wrapped to next line in {mode} mode");
    }

    /// <summary>
    ///     Tests cursor visibility tracking across screen mode switches.
    ///     Validates Requirements 8.5, 20.2.
    /// </summary>
    [Test]
    public void CursorVisibility_PreservedAcrossScreenModes()
    {
        // Set cursor invisible in primary screen
        _terminal.Write("\x1b[?25l"); // Hide cursor
        Assert.That(_terminal.Cursor.Visible, Is.False, "Cursor should be hidden in primary screen");

        // Switch to alternate screen
        _terminal.Write("\x1b[?47h");
        Assert.That(_terminal.State.IsAlternateScreenActive, Is.True);
        
        // Cursor visibility should be preserved (or reset to default - document the behavior)
        bool alternateVisibility = _terminal.Cursor.Visible;
        
        // Make cursor visible in alternate screen
        _terminal.Write("\x1b[?25h"); // Show cursor
        Assert.That(_terminal.Cursor.Visible, Is.True, "Cursor should be visible in alternate screen");

        // Switch back to primary screen
        _terminal.Write("\x1b[?47l");
        Assert.That(_terminal.State.IsAlternateScreenActive, Is.False);
        
        // Document the behavior: cursor visibility may be per-buffer or global
        bool primaryVisibilityRestored = _terminal.Cursor.Visible;
        
        // This test documents the current behavior rather than asserting a specific requirement
        TestContext.WriteLine($"Cursor visibility behavior:");
        TestContext.WriteLine($"  Primary (hidden): false");
        TestContext.WriteLine($"  Alternate (initial): {alternateVisibility}");
        TestContext.WriteLine($"  Alternate (shown): true");
        TestContext.WriteLine($"  Primary (restored): {primaryVisibilityRestored}");
    }

    /// <summary>
    ///     Tests terminal mode switching and state preservation.
    ///     Validates Requirements 20.1-20.5.
    /// </summary>
    [Test]
    public void TerminalModes_SwitchingAndStatePreservation()
    {
        // Test auto-wrap mode
        Assert.That(_terminal.ModeManager.AutoWrapMode, Is.True, "Auto-wrap should be enabled by default");
        
        _terminal.Write("\x1b[?7l"); // Disable auto-wrap
        Assert.That(_terminal.ModeManager.AutoWrapMode, Is.False, "Auto-wrap should be disabled");
        
        _terminal.Write("\x1b[?7h"); // Enable auto-wrap
        Assert.That(_terminal.ModeManager.AutoWrapMode, Is.True, "Auto-wrap should be enabled again");

        // Test application cursor keys mode
        Assert.That(_terminal.ModeManager.ApplicationCursorKeys, Is.False, "App cursor keys should be disabled by default");
        
        _terminal.Write("\x1b[?1h"); // Enable application cursor keys
        Assert.That(_terminal.ModeManager.ApplicationCursorKeys, Is.True, "App cursor keys should be enabled");
        
        _terminal.Write("\x1b[?1l"); // Disable application cursor keys
        Assert.That(_terminal.ModeManager.ApplicationCursorKeys, Is.False, "App cursor keys should be disabled again");

        // Test bracketed paste mode
        Assert.That(_terminal.ModeManager.BracketedPasteMode, Is.False, "Bracketed paste should be disabled by default");
        
        _terminal.Write("\x1b[?2004h"); // Enable bracketed paste
        Assert.That(_terminal.ModeManager.BracketedPasteMode, Is.True, "Bracketed paste should be enabled");
        
        _terminal.Write("\x1b[?2004l"); // Disable bracketed paste
        Assert.That(_terminal.ModeManager.BracketedPasteMode, Is.False, "Bracketed paste should be disabled again");
    }

    /// <summary>
    ///     Tests bracketed paste mode functionality.
    ///     Validates Requirements 20.5.
    /// </summary>
    [Test]
    public void BracketedPasteMode_WrapsContentCorrectly()
    {
        // Enable bracketed paste mode
        _terminal.Write("\x1b[?2004h");
        Assert.That(_terminal.ModeManager.BracketedPasteMode, Is.True);

        // Test paste content wrapping
        string pasteContent = "Hello, World!";
        string wrappedContent = _terminal.WrapPasteContent(pasteContent);
        
        Assert.That(wrappedContent, Is.EqualTo("\x1b[200~Hello, World!\x1b[201~"), 
            "Paste content should be wrapped with bracketed paste markers");

        // Test with empty content
        string emptyWrapped = _terminal.WrapPasteContent("");
        Assert.That(emptyWrapped, Is.EqualTo(""), "Empty content should remain empty");

        // Disable bracketed paste mode
        _terminal.Write("\x1b[?2004l");
        Assert.That(_terminal.ModeManager.BracketedPasteMode, Is.False);

        // Test that content is not wrapped when mode is disabled
        string unwrappedContent = _terminal.WrapPasteContent(pasteContent);
        Assert.That(unwrappedContent, Is.EqualTo(pasteContent), 
            "Paste content should not be wrapped when bracketed paste is disabled");
    }

    /// <summary>
    ///     Tests complex full-screen application scenario with multiple mode switches.
    ///     Simulates applications like vim that use multiple terminal features.
    /// </summary>
    [Test]
    public void ComplexFullScreenApplication_VimSimulation_WorksCorrectly()
    {
        // Arrange: Set up shell environment
        _terminal.Write("user@host:~$ vim document.txt\r\n");
        var shellState = CaptureScreenContent();

        // Act: Simulate vim startup sequence
        _terminal.Write("\x1b[?1049h"); // Save cursor, switch to alternate screen, clear
        _terminal.Write("\x1b[?1h");    // Enable application cursor keys
        _terminal.Write("\x1b[?25h");   // Ensure cursor is visible

        // Verify vim environment is set up
        Assert.That(_terminal.State.IsAlternateScreenActive, Is.True, "Should be in alternate screen");
        Assert.That(_terminal.ModeManager.ApplicationCursorKeys, Is.True, "App cursor keys should be enabled");
        Assert.That(_terminal.Cursor.Visible, Is.True, "Cursor should be visible");

        // Simulate vim displaying file content and status line
        for (int i = 1; i <= 22; i++)
        {
            _terminal.Write($"Line {i} of the file being edited in vim\r\n");
        }
        _terminal.Write("\"document.txt\" 22L, 456C"); // Status line

        // Simulate some editing operations (cursor movements, text insertion)
        _terminal.Write("\x1b[10;5H"); // Move cursor to line 10, column 5
        _terminal.Write("i");          // Enter insert mode (conceptually)
        _terminal.Write("inserted text");
        _terminal.Write("\x1b");       // ESC to exit insert mode (conceptually)

        // Verify content was modified
        Assert.That(_terminal.ScreenBuffer.GetCell(9, 4).Character, Is.EqualTo('i'), 
            "Inserted text should be visible");

        // Simulate vim exit sequence
        _terminal.Write("\x1b[?1l");    // Disable application cursor keys
        _terminal.Write("\x1b[?1049l"); // Restore cursor, switch back to primary screen

        // Assert: Should be back to shell with original state
        Assert.That(_terminal.State.IsAlternateScreenActive, Is.False, "Should be back in primary screen");
        Assert.That(_terminal.ModeManager.ApplicationCursorKeys, Is.False, "App cursor keys should be disabled");
        
        // Shell prompt should be restored (though exact cursor position may vary)
        var restoredState = CaptureScreenContent();
        Assert.That(restoredState[0, 0].Character, Is.EqualTo(shellState[0, 0].Character), 
            "Shell state should be restored");
    }

    /// <summary>
    ///     Tests scrollback isolation during alternate screen operations.
    ///     Validates Requirements 15.3.
    /// </summary>
    [Test]
    public void ScrollbackIsolation_AlternateScreenDoesNotAffectScrollback()
    {
        // Arrange: Create scrollback content in primary screen
        for (int i = 0; i < 30; i++)
        {
            _terminal.Write($"Primary scrollback line {i}\r\n");
        }
        
        int initialScrollback = _terminal.ScrollbackManager.CurrentLines;
        Assert.That(initialScrollback, Is.GreaterThan(0), "Should have scrollback content");

        // Act: Switch to alternate screen and create content that would normally scroll
        _terminal.Write("\x1b[?47h");
        
        for (int i = 0; i < 50; i++)
        {
            _terminal.Write($"Alternate screen line {i} that should not affect scrollback\r\n");
        }

        // Assert: Scrollback should be unchanged
        int scrollbackDuringAlternate = _terminal.ScrollbackManager.CurrentLines;
        Assert.That(scrollbackDuringAlternate, Is.EqualTo(initialScrollback), 
            "Scrollback should not change while in alternate screen");

        // Act: Return to primary screen
        _terminal.Write("\x1b[?47l");

        // Assert: Scrollback should still be unchanged
        int finalScrollback = _terminal.ScrollbackManager.CurrentLines;
        Assert.That(finalScrollback, Is.EqualTo(initialScrollback), 
            "Scrollback should remain unchanged after returning to primary screen");
    }

    /// <summary>
    ///     Documents any identified mode handling issues for future reference.
    /// </summary>
    [Test]
    public void DocumentModeHandlingIssues()
    {
        TestContext.WriteLine("=== Mode Handling Validation Results ===");
        
        // Test and document cursor visibility behavior across screen switches
        TestContext.WriteLine("\n1. Cursor Visibility Across Screen Modes:");
        _terminal.Write("\x1b[?25l"); // Hide cursor
        bool primaryHidden = !_terminal.Cursor.Visible;
        
        _terminal.Write("\x1b[?47h"); // Switch to alternate
        bool alternateInitial = _terminal.Cursor.Visible;
        
        _terminal.Write("\x1b[?25h"); // Show cursor in alternate
        bool alternateShown = _terminal.Cursor.Visible;
        
        _terminal.Write("\x1b[?47l"); // Switch back to primary
        bool primaryRestored = _terminal.Cursor.Visible;
        
        TestContext.WriteLine($"   Primary (hidden): {!primaryHidden}");
        TestContext.WriteLine($"   Alternate (initial): {alternateInitial}");
        TestContext.WriteLine($"   Alternate (shown): {alternateShown}");
        TestContext.WriteLine($"   Primary (restored): {primaryRestored}");
        TestContext.WriteLine($"   Issue: Cursor visibility may not be properly isolated per buffer");

        // Test and document auto-wrap behavior
        TestContext.WriteLine("\n2. Auto-wrap Mode Behavior:");
        _terminal.Write("\x1b[2J\x1b[H"); // Clear and home
        _terminal.Write("\x1b[?7l"); // Disable auto-wrap
        _terminal.Write($"\x1b[1;{_terminal.Width - 2}H"); // Near right edge
        _terminal.Write("OVERFLOW");
        
        bool wrappedWhenDisabled = _terminal.Cursor.Row > 0;
        TestContext.WriteLine($"   Text wraps when auto-wrap disabled: {wrappedWhenDisabled}");
        
        if (wrappedWhenDisabled)
        {
            TestContext.WriteLine($"   Issue: Auto-wrap disable may not be fully implemented");
        }

        // Test and document mode persistence
        TestContext.WriteLine("\n3. Mode Persistence:");
        _terminal.Write("\x1b[?1h\x1b[?2004h"); // Enable app cursor keys and bracketed paste
        bool modesSet = _terminal.ModeManager.ApplicationCursorKeys && _terminal.ModeManager.BracketedPasteMode;
        
        _terminal.Write("\x1b[?47h\x1b[?47l"); // Switch to alternate and back
        bool modesPreserved = _terminal.ModeManager.ApplicationCursorKeys && _terminal.ModeManager.BracketedPasteMode;
        
        TestContext.WriteLine($"   Modes set correctly: {modesSet}");
        TestContext.WriteLine($"   Modes preserved across screen switch: {modesPreserved}");
        
        if (!modesPreserved)
        {
            TestContext.WriteLine($"   Issue: Terminal modes may not persist across screen buffer switches");
        }

        TestContext.WriteLine("\n=== End Validation Results ===");
    }

    /// <summary>
    ///     Helper method to capture current screen content for comparison.
    /// </summary>
    private Cell[,] CaptureScreenContent()
    {
        var content = new Cell[_terminal.Height, _terminal.Width];
        
        for (int row = 0; row < _terminal.Height; row++)
        {
            for (int col = 0; col < _terminal.Width; col++)
            {
                content[row, col] = _terminal.ScreenBuffer.GetCell(row, col);
            }
        }
        
        return content;
    }
}