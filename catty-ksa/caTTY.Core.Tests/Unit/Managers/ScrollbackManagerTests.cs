using NUnit.Framework;
using caTTY.Core.Managers;
using caTTY.Core.Types;

namespace caTTY.Core.Tests.Unit.Managers;

[TestFixture]
public class ScrollbackManagerTests
{
    private ScrollbackManager _scrollbackManager = null!;
    private const int TestColumns = 80;
    private const int TestMaxLines = 5;

    [SetUp]
    public void SetUp()
    {
        _scrollbackManager = new ScrollbackManager(TestMaxLines, TestColumns);
    }

    [TearDown]
    public void TearDown()
    {
        _scrollbackManager?.Dispose();
    }

    [Test]
    public void Constructor_ValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        using var manager = new ScrollbackManager(100, 80);

        // Assert
        Assert.That(manager.MaxLines, Is.EqualTo(100));
        Assert.That(manager.CurrentLines, Is.EqualTo(0));
        Assert.That(manager.ViewportOffset, Is.EqualTo(0));
        Assert.That(manager.IsAtBottom, Is.True);
    }

    [Test]
    public void Constructor_InvalidParameters_ThrowsException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScrollbackManager(-1, 80));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScrollbackManager(100, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScrollbackManager(100, -1));
    }

    [Test]
    public void AddLine_SingleLine_StoresCorrectly()
    {
        // Arrange
        var line = CreateTestLine("Hello World");

        // Act
        _scrollbackManager.AddLine(line);

        // Assert
        Assert.That(_scrollbackManager.CurrentLines, Is.EqualTo(1));
        var retrievedLine = _scrollbackManager.GetLine(0);
        Assert.That(GetLineText(retrievedLine), Is.EqualTo("Hello World" + new string(' ', TestColumns - 11)));
    }

    [Test]
    public void AddLine_ExceedsMaxLines_RemovesOldestLine()
    {
        // Arrange
        for (int i = 0; i < TestMaxLines + 2; i++)
        {
            var line = CreateTestLine($"Line {i}");
            _scrollbackManager.AddLine(line);
        }

        // Act & Assert
        Assert.That(_scrollbackManager.CurrentLines, Is.EqualTo(TestMaxLines));
        
        // Should have lines 2, 3, 4, 5, 6 (oldest lines 0, 1 removed)
        var firstLine = _scrollbackManager.GetLine(0);
        Assert.That(GetLineText(firstLine), Does.StartWith("Line 2"));
        
        var lastLine = _scrollbackManager.GetLine(TestMaxLines - 1);
        Assert.That(GetLineText(lastLine), Does.StartWith("Line 6"));
    }

    [Test]
    public void GetLine_InvalidIndex_ThrowsException()
    {
        // Arrange
        _scrollbackManager.AddLine(CreateTestLine("Test"));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _scrollbackManager.GetLine(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _scrollbackManager.GetLine(1));
    }

    [Test]
    public void Clear_RemovesAllLines()
    {
        // Arrange
        _scrollbackManager.AddLine(CreateTestLine("Line 1"));
        _scrollbackManager.AddLine(CreateTestLine("Line 2"));

        // Act
        _scrollbackManager.Clear();

        // Assert
        Assert.That(_scrollbackManager.CurrentLines, Is.EqualTo(0));
        Assert.That(_scrollbackManager.ViewportOffset, Is.EqualTo(0));
        Assert.That(_scrollbackManager.IsAtBottom, Is.True);
    }

    [Test]
    public void SetViewportOffset_ValidValues_UpdatesCorrectly()
    {
        // Arrange
        _scrollbackManager.AddLine(CreateTestLine("Line 1"));
        _scrollbackManager.AddLine(CreateTestLine("Line 2"));

        // Act & Assert
        _scrollbackManager.SetViewportOffset(1);
        Assert.That(_scrollbackManager.ViewportOffset, Is.EqualTo(1));
        Assert.That(_scrollbackManager.IsAtBottom, Is.False);

        _scrollbackManager.SetViewportOffset(0);
        Assert.That(_scrollbackManager.ViewportOffset, Is.EqualTo(0));
        Assert.That(_scrollbackManager.IsAtBottom, Is.True);
    }

    [Test]
    public void SetViewportOffset_OutOfRange_ClampsToValidRange()
    {
        // Arrange
        _scrollbackManager.AddLine(CreateTestLine("Line 1"));
        _scrollbackManager.AddLine(CreateTestLine("Line 2"));

        // Act & Assert
        _scrollbackManager.SetViewportOffset(-5);
        Assert.That(_scrollbackManager.ViewportOffset, Is.EqualTo(0));

        _scrollbackManager.SetViewportOffset(100);
        Assert.That(_scrollbackManager.ViewportOffset, Is.EqualTo(2)); // Clamped to CurrentLines
    }

    [Test]
    public void GetViewportRows_AlternateScreenActive_ReturnsScreenBufferOnly()
    {
        // Arrange
        _scrollbackManager.AddLine(CreateTestLine("Scrollback Line"));
        var screenBuffer = new ReadOnlyMemory<Cell>[] { CreateTestLine("Screen Line").ToArray().AsMemory() };

        // Act
        var result = _scrollbackManager.GetViewportRows(screenBuffer, isAlternateScreenActive: true, requestedRows: 2);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(GetLineText(result[0].Span), Does.StartWith("Screen Line"));
    }

    [Test]
    public void GetViewportRows_NormalMode_CombinesScrollbackAndScreen()
    {
        // Arrange
        _scrollbackManager.AddLine(CreateTestLine("Scrollback Line 1"));
        _scrollbackManager.AddLine(CreateTestLine("Scrollback Line 2"));
        var screenBuffer = new ReadOnlyMemory<Cell>[] { CreateTestLine("Screen Line").ToArray().AsMemory() };

        // Act
        var result = _scrollbackManager.GetViewportRows(screenBuffer, isAlternateScreenActive: false, requestedRows: 3);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(GetLineText(result[0].Span), Does.StartWith("Scrollback Line 1"));
        Assert.That(GetLineText(result[1].Span), Does.StartWith("Scrollback Line 2"));
        Assert.That(GetLineText(result[2].Span), Does.StartWith("Screen Line"));
    }

    private ReadOnlySpan<Cell> CreateTestLine(string text)
    {
        var cells = new Cell[TestColumns];
        for (int i = 0; i < TestColumns; i++)
        {
            char ch = i < text.Length ? text[i] : ' ';
            cells[i] = new Cell(ch, SgrAttributes.Default);
        }
        return cells;
    }

    private string GetLineText(ReadOnlySpan<Cell> line)
    {
        var chars = new char[line.Length];
        for (int i = 0; i < line.Length; i++)
        {
            chars[i] = line[i].Character;
        }
        return new string(chars);
    }
}