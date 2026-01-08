using caTTY.Core.Rpc;
using caTTY.TermSequenceRpc;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace caTTY.TermSequenceRpc.Tests.Unit;

/// <summary>
/// Unit tests for KSA-specific OSC RPC handler.
/// Note: Full integration testing requires actual KSA game context
/// or sophisticated mocking of Program.ControlledVehicle.
/// </summary>
[TestFixture]
[Category("Unit")]
public class KsaOscRpcHandlerTests
{
    private KsaOscRpcHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new KsaOscRpcHandler(NullLogger.Instance);
    }

    #region Instantiation Tests

    [Test]
    public void Constructor_WithLogger_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => new KsaOscRpcHandler(NullLogger.Instance));
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new KsaOscRpcHandler(null!));
    }

    #endregion

    #region Actions Constants Tests

    [Test]
    public void Actions_EngineIgnite_HasCorrectValue()
    {
        Assert.That(KsaOscRpcHandler.Actions.EngineIgnite, Is.EqualTo("engine_ignite"));
    }

    [Test]
    public void Actions_EngineShutdown_HasCorrectValue()
    {
        Assert.That(KsaOscRpcHandler.Actions.EngineShutdown, Is.EqualTo("engine_shutdown"));
    }

    #endregion

    #region HandleCommand Tests

    // NOTE: These tests verify the handler doesn't crash when called.
    // Full integration testing would require mocking Program.ControlledVehicle,
    // which is a global static from the KSA game engine.

    [Test]
    public void HandleCommand_EngineIgnite_DoesNotThrow()
    {
        // Arrange
        string payload = "{\"action\":\"engine_ignite\"}";

        // Act & Assert - should not throw even if Program.ControlledVehicle is null
        Assert.DoesNotThrow(() => _handler.HandleCommand(OscRpcHandler.JsonActionCommand, payload));
    }

    [Test]
    public void HandleCommand_EngineShutdown_DoesNotThrow()
    {
        // Arrange
        string payload = "{\"action\":\"engine_shutdown\"}";

        // Act & Assert - should not throw even if Program.ControlledVehicle is null
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

    #endregion

    // TODO: Integration tests
    // To fully test KSA game integration:
    // 1. Create mock IVehicleController interface
    // 2. Inject it into command handlers instead of using Program.ControlledVehicle
    // 3. Verify SetEnum calls with expected VehicleEngine values
    // 4. OR run tests in actual KSA game context (more complex setup)
}
