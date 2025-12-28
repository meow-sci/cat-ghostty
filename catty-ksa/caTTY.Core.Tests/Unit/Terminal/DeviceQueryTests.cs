using System.Text;
using caTTY.Core.Terminal;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace caTTY.Core.Tests.Unit.Terminal;

/// <summary>
///     Tests for device query sequences and responses.
///     Validates Requirements 11.1, 11.2, 27.1, 27.2.
/// </summary>
[TestFixture]
public class DeviceQueryTests
{
    [SetUp]
    public void SetUp()
    {
        _terminal = new TerminalEmulator(80, 24, NullLogger.Instance);
        _responses = new List<string>();

        // Capture responses
        _terminal.ResponseEmitted += (sender, args) =>
        {
            string responseText = Encoding.UTF8.GetString(args.ResponseData.Span);
            _responses.Add(responseText);
        };
    }

    [TearDown]
    public void TearDown()
    {
        _terminal.Dispose();
    }

    private TerminalEmulator _terminal = null!;
    private List<string> _responses = null!;

    [Test]
    public void DeviceAttributesPrimary_ShouldRespondWithVT100Capabilities()
    {
        // Act: Send primary DA query (CSI c)
        _terminal.Write("\x1b[c");

        // Assert: Should respond with VT100 with Advanced Video Option
        Assert.That(_responses, Has.Count.EqualTo(1));
        Assert.That(_responses[0], Is.EqualTo("\x1b[?1;2c"));
    }

    [Test]
    public void DeviceAttributesPrimary_WithZeroParameter_ShouldRespondWithVT100Capabilities()
    {
        // Act: Send primary DA query with explicit 0 parameter (CSI 0 c)
        _terminal.Write("\x1b[0c");

        // Assert: Should respond with VT100 with Advanced Video Option
        Assert.That(_responses, Has.Count.EqualTo(1));
        Assert.That(_responses[0], Is.EqualTo("\x1b[?1;2c"));
    }

    [Test]
    public void DeviceAttributesSecondary_ShouldRespondWithTerminalVersion()
    {
        // Act: Send secondary DA query (CSI > c)
        _terminal.Write("\x1b[>c");

        // Assert: Should respond with VT100 compatible terminal, version 0
        Assert.That(_responses, Has.Count.EqualTo(1));
        Assert.That(_responses[0], Is.EqualTo("\x1b[>0;0;0c"));
    }

    [Test]
    public void DeviceAttributesSecondary_WithZeroParameter_ShouldRespondWithTerminalVersion()
    {
        // Act: Send secondary DA query with explicit 0 parameter (CSI > 0 c)
        _terminal.Write("\x1b[>0c");

        // Assert: Should respond with VT100 compatible terminal, version 0
        Assert.That(_responses, Has.Count.EqualTo(1));
        Assert.That(_responses[0], Is.EqualTo("\x1b[>0;0;0c"));
    }

    [Test]
    public void DeviceStatusReport_ShouldRespondWithReady()
    {
        // Act: Send DSR query (CSI 5 n)
        _terminal.Write("\x1b[5n");

        // Assert: Should respond with ready status
        Assert.That(_responses, Has.Count.EqualTo(1));
        Assert.That(_responses[0], Is.EqualTo("\x1b[0n"));
    }

    [Test]
    public void CursorPositionReport_ShouldRespondWithCurrentPosition()
    {
        // Arrange: Move cursor to a specific position
        _terminal.Write("\x1b[5;10H"); // Move to row 5, column 10

        // Act: Send CPR query (CSI 6 n)
        _terminal.Write("\x1b[6n");

        // Assert: Should respond with current cursor position (1-indexed)
        Assert.That(_responses, Has.Count.EqualTo(1));
        Assert.That(_responses[0], Is.EqualTo("\x1b[5;10R"));
    }

    [Test]
    public void CursorPositionReport_AtOrigin_ShouldRespondWithOneOne()
    {
        // Act: Send CPR query at origin (CSI 6 n)
        _terminal.Write("\x1b[6n");

        // Assert: Should respond with position 1,1 (1-indexed)
        Assert.That(_responses, Has.Count.EqualTo(1));
        Assert.That(_responses[0], Is.EqualTo("\x1b[1;1R"));
    }

    [Test]
    public void TerminalSizeQuery_ShouldRespondWithDimensions()
    {
        // Act: Send terminal size query (CSI 18 t)
        _terminal.Write("\x1b[18t");

        // Assert: Should respond with terminal dimensions
        Assert.That(_responses, Has.Count.EqualTo(1));
        Assert.That(_responses[0], Is.EqualTo("\x1b[8;24;80t")); // 24 rows, 80 columns
    }

    [Test]
    public void CharacterSetQuery_ShouldRespondWithCurrentCharset()
    {
        // Act: Send character set query (CSI ? 26 n)
        _terminal.Write("\x1b[?26n");

        // Assert: Should respond with current character set (UTF-8 by default)
        Assert.That(_responses, Has.Count.EqualTo(1));
        Assert.That(_responses[0], Is.EqualTo("\x1b[?26;utf-8\x1b\\")); // UTF-8 mode enabled by default
    }

    [Test]
    public void MultipleQueries_ShouldRespondToEach()
    {
        // Act: Send multiple queries in sequence
        _terminal.Write("\x1b[c"); // Primary DA
        _terminal.Write("\x1b[5n"); // DSR
        _terminal.Write("\x1b[6n"); // CPR

        // Assert: Should respond to all queries
        Assert.That(_responses, Has.Count.EqualTo(3));
        Assert.That(_responses[0], Is.EqualTo("\x1b[?1;2c")); // Primary DA response
        Assert.That(_responses[1], Is.EqualTo("\x1b[0n")); // DSR response
        Assert.That(_responses[2], Is.EqualTo("\x1b[1;1R")); // CPR response
    }

    [Test]
    public void DeviceResponses_StaticMethods_ShouldGenerateCorrectResponses()
    {
        // Test static response generation methods directly
        Assert.That(DeviceResponses.GenerateDeviceAttributesPrimaryResponse(),
            Is.EqualTo("\x1b[?1;2c"));

        Assert.That(DeviceResponses.GenerateDeviceAttributesSecondaryResponse(),
            Is.EqualTo("\x1b[>0;0;0c"));

        Assert.That(DeviceResponses.GenerateDeviceStatusReportResponse(),
            Is.EqualTo("\x1b[0n"));

        Assert.That(DeviceResponses.GenerateCursorPositionReport(9, 4),
            Is.EqualTo("\x1b[5;10R")); // 0-indexed to 1-indexed conversion

        Assert.That(DeviceResponses.GenerateTerminalSizeResponse(25, 132),
            Is.EqualTo("\x1b[8;25;132t"));

        Assert.That(DeviceResponses.GenerateCharacterSetQueryResponse(),
            Is.EqualTo("\x1b[?26;0n"));

        Assert.That(DeviceResponses.GenerateCharacterSetQueryResponse("B"),
            Is.EqualTo("\x1b[?26;Bn"));
    }
}
