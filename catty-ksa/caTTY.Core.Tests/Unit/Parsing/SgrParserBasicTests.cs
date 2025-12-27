using NUnit.Framework;
using caTTY.Core.Parsing;
using caTTY.Core.Types;
using Microsoft.Extensions.Logging.Abstractions;

namespace caTTY.Core.Tests.Unit.Parsing;

/// <summary>
///     Unit tests for basic SGR parameter parsing functionality.
///     Tests the core requirements for task 3.2: basic colors and styles.
/// </summary>
[TestFixture]
public class SgrParserBasicTests
{
    private SgrParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new SgrParser(NullLogger.Instance);
    }

    [Test]
    public void ParseSgrSequence_Reset_CreatesResetMessage()
    {
        // Arrange
        var raw = "\x1b[0m";
        var escapeSequence = new byte[] { 0x1b, 0x5b, 0x30, 0x6d };

        // Act
        var result = _parser.ParseSgrSequence(escapeSequence, raw);

        // Assert
        Assert.That(result.Type, Is.EqualTo("sgr"));
        Assert.That(result.Messages, Has.Length.EqualTo(1));
        Assert.That(result.Messages[0].Type, Is.EqualTo("sgr.reset"));
        Assert.That(result.Messages[0].Implemented, Is.True);
    }

    [Test]
    public void ParseSgrSequence_Bold_CreatesBoldMessage()
    {
        // Arrange
        var raw = "\x1b[1m";
        var escapeSequence = new byte[] { 0x1b, 0x5b, 0x31, 0x6d };

        // Act
        var result = _parser.ParseSgrSequence(escapeSequence, raw);

        // Assert
        Assert.That(result.Type, Is.EqualTo("sgr"));
        Assert.That(result.Messages, Has.Length.EqualTo(1));
        Assert.That(result.Messages[0].Type, Is.EqualTo("sgr.bold"));
        Assert.That(result.Messages[0].Implemented, Is.True);
    }

    [Test]
    public void ParseSgrSequence_Italic_CreatesItalicMessage()
    {
        // Arrange
        var raw = "\x1b[3m";
        var escapeSequence = new byte[] { 0x1b, 0x5b, 0x33, 0x6d };

        // Act
        var result = _parser.ParseSgrSequence(escapeSequence, raw);

        // Assert
        Assert.That(result.Type, Is.EqualTo("sgr"));
        Assert.That(result.Messages, Has.Length.EqualTo(1));
        Assert.That(result.Messages[0].Type, Is.EqualTo("sgr.italic"));
        Assert.That(result.Messages[0].Implemented, Is.True);
    }

    [Test]
    public void ParseSgrSequence_Underline_CreatesUnderlineMessage()
    {
        // Arrange
        var raw = "\x1b[4m";
        var escapeSequence = new byte[] { 0x1b, 0x5b, 0x34, 0x6d };

        // Act
        var result = _parser.ParseSgrSequence(escapeSequence, raw);

        // Assert
        Assert.That(result.Type, Is.EqualTo("sgr"));
        Assert.That(result.Messages, Has.Length.EqualTo(1));
        Assert.That(result.Messages[0].Type, Is.EqualTo("sgr.underline"));
        Assert.That(result.Messages[0].Implemented, Is.True);
    }

    [Test]
    public void ParseSgrSequence_Strikethrough_CreatesStrikethroughMessage()
    {
        // Arrange
        var raw = "\x1b[9m";
        var escapeSequence = new byte[] { 0x1b, 0x5b, 0x39, 0x6d };

        // Act
        var result = _parser.ParseSgrSequence(escapeSequence, raw);

        // Assert
        Assert.That(result.Type, Is.EqualTo("sgr"));
        Assert.That(result.Messages, Has.Length.EqualTo(1));
        Assert.That(result.Messages[0].Type, Is.EqualTo("sgr.strikethrough"));
        Assert.That(result.Messages[0].Implemented, Is.True);
    }

    [Test]
    public void ParseSgrSequence_RedForeground_CreatesForegroundColorMessage()
    {
        // Arrange
        var raw = "\x1b[31m";
        var escapeSequence = new byte[] { 0x1b, 0x5b, 0x33, 0x31, 0x6d };

        // Act
        var result = _parser.ParseSgrSequence(escapeSequence, raw);

        // Assert
        Assert.That(result.Type, Is.EqualTo("sgr"));
        Assert.That(result.Messages, Has.Length.EqualTo(1));
        Assert.That(result.Messages[0].Type, Is.EqualTo("sgr.foregroundColor"));
        Assert.That(result.Messages[0].Implemented, Is.True);
        
        var color = (Color)result.Messages[0].Data!;
        Assert.That(color.Type, Is.EqualTo(ColorType.Named));
        Assert.That(color.NamedColor, Is.EqualTo(NamedColor.Red));
    }

    [Test]
    public void ParseSgrSequence_BlueBackground_CreatesBackgroundColorMessage()
    {
        // Arrange
        var raw = "\x1b[44m";
        var escapeSequence = new byte[] { 0x1b, 0x5b, 0x34, 0x34, 0x6d };

        // Act
        var result = _parser.ParseSgrSequence(escapeSequence, raw);

        // Assert
        Assert.That(result.Type, Is.EqualTo("sgr"));
        Assert.That(result.Messages, Has.Length.EqualTo(1));
        Assert.That(result.Messages[0].Type, Is.EqualTo("sgr.backgroundColor"));
        Assert.That(result.Messages[0].Implemented, Is.True);
        
        var color = (Color)result.Messages[0].Data!;
        Assert.That(color.Type, Is.EqualTo(ColorType.Named));
        Assert.That(color.NamedColor, Is.EqualTo(NamedColor.Blue));
    }

    [Test]
    public void ParseSgrSequence_MultipleParameters_CreatesMultipleMessages()
    {
        // Arrange - Bold + Red foreground + White background
        var raw = "\x1b[1;31;47m";
        var escapeSequence = new byte[] { 0x1b, 0x5b, 0x31, 0x3b, 0x33, 0x31, 0x3b, 0x34, 0x37, 0x6d };

        // Act
        var result = _parser.ParseSgrSequence(escapeSequence, raw);

        // Assert
        Assert.That(result.Type, Is.EqualTo("sgr"));
        Assert.That(result.Messages, Has.Length.EqualTo(3));
        
        // Bold
        Assert.That(result.Messages[0].Type, Is.EqualTo("sgr.bold"));
        Assert.That(result.Messages[0].Implemented, Is.True);
        
        // Red foreground
        Assert.That(result.Messages[1].Type, Is.EqualTo("sgr.foregroundColor"));
        Assert.That(result.Messages[1].Implemented, Is.True);
        var fgColor = (Color)result.Messages[1].Data!;
        Assert.That(fgColor.NamedColor, Is.EqualTo(NamedColor.Red));
        
        // White background
        Assert.That(result.Messages[2].Type, Is.EqualTo("sgr.backgroundColor"));
        Assert.That(result.Messages[2].Implemented, Is.True);
        var bgColor = (Color)result.Messages[2].Data!;
        Assert.That(bgColor.NamedColor, Is.EqualTo(NamedColor.White));
    }

    [Test]
    public void ParseSgrSequence_ColonSeparator_ParsesCorrectly()
    {
        // Arrange - Underline with colon separator for style
        var raw = "\x1b[4:3m";
        var escapeSequence = new byte[] { 0x1b, 0x5b, 0x34, 0x3a, 0x33, 0x6d };

        // Act
        var result = _parser.ParseSgrSequence(escapeSequence, raw);

        // Assert
        Assert.That(result.Type, Is.EqualTo("sgr"));
        Assert.That(result.Messages, Has.Length.EqualTo(1));
        Assert.That(result.Messages[0].Type, Is.EqualTo("sgr.underline"));
        Assert.That(result.Messages[0].Implemented, Is.True);
        
        var style = (UnderlineStyle)result.Messages[0].Data!;
        Assert.That(style, Is.EqualTo(UnderlineStyle.Curly));
    }

    [Test]
    public void ParseSgrSequence_AllStandardColors_ParsesCorrectly()
    {
        // Test all standard foreground colors (30-37)
        var standardColors = new[]
        {
            (30, NamedColor.Black),
            (31, NamedColor.Red),
            (32, NamedColor.Green),
            (33, NamedColor.Yellow),
            (34, NamedColor.Blue),
            (35, NamedColor.Magenta),
            (36, NamedColor.Cyan),
            (37, NamedColor.White)
        };

        foreach (var (code, expectedColor) in standardColors)
        {
            // Arrange
            var raw = $"\x1b[{code}m";
            var escapeSequence = System.Text.Encoding.UTF8.GetBytes(raw);

            // Act
            var result = _parser.ParseSgrSequence(escapeSequence, raw);

            // Assert
            Assert.That(result.Messages, Has.Length.EqualTo(1), $"Failed for color code {code}");
            Assert.That(result.Messages[0].Type, Is.EqualTo("sgr.foregroundColor"), $"Failed for color code {code}");
            
            var color = (Color)result.Messages[0].Data!;
            Assert.That(color.NamedColor, Is.EqualTo(expectedColor), $"Failed for color code {code}");
        }
    }

    [Test]
    public void TryParseParameters_SemicolonSeparator_ParsesCorrectly()
    {
        // Arrange
        var parameterString = "1;31;47".AsSpan();

        // Act
        var success = _parser.TryParseParameters(parameterString, out var parameters);

        // Assert
        Assert.That(success, Is.True);
        Assert.That(parameters, Is.EqualTo(new[] { 1, 31, 47 }));
    }

    [Test]
    public void TryParseParameters_ColonSeparator_ParsesCorrectly()
    {
        // Arrange
        var parameterString = "4:3".AsSpan();

        // Act
        var success = _parser.TryParseParameters(parameterString, out var parameters);

        // Assert
        Assert.That(success, Is.True);
        Assert.That(parameters, Is.EqualTo(new[] { 4, 3 }));
    }

    [Test]
    public void TryParseParameters_MixedSeparators_ParsesCorrectly()
    {
        // Arrange
        var parameterString = "1;4:3;31".AsSpan();

        // Act
        var success = _parser.TryParseParameters(parameterString, out var parameters);

        // Assert
        Assert.That(success, Is.True);
        Assert.That(parameters, Is.EqualTo(new[] { 1, 4, 3, 31 }));
    }
}