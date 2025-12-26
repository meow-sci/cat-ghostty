using caTTY.Core.Parsing;
using caTTY.Core.Types;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace caTTY.Core.Tests.Unit.Parsing;

/// <summary>
///     Unit tests for the OSC sequence parser.
/// </summary>
[TestFixture]
[Category("Unit")]
public class OscParserTests
{
    private OscParser _parser = null!;
    private TestLogger _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new TestLogger();
        _parser = new OscParser(_logger);
    }

    [Test]
    public void ProcessOscByte_BelTerminator_ReturnsCompleteMessage()
    {
        // Arrange
        var escapeSequence = new List<byte> { 0x1b, 0x5d, 0x30, 0x3b }; // ESC ] 0 ;
        byte b = 0x07; // BEL

        // Act
        bool isComplete = _parser.ProcessOscByte(b, escapeSequence, out OscMessage? message);

        // Assert
        Assert.That(isComplete, Is.True);
        Assert.That(message, Is.Not.Null);
        Assert.That(message!.Type, Is.EqualTo("osc"));
        Assert.That(message.Raw, Is.EqualTo("\x1b]0;\x07"));
        Assert.That(message.Terminator, Is.EqualTo("BEL"));
        Assert.That(message.Implemented, Is.False);
        Assert.That(escapeSequence, Has.Count.EqualTo(5));
        Assert.That(escapeSequence[4], Is.EqualTo(0x07));
    }

    [Test]
    public void ProcessOscByte_ValidByte_ContinuesParsing()
    {
        // Arrange
        var escapeSequence = new List<byte> { 0x1b, 0x5d }; // ESC ]
        byte b = 0x30; // 0

        // Act
        bool isComplete = _parser.ProcessOscByte(b, escapeSequence, out OscMessage? message);

        // Assert
        Assert.That(isComplete, Is.False);
        Assert.That(message, Is.Null);
        Assert.That(escapeSequence, Has.Count.EqualTo(3));
        Assert.That(escapeSequence[2], Is.EqualTo(0x30));
    }

    [Test]
    public void ProcessOscByte_EscByte_ContinuesParsing()
    {
        // Arrange
        var escapeSequence = new List<byte> { 0x1b, 0x5d, 0x30 }; // ESC ] 0
        byte b = 0x1b; // ESC

        // Act
        bool isComplete = _parser.ProcessOscByte(b, escapeSequence, out OscMessage? message);

        // Assert
        Assert.That(isComplete, Is.False);
        Assert.That(message, Is.Null);
        Assert.That(escapeSequence, Has.Count.EqualTo(4));
        Assert.That(escapeSequence[3], Is.EqualTo(0x1b));
    }

    [Test]
    public void ProcessOscByte_ByteOutOfRange_LogsWarningAndContinues()
    {
        // Arrange
        var escapeSequence = new List<byte> { 0x1b, 0x5d }; // ESC ]
        byte b = 0x10; // Control character (out of range)

        // Act
        bool isComplete = _parser.ProcessOscByte(b, escapeSequence, out OscMessage? message);

        // Assert
        Assert.That(isComplete, Is.False);
        Assert.That(message, Is.Null);
        Assert.That(_logger.LogMessages, Has.Some.Contains("OSC: byte out of range 0x10"));
        Assert.That(escapeSequence, Has.Count.EqualTo(2)); // Byte not added
    }

    [Test]
    public void ProcessOscEscapeByte_StTerminator_ReturnsCompleteMessage()
    {
        // Arrange
        var escapeSequence = new List<byte> { 0x1b, 0x5d, 0x30, 0x3b, 0x1b }; // ESC ] 0 ; ESC
        byte b = 0x5c; // \

        // Act
        bool isComplete = _parser.ProcessOscEscapeByte(b, escapeSequence, out OscMessage? message);

        // Assert
        Assert.That(isComplete, Is.True);
        Assert.That(message, Is.Not.Null);
        Assert.That(message!.Type, Is.EqualTo("osc"));
        Assert.That(message.Raw, Is.EqualTo("\x1b]0;\x1b\\"));
        Assert.That(message.Terminator, Is.EqualTo("ST"));
        Assert.That(message.Implemented, Is.False);
        Assert.That(escapeSequence, Has.Count.EqualTo(6));
        Assert.That(escapeSequence[5], Is.EqualTo(0x5c));
    }

    [Test]
    public void ProcessOscEscapeByte_BelAfterEsc_ReturnsCompleteMessage()
    {
        // Arrange
        var escapeSequence = new List<byte> { 0x1b, 0x5d, 0x30, 0x3b, 0x1b }; // ESC ] 0 ; ESC
        byte b = 0x07; // BEL

        // Act
        bool isComplete = _parser.ProcessOscEscapeByte(b, escapeSequence, out OscMessage? message);

        // Assert
        Assert.That(isComplete, Is.True);
        Assert.That(message, Is.Not.Null);
        Assert.That(message!.Type, Is.EqualTo("osc"));
        Assert.That(message.Raw, Is.EqualTo("\x1b]0;\x1b\x07"));
        Assert.That(message.Terminator, Is.EqualTo("BEL"));
        Assert.That(message.Implemented, Is.False);
    }

    [Test]
    public void ProcessOscEscapeByte_OtherByte_ContinuesParsing()
    {
        // Arrange
        var escapeSequence = new List<byte> { 0x1b, 0x5d, 0x30, 0x3b, 0x1b }; // ESC ] 0 ; ESC
        byte b = 0x41; // A

        // Act
        bool isComplete = _parser.ProcessOscEscapeByte(b, escapeSequence, out OscMessage? message);

        // Assert
        Assert.That(isComplete, Is.False);
        Assert.That(message, Is.Null);
        Assert.That(escapeSequence, Has.Count.EqualTo(6));
        Assert.That(escapeSequence[5], Is.EqualTo(0x41));
    }

    [Test]
    public void ProcessOscByte_ComplexSequence_HandlesCorrectly()
    {
        // Arrange - OSC 0;title BEL sequence
        var escapeSequence = new List<byte> { 0x1b, 0x5d }; // ESC ]
        byte[] titleBytes = { 0x30, 0x3b, 0x54, 0x65, 0x73, 0x74 }; // 0;Test

        // Act - Process title bytes
        bool isComplete = false;
        OscMessage? message = null;
        foreach (byte titleByte in titleBytes)
        {
            isComplete = _parser.ProcessOscByte(titleByte, escapeSequence, out message);
            if (isComplete) break;
        }

        // Process BEL terminator
        if (!isComplete)
        {
            isComplete = _parser.ProcessOscByte(0x07, escapeSequence, out message);
        }

        // Assert
        Assert.That(isComplete, Is.True);
        Assert.That(message, Is.Not.Null);
        Assert.That(message!.Raw, Is.EqualTo("\x1b]0;Test\x07"));
        Assert.That(message.Terminator, Is.EqualTo("BEL"));
        Assert.That(_logger.LogMessages, Has.Some.Contains("OSC (opaque, BEL): \x1b]0;Test\x07"));
    }
}