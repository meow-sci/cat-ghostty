using NUnit.Framework;
using caTTY.Core.Rpc;
using System.Reflection;

namespace caTTY.Core.Tests.Unit.Rpc;

/// <summary>
/// Unit tests for specific vehicle command implementations.
/// Tests the IgniteMainThrottle, ShutdownMainEngine, and GetThrottleStatus commands.
/// </summary>
[TestFixture]
[Category("Unit")]
public class VehicleCommandTests
{
    private RpcParameters _emptyParameters = null!;

    [SetUp]
    public void SetUp()
    {
        _emptyParameters = new RpcParameters();
    }

    #region IgniteMainThrottle Command Tests

    [Test]
    public void IgniteMainThrottleCommand_ShouldBeFireAndForget()
    {
        // Arrange & Act
        var command = CreateIgniteMainThrottleCommand();

        // Assert
        Assert.That(command.IsFireAndForget, Is.True, "IgniteMainThrottle should be a fire-and-forget command");
        Assert.That(command.Timeout, Is.EqualTo(TimeSpan.Zero), "Fire-and-forget commands should have zero timeout");
    }

    [Test]
    public void IgniteMainThrottleCommand_ShouldHaveCorrectDescription()
    {
        // Arrange & Act
        var command = CreateIgniteMainThrottleCommand();

        // Assert
        Assert.That(command.Description, Is.EqualTo("Ignite Main Throttle"), "Command should have correct description");
    }

    [Test]
    public async Task IgniteMainThrottleCommand_ExecuteAsync_ShouldReturnNull()
    {
        // Arrange
        var command = CreateIgniteMainThrottleCommand();

        // Act
        var result = await command.ExecuteAsync(_emptyParameters);

        // Assert
        Assert.That(result, Is.Null, "Fire-and-forget commands should return null");
    }

