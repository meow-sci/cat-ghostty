using caTTY.Core.Rpc;
using caTTY.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace caTTY.Core.Tests.Unit.Rpc;

/// <summary>
/// Unit tests for OSC-based RPC commands.
/// Tests the OSC sequence format: ESC ] {command} ; {payload} BEL/ST
/// </summary>
[TestFixture]
[Category("Unit")]
public class OscRpcHandlerTests
{
    private OscRpcHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new OscRpcHandler(NullLogger.Instance);
    }

    #region IsPrivateCommand Tests

    [Test]
    public void IsPrivateCommand_Command1010_ReturnsTrue()
    {
        Assert.That(_handler.IsPrivateCommand(1010), Is.True);
    }

    [Test]
    public void IsPrivateCommand_Command1000_ReturnsTrue()
    {
        Assert.That(_handler.IsPrivateCommand(1000), Is.True);
    }

    [Test]
    public void IsPrivateCommand_Command999_ReturnsFalse()
    {
        Assert.That(_handler.IsPrivateCommand(999), Is.False);
    }

    [Test]
    public void IsPrivateCommand_StandardOscCommand_ReturnsFalse()
    {
        // OSC 0, 1, 2 are standard title commands
        Assert.That(_handler.IsPrivateCommand(0), Is.False);
        Assert.That(_handler.IsPrivateCommand(1), Is.False);
        Assert.That(_handler.IsPrivateCommand(2), Is.False);
        // OSC 52 is clipboard
        Assert.That(_handler.IsPrivateCommand(52), Is.False);
    }

    #endregion

    #region HandleCommand Tests

    [Test]
    public void HandleCommand_ValidEngineIgnite_DoesNotThrow()
    {
        // Arrange
        string payload = "{\"action\":\"engine_ignite\"}";

        // Act & Assert - should not throw
        Assert.DoesNotThrow(() => _handler.HandleCommand(OscRpcHandler.JsonActionCommand, payload));
    }

    [Test]
    public void HandleCommand_ValidEngineShutdown_DoesNotThrow()
    {
        // Arrange
        string payload = "{\"action\":\"engine_shutdown\"}";

        // Act & Assert - should not throw
        Assert.DoesNotThrow(() => _handler.HandleCommand(OscRpcHandler.JsonActionCommand, payload));
    }

    [Test]
    public void HandleCommand_EmptyPayload_DoesNotThrow()
    {
        // Act & Assert - should handle gracefully
        Assert.DoesNotThrow(() => _handler.HandleCommand(OscRpcHandler.JsonActionCommand, null));
        Assert.DoesNotThrow(() => _handler.HandleCommand(OscRpcHandler.JsonActionCommand, ""));
    }

    [Test]
    public void HandleCommand_InvalidJson_DoesNotThrow()
    {
        // Arrange
        string payload = "not valid json";

        // Act & Assert - should handle gracefully
        Assert.DoesNotThrow(() => _handler.HandleCommand(OscRpcHandler.JsonActionCommand, payload));
    }

    [Test]
    public void HandleCommand_MissingActionProperty_DoesNotThrow()
    {
        // Arrange
        string payload = "{\"something\":\"else\"}";

        // Act & Assert - should handle gracefully
        Assert.DoesNotThrow(() => _handler.HandleCommand(OscRpcHandler.JsonActionCommand, payload));
    }

    [Test]
    public void HandleCommand_UnknownAction_DoesNotThrow()
    {
        // Arrange
        string payload = "{\"action\":\"unknown_action\"}";

        // Act & Assert - should handle gracefully
        Assert.DoesNotThrow(() => _handler.HandleCommand(OscRpcHandler.JsonActionCommand, payload));
    }

    [Test]
    public void HandleCommand_UnknownCommand_DoesNotThrow()
    {
        // Arrange
        int unknownCommand = 9999;
        string payload = "{}";

        // Act & Assert - should handle gracefully
        Assert.DoesNotThrow(() => _handler.HandleCommand(unknownCommand, payload));
    }

    #endregion

    #region Constants Tests

    [Test]
    public void JsonActionCommand_Is1010()
    {
        Assert.That(OscRpcHandler.JsonActionCommand, Is.EqualTo(1010));
    }

    [Test]
    public void Actions_EngineIgnite_IsCorrect()
    {
        Assert.That(OscRpcHandler.Actions.EngineIgnite, Is.EqualTo("engine_ignite"));
    }

    [Test]
    public void Actions_EngineShutdown_IsCorrect()
    {
        Assert.That(OscRpcHandler.Actions.EngineShutdown, Is.EqualTo("engine_shutdown"));
    }

    #endregion

    #region OSC Message Parsing Integration Tests

    [Test]
    public void OscPrivateMessage_Command1010_HasCorrectType()
    {
        // Arrange - this is how the OscParser would create the message
        var message = new XtermOscMessage
        {
            Type = "osc.private",
            Raw = "\x1b]1010;{\"action\":\"engine_ignite\"}\x07",
            Terminator = "BEL",
            Command = 1010,
            Payload = "{\"action\":\"engine_ignite\"}",
            Implemented = true
        };

        // Assert - type should be "osc.private" for all private-use commands
        Assert.That(message.Type, Is.EqualTo("osc.private"));
        Assert.That(message.Command, Is.EqualTo(1010));
        Assert.That(_handler.IsPrivateCommand(message.Command), Is.True);
    }

    [Test]
    public void OscPrivateMessage_PrivateRange_StartsAt1000()
    {
        // Private-use OSC commands start at 1000
        const int privateRangeStart = 1000;
        
        Assert.That(_handler.IsPrivateCommand(privateRangeStart), Is.True);
        Assert.That(_handler.IsPrivateCommand(privateRangeStart - 1), Is.False);
    }

    #endregion
}
