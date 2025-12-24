using NUnit.Framework;
using caTTY.Core.Parsing;
using System.Text;

namespace caTTY.Core.Tests.Unit.Parsing;

[TestFixture]
[Category("Unit")]
public class CsiParserBasicTests
{
    private CsiParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new CsiParser();
    }

    [Test]
    public void CsiParser_CanBeCreated()
    {
        var parser = new CsiParser();
        Assert.That(parser, Is.Not.Null);
    }

    [Test]
    public void GetParameter_WithValidIndex_ReturnsValue()
    {
        var parameters = new[] { 10, 20, 30 };
        
        var result = _parser.GetParameter(parameters, 1, 99);
        
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void GetParameter_WithInvalidIndex_ReturnsFallback()
    {
        var parameters = new[] { 10, 20 };
        
        Assert.That(_parser.GetParameter(parameters, 5, 99), Is.EqualTo(99));
        Assert.That(_parser.GetParameter(parameters, -1, 99), Is.EqualTo(99));
    }

    [Test]
    public void TryParseParameters_EmptyString_ReturnsEmptyArray()
    {
        var result = _parser.TryParseParameters("".AsSpan(), out var parameters, out var isPrivate, out var prefix);
        
        Assert.That(result, Is.True);
        Assert.That(parameters, Is.Empty);
        Assert.That(isPrivate, Is.False);
        Assert.That(prefix, Is.Null);
    }

    [Test]
    public void TryParseParameters_PrivateMode_SetsPrivateFlag()
    {
        var result = _parser.TryParseParameters("?1;2".AsSpan(), out var parameters, out var isPrivate, out var prefix);
        
        Assert.That(result, Is.True);
        Assert.That(parameters, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(isPrivate, Is.True);
        Assert.That(prefix, Is.Null);
    }

    [Test]
    public void TryParseParameters_PrefixMode_SetsPrefix()
    {
        var result = _parser.TryParseParameters(">4;5".AsSpan(), out var parameters, out var isPrivate, out var prefix);
        
        Assert.That(result, Is.True);
        Assert.That(parameters, Is.EqualTo(new[] { 4, 5 }));
        Assert.That(isPrivate, Is.False);
        Assert.That(prefix, Is.EqualTo(">"));
    }

    [Test]
    public void TryParseParameters_SemicolonSeparated_ParsesCorrectly()
    {
        var result = _parser.TryParseParameters("1;2;3".AsSpan(), out var parameters, out var isPrivate, out var prefix);
        
        Assert.That(result, Is.True);
        Assert.That(parameters, Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(isPrivate, Is.False);
        Assert.That(prefix, Is.Null);
    }

    [Test]
    public void ParseCsiSequence_CursorUp_ParsesCorrectly()
    {
        var sequence = Encoding.ASCII.GetBytes("\x1b[5A");
        var result = _parser.ParseCsiSequence(sequence, "\x1b[5A");
        
        Assert.That(result.Type, Is.EqualTo("csi.cursorUp"));
        Assert.That(result.Implemented, Is.True);
        Assert.That(result.Count, Is.EqualTo(5));
        Assert.That(result.Parameters, Is.EqualTo(new[] { 5 }));
    }

    [Test]
    public void ParseCsiSequence_CursorPosition_ParsesCorrectly()
    {
        var sequence = Encoding.ASCII.GetBytes("\x1b[10;20H");
        var result = _parser.ParseCsiSequence(sequence, "\x1b[10;20H");
        
        Assert.That(result.Type, Is.EqualTo("csi.cursorPosition"));
        Assert.That(result.Implemented, Is.True);
        Assert.That(result.Row, Is.EqualTo(10));
        Assert.That(result.Column, Is.EqualTo(20));
        Assert.That(result.Parameters, Is.EqualTo(new[] { 10, 20 }));
    }

    [Test]
    public void ParseCsiSequence_DecModeSet_ParsesCorrectly()
    {
        var sequence = Encoding.ASCII.GetBytes("\x1b[?1;2h");
        var result = _parser.ParseCsiSequence(sequence, "\x1b[?1;2h");
        
        Assert.That(result.Type, Is.EqualTo("csi.decModeSet"));
        Assert.That(result.Implemented, Is.True);
        Assert.That(result.DecModes, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(result.Parameters, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void ParseCsiSequence_EraseInDisplay_ParsesCorrectly()
    {
        var sequence = Encoding.ASCII.GetBytes("\x1b[2J");
        var result = _parser.ParseCsiSequence(sequence, "\x1b[2J");
        
        Assert.That(result.Type, Is.EqualTo("csi.eraseInDisplay"));
        Assert.That(result.Implemented, Is.True);
        Assert.That(result.Mode, Is.EqualTo(2));
    }

    [Test]
    public void ParseCsiSequence_UnknownSequence_ReturnsUnknown()
    {
        var sequence = Encoding.ASCII.GetBytes("\x1b[99z");
        var result = _parser.ParseCsiSequence(sequence, "\x1b[99z");
        
        Assert.That(result.Type, Is.EqualTo("csi.unknown"));
        Assert.That(result.Implemented, Is.False);
        Assert.That(result.Parameters, Is.EqualTo(new[] { 99 }));
    }

    [Test]
    public void ParseCsiSequence_TabCommands_ParseCorrectly()
    {
        // Forward tab
        var forwardTab = Encoding.ASCII.GetBytes("\x1b[3I");
        var result1 = _parser.ParseCsiSequence(forwardTab, "\x1b[3I");
        
        Assert.That(result1.Type, Is.EqualTo("csi.cursorForwardTab"));
        Assert.That(result1.Implemented, Is.True);
        Assert.That(result1.Count, Is.EqualTo(3));

        // Backward tab
        var backwardTab = Encoding.ASCII.GetBytes("\x1b[2Z");
        var result2 = _parser.ParseCsiSequence(backwardTab, "\x1b[2Z");
        
        Assert.That(result2.Type, Is.EqualTo("csi.cursorBackwardTab"));
        Assert.That(result2.Implemented, Is.True);
        Assert.That(result2.Count, Is.EqualTo(2));

        // Tab clear
        var tabClear = Encoding.ASCII.GetBytes("\x1b[3g");
        var result3 = _parser.ParseCsiSequence(tabClear, "\x1b[3g");
        
        Assert.That(result3.Type, Is.EqualTo("csi.tabClear"));
        Assert.That(result3.Implemented, Is.True);
        Assert.That(result3.Mode, Is.EqualTo(3));
    }

    [Test]
    public void ParseCsiSequence_DeviceQueries_ParseCorrectly()
    {
        // Primary DA
        var primaryDa = Encoding.ASCII.GetBytes("\x1b[c");
        var result1 = _parser.ParseCsiSequence(primaryDa, "\x1b[c");
        
        Assert.That(result1.Type, Is.EqualTo("csi.deviceAttributesPrimary"));
        Assert.That(result1.Implemented, Is.True);

        // Secondary DA
        var secondaryDa = Encoding.ASCII.GetBytes("\x1b[>c");
        var result2 = _parser.ParseCsiSequence(secondaryDa, "\x1b[>c");
        
        Assert.That(result2.Type, Is.EqualTo("csi.deviceAttributesSecondary"));
        Assert.That(result2.Implemented, Is.True);

        // Cursor position report
        var cpr = Encoding.ASCII.GetBytes("\x1b[6n");
        var result3 = _parser.ParseCsiSequence(cpr, "\x1b[6n");
        
        Assert.That(result3.Type, Is.EqualTo("csi.cursorPositionReport"));
        Assert.That(result3.Implemented, Is.True);
    }
}