    [Test]
    public async Task IgniteMainThrottleCommand_ExecuteAsync_ShouldAcceptEmptyParameters()
    {
        // Arrange
        var command = CreateIgniteMainThrottleCommand();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await command.ExecuteAsync(_emptyParameters),
            "Command should accept empty parameters");
    }

    [Test]
    public void IgniteMainThrottleCommand_ValidateParameters_ShouldAcceptEmptyParameters()
    {
        // Arrange
        var command = CreateIgniteMainThrottleCommand();

        // Act
        var isValid = InvokeValidateParameters(command, _emptyParameters);

        // Assert
        Assert.That(isValid, Is.True, "Command should accept empty parameters");
    }

    [Test]
    public void IgniteMainThrottleCommand_ValidateParameters_ShouldRejectNullParameters()
    {
        // Arrange
        var command = CreateIgniteMainThrottleCommand();

        // Act
        var isValid = InvokeValidateParameters(command, null!);

        // Assert
        Assert.That(isValid, Is.False, "Command should reject null parameters");
    }

    #endregion

    #region ShutdownMainEngine Command Tests

    [Test]
    public void ShutdownMainEngineCommand_ShouldBeFireAndForget()
    {
        // Arrange & Act
        var command = CreateShutdownMainEngineCommand();

        // Assert
        Assert.That(command.IsFireAndForget, Is.True, "ShutdownMainEngine should be a fire-and-forget command");
        Assert.That(command.Timeout, Is.EqualTo(TimeSpan.Zero), "Fire-and-forget commands should have zero timeout");
    }

    [Test]
    public void ShutdownMainEngineCommand_ShouldHaveCorrectDescription()
    {
        // Arrange & Act
        var command = CreateShutdownMainEngineCommand();

        // Assert
        Assert.That(command.Description, Is.EqualTo("Shutdown Main Engine"), "Command should have correct description");
    }

    [Test]
    public async Task ShutdownMainEngineCommand_ExecuteAsync_ShouldReturnNull()
    {
        // Arrange
        var command = CreateShutdownMainEngineCommand();

        // Act
        var result = await command.ExecuteAsync(_emptyParameters);

        // Assert
        Assert.That(result, Is.Null, "Fire-and-forget commands should return null");
    }

    [Test]
    public async Task ShutdownMainEngineCommand_ExecuteAsync_ShouldAcceptEmptyParameters()
    {
        // Arrange
        var command = CreateShutdownMainEngineCommand();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await command.ExecuteAsync(_emptyParameters),
            "Command should accept empty parameters");
    }

    [Test]
    public void ShutdownMainEngineCommand_ValidateParameters_ShouldAcceptEmptyParameters()
    {
        // Arrange
        var command = CreateShutdownMainEngineCommand();

        // Act
        var isValid = InvokeValidateParameters(command, _emptyParameters);

        // Assert
        Assert.That(isValid, Is.True, "Command should accept empty parameters");
    }

    [Test]
    public void ShutdownMainEngineCommand_ValidateParameters_ShouldRejectNullParameters()
    {
        // Arrange
        var command = CreateShutdownMainEngineCommand();

        // Act
        var isValid = InvokeValidateParameters(command, null!);

        // Assert
        Assert.That(isValid, Is.False, "Command should reject null parameters");
    }

    #endregion

    #region GetThrottleStatus Query Tests

    [Test]
    public void GetThrottleStatusQuery_ShouldBeQueryCommand()
    {
        // Arrange & Act
        var command = CreateGetThrottleStatusQuery();

        // Assert
        Assert.That(command.IsFireAndForget, Is.False, "GetThrottleStatus should be a query command");
        Assert.That(command.Timeout, Is.EqualTo(TimeSpan.FromSeconds(3)), "Query should have 3-second timeout");
    }

    [Test]
    public void GetThrottleStatusQuery_ShouldHaveCorrectDescription()
    {
        // Arrange & Act
        var command = CreateGetThrottleStatusQuery();

        // Assert
        Assert.That(command.Description, Is.EqualTo("Get Throttle Status"), "Command should have correct description");
    }

    [Test]
    public async Task GetThrottleStatusQuery_ExecuteAsync_ShouldReturnThrottleData()
    {
        // Arrange
        var command = CreateGetThrottleStatusQuery();

        // Act
        var result = await command.ExecuteAsync(_emptyParameters);

        // Assert
        Assert.That(result, Is.Not.Null, "Query should return data");

        // Verify the structure of the returned data
        var resultDict = result as Dictionary<string, object?>;
        Assert.That(resultDict, Is.Not.Null, "Result should be a dictionary");
        Assert.That(resultDict!.ContainsKey("status"), Is.True, "Result should contain status");
        Assert.That(resultDict.ContainsKey("value"), Is.True, "Result should contain value");
        Assert.That(resultDict.ContainsKey("data"), Is.True, "Result should contain additional data");

        // Verify the mock data values
        Assert.That(resultDict["status"], Is.EqualTo("enabled"), "Status should be 'enabled'");
        Assert.That(resultDict["value"], Is.EqualTo(75), "Value should be 75 (mock throttle level)");

        // Verify the additional data structure
        var additionalData = resultDict["data"];
        Assert.That(additionalData, Is.Not.Null, "Additional data should not be null");
    }

    [Test]
    public async Task GetThrottleStatusQuery_ExecuteAsync_ShouldAcceptEmptyParameters()
    {
        // Arrange
        var command = CreateGetThrottleStatusQuery();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await command.ExecuteAsync(_emptyParameters),
            "Query should accept empty parameters");
    }

    [Test]
    public void GetThrottleStatusQuery_ValidateParameters_ShouldAcceptEmptyParameters()
    {
        // Arrange
        var command = CreateGetThrottleStatusQuery();

        // Act
        var isValid = InvokeValidateParameters(command, _emptyParameters);

        // Assert
        Assert.That(isValid, Is.True, "Query should accept empty parameters");
    }

    [Test]
    public void GetThrottleStatusQuery_ValidateParameters_ShouldRejectNullParameters()
    {
        // Arrange
        var command = CreateGetThrottleStatusQuery();

        // Act
        var isValid = InvokeValidateParameters(command, null!);

        // Assert
        Assert.That(isValid, Is.False, "Query should reject null parameters");
    }

    [Test]
    public async Task GetThrottleStatusQuery_ExecuteAsync_ShouldReturnConsistentMockData()
    {
        // Arrange
        var command = CreateGetThrottleStatusQuery();

        // Act - Execute multiple times
        var result1 = await command.ExecuteAsync(_emptyParameters);
        var result2 = await command.ExecuteAsync(_emptyParameters);

        // Assert - Results should be consistent (same mock data)
        Assert.That(result1, Is.Not.Null);
        Assert.That(result2, Is.Not.Null);

        var dict1 = result1 as Dictionary<string, object?>;
        var dict2 = result2 as Dictionary<string, object?>;

        Assert.That(dict1!["status"], Is.EqualTo(dict2!["status"]), "Status should be consistent");
        Assert.That(dict1["value"], Is.EqualTo(dict2["value"]), "Value should be consistent");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates an instance of IgniteMainThrottleCommand using reflection.
    /// </summary>
    private static IRpcCommandHandler CreateIgniteMainThrottleCommand()
    {
        var type = Assembly.GetAssembly(typeof(GameActionRegistry))!
            .GetTypes()
            .First(t => t.Name == "IgniteMainThrottleCommand");

        return (IRpcCommandHandler)Activator.CreateInstance(type)!;
    }

    /// <summary>
    /// Creates an instance of ShutdownMainEngineCommand using reflection.
    /// </summary>
    private static IRpcCommandHandler CreateShutdownMainEngineCommand()
    {
        var type = Assembly.GetAssembly(typeof(GameActionRegistry))!
            .GetTypes()
            .First(t => t.Name == "ShutdownMainEngineCommand");

        return (IRpcCommandHandler)Activator.CreateInstance(type)!;
    }

    /// <summary>
    /// Creates an instance of GetThrottleStatusQuery using reflection.
    /// </summary>
    private static IRpcCommandHandler CreateGetThrottleStatusQuery()
    {
        var type = Assembly.GetAssembly(typeof(GameActionRegistry))!
            .GetTypes()
            .First(t => t.Name == "GetThrottleStatusQuery");

        return (IRpcCommandHandler)Activator.CreateInstance(type)!;
    }

    /// <summary>
    /// Invokes the ValidateParameters method using reflection.
    /// </summary>
    private static bool InvokeValidateParameters(IRpcCommandHandler handler, RpcParameters parameters)
    {
        var method = handler.GetType().GetMethod("ValidateParameters",
            BindingFlags.NonPublic | BindingFlags.Instance);

        return (bool)method!.Invoke(handler, new object[] { parameters })!;
    }

    #endregion
}
