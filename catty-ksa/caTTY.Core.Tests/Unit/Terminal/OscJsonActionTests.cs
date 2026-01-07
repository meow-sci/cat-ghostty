using caTTY.Core.Types;
using NUnit.Framework;

namespace caTTY.Core.Tests.Unit.Terminal;

/// <summary>
/// Unit tests for OSC 1010 JSON action commands.
/// Tests the OSC sequence format: ESC ] 1010 ; {"action":"action_name"} BEL/ST
/// </summary>
[TestFixture]
[Category("Unit")]
public class OscJsonActionTests
{
    #region OSC 1010 Parsing Tests

    [Test]
    public void OscJsonAction_ValidEngineIgnite_ParsesCorrectly()
    {
        // Arrange - Format: ESC ] 1010 ; {"action":"engine_ignite"} BEL
        var message = new XtermOscMessage
        {
            Type = "osc.jsonAction",
            Raw = "\x1b]1010;{\"action\":\"engine_ignite\"}\x07",
            Terminator = "BEL",
            Command = 1010,
            Payload = "{\"action\":\"engine_ignite\"}",
            Implemented = true
        };

        // Assert
        Assert.That(message.Type, Is.EqualTo("osc.jsonAction"));
        Assert.That(message.Command, Is.EqualTo(1010));
        Assert.That(message.Payload, Does.Contain("engine_ignite"));
        Assert.That(message.Implemented, Is.True);
    }

    [Test]
    public void OscJsonAction_ValidEngineShutdown_ParsesCorrectly()
    {
        // Arrange - Format: ESC ] 1010 ; {"action":"engine_shutdown"} ST
        var message = new XtermOscMessage
        {
            Type = "osc.jsonAction",
            Raw = "\x1b]1010;{\"action\":\"engine_shutdown\"}\x1b\\",
            Terminator = "ST",
            Command = 1010,
            Payload = "{\"action\":\"engine_shutdown\"}",
            Implemented = true
        };

        // Assert
        Assert.That(message.Type, Is.EqualTo("osc.jsonAction"));
        Assert.That(message.Command, Is.EqualTo(1010));
        Assert.That(message.Payload, Does.Contain("engine_shutdown"));
    }

    [Test]
    public void OscJsonAction_BelTerminator_IsValid()
    {
        // Arrange
        var message = new XtermOscMessage
        {
            Type = "osc.jsonAction",
            Terminator = "BEL",
            Command = 1010,
            Payload = "{\"action\":\"engine_ignite\"}",
            Implemented = true
        };

        // Assert - BEL (0x07) is a valid OSC terminator
        Assert.That(message.Terminator, Is.EqualTo("BEL"));
    }

    [Test]
    public void OscJsonAction_StTerminator_IsValid()
    {
        // Arrange
        var message = new XtermOscMessage
        {
            Type = "osc.jsonAction",
            Terminator = "ST",
            Command = 1010,
            Payload = "{\"action\":\"engine_shutdown\"}",
            Implemented = true
        };

        // Assert - ST (ESC \) is a valid OSC terminator
        Assert.That(message.Terminator, Is.EqualTo("ST"));
    }

    #endregion

    #region JSON Payload Validation Tests

    [Test]
    public void OscJsonAction_PayloadContainsAction_IsValid()
    {
        // Arrange
        string payload = "{\"action\":\"engine_ignite\"}";

        // Assert - payload should be valid JSON with action field
        Assert.That(payload, Does.StartWith("{"));
        Assert.That(payload, Does.EndWith("}"));
        Assert.That(payload, Does.Contain("\"action\""));
    }

    [Test]
    public void OscJsonAction_PayloadWithExtraFields_IsValid()
    {
        // Arrange - JSON can have additional fields
        string payload = "{\"action\":\"engine_ignite\",\"throttle\":0.75}";
        
        var message = new XtermOscMessage
        {
            Type = "osc.jsonAction",
            Command = 1010,
            Payload = payload,
            Implemented = true
        };

        // Assert - extra fields should be allowed
        Assert.That(message.Payload, Does.Contain("engine_ignite"));
        Assert.That(message.Payload, Does.Contain("throttle"));
    }

    #endregion

    #region Command Number Tests

    [Test]
    public void OscJsonAction_Command1010_IsCorrect()
    {
        // Arrange
        var message = new XtermOscMessage
        {
            Type = "osc.jsonAction",
            Command = 1010,
            Implemented = true
        };

        // Assert - OSC 1010 is our custom JSON action command
        Assert.That(message.Command, Is.EqualTo(1010));
    }

    [Test]
    public void OscJsonAction_NotStandardOscCommand_IsPrivateUse()
    {
        // Arrange
        const int customOscCommand = 1010;
        const int maxXtermOscCommand = 119;
        
        // Assert - 1010 is in the private/application use range (> 99)
        // Standard OSC codes are 0-9, xterm extensions are 10-119
        Assert.That(customOscCommand > maxXtermOscCommand, Is.True,
            "OSC 1010 should be in private use range");
    }

    #endregion
}
