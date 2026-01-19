using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using caTTY.Core.Rpc.Socket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace caTTY.Core.Tests.Unit.Rpc.Socket;

[TestFixture]
[Category("Unit")]
public class SocketRpcServerTests : IDisposable
{
    private string _socketPath = null!;
    private readonly ILogger _logger = NullLogger.Instance;

    [SetUp]
    public void SetUp()
    {
        _socketPath = Path.Combine(Path.GetTempPath(), $"catty-test-{Guid.NewGuid():N}.sock");
    }

    [TearDown]
    public void Dispose()
    {
        if (File.Exists(_socketPath))
        {
            try { File.Delete(_socketPath); } catch { /* ignore cleanup errors */ }
        }
    }

    [Test]
    public async Task StartAsync_CreatesSocketFile()
    {
        // Arrange
        var handler = new TestHandler(req => SocketRpcResponse.Ok());
        using var server = new SocketRpcServer(_socketPath, handler, _logger);

        // Act
        await server.StartAsync();

        // Assert
        Assert.That(server.IsRunning, Is.True);
        Assert.That(File.Exists(_socketPath), Is.True);

        await server.StopAsync();
    }

    [Test]
    public async Task StopAsync_RemovesSocketFile()
    {
        // Arrange
        var handler = new TestHandler(req => SocketRpcResponse.Ok());
        using var server = new SocketRpcServer(_socketPath, handler, _logger);
        await server.StartAsync();

        // Act
        await server.StopAsync();

        // Assert
        Assert.That(server.IsRunning, Is.False);
        Assert.That(File.Exists(_socketPath), Is.False);
    }

    [Test]
    public async Task HandleRequest_ReturnsHandlerResponse()
    {
        // Arrange
        var expectedData = new { crafts = new[] { "Rocket-1", "Rocket-2" } };
        var handler = new TestHandler(req =>
        {
            Assert.That(req.Action, Is.EqualTo("list-crafts"));
            return SocketRpcResponse.Ok(expectedData);
        });

        using var server = new SocketRpcServer(_socketPath, handler, _logger);
        await server.StartAsync();

        // Act
        var response = await SendRequestAsync(_socketPath, new SocketRpcRequest { Action = "list-crafts" });

        // Assert
        Assert.That(response.Success, Is.True);
        Assert.That(response.Data, Is.Not.Null);

        await server.StopAsync();
    }

    [Test]
    public async Task HandleRequest_InvalidJson_ReturnsError()
    {
        // Arrange
        var handler = new TestHandler(req => SocketRpcResponse.Ok());
        using var server = new SocketRpcServer(_socketPath, handler, _logger);
        await server.StartAsync();

        // Act
        var response = await SendRawRequestAsync(_socketPath, "not valid json\n");

        // Assert
        Assert.That(response.Success, Is.False);
        Assert.That(response.Error, Does.Contain("Invalid JSON"));

        await server.StopAsync();
    }

    [Test]
    public async Task HandleRequest_MissingAction_ReturnsError()
    {
        // Arrange
        var handler = new TestHandler(req => SocketRpcResponse.Ok());
        using var server = new SocketRpcServer(_socketPath, handler, _logger);
        await server.StartAsync();

        // Act
        var response = await SendRawRequestAsync(_socketPath, "{}\n");

        // Assert
        Assert.That(response.Success, Is.False);
        Assert.That(response.Error, Does.Contain("missing action"));

        await server.StopAsync();
    }

    [Test]
    public async Task HandleRequest_HandlerThrows_ReturnsError()
    {
        // Arrange
        var handler = new TestHandler(req => throw new InvalidOperationException("Game not loaded"));
        using var server = new SocketRpcServer(_socketPath, handler, _logger);
        await server.StartAsync();

        // Act
        var response = await SendRequestAsync(_socketPath, new SocketRpcRequest { Action = "test" });

        // Assert
        Assert.That(response.Success, Is.False);
        Assert.That(response.Error, Does.Contain("Game not loaded"));

        await server.StopAsync();
    }

    [Test]
    public async Task HandleRequest_WithParams_PassesParamsToHandler()
    {
        // Arrange
        JsonElement? receivedParams = null;
        var handler = new TestHandler(req =>
        {
            receivedParams = req.Params;
            return SocketRpcResponse.Ok();
        });

        using var server = new SocketRpcServer(_socketPath, handler, _logger);
        await server.StartAsync();

        // Act
        var request = "{\"action\":\"test\",\"params\":{\"craftId\":42}}\n";
        await SendRawRequestAsync(_socketPath, request);

        // Assert
        Assert.That(receivedParams, Is.Not.Null);
        Assert.That(receivedParams!.Value.GetProperty("craftId").GetInt32(), Is.EqualTo(42));

        await server.StopAsync();
    }

    [Test]
    public void StartAsync_AlreadyRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new TestHandler(req => SocketRpcResponse.Ok());
        using var server = new SocketRpcServer(_socketPath, handler, _logger);
        server.StartAsync().Wait();

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await server.StartAsync());

        server.StopAsync().Wait();
    }

    [Test]
    public void Dispose_RemovesSocketFile()
    {
        // Arrange
        var handler = new TestHandler(req => SocketRpcResponse.Ok());
        var server = new SocketRpcServer(_socketPath, handler, _logger);
        server.StartAsync().Wait();

        // Act
        server.Dispose();

        // Assert
        Assert.That(File.Exists(_socketPath), Is.False);
    }

    private static async Task<SocketRpcResponse> SendRequestAsync(string socketPath, SocketRpcRequest request)
    {
        var json = JsonSerializer.Serialize(request) + "\n";
        return await SendRawRequestAsync(socketPath, json);
    }

    private static async Task<SocketRpcResponse> SendRawRequestAsync(string socketPath, string rawRequest)
    {
        using var socket = new System.Net.Sockets.Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath));

        using var stream = new NetworkStream(socket);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        await writer.WriteAsync(rawRequest);
        var responseLine = await reader.ReadLineAsync();

        return JsonSerializer.Deserialize<SocketRpcResponse>(responseLine!)
               ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    private class TestHandler : ISocketRpcHandler
    {
        private readonly Func<SocketRpcRequest, SocketRpcResponse> _handleFunc;

        public TestHandler(Func<SocketRpcRequest, SocketRpcResponse> handleFunc)
        {
            _handleFunc = handleFunc;
        }

        public SocketRpcResponse HandleRequest(SocketRpcRequest request) => _handleFunc(request);
    }
}
