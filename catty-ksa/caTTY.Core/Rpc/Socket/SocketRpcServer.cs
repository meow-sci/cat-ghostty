using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace caTTY.Core.Rpc.Socket;

/// <summary>
/// Unix Domain Socket RPC server implementation.
/// Accepts connections, reads JSON requests, dispatches to handler, sends JSON responses.
/// Uses single-threaded accept loop with synchronous request handling.
/// </summary>
public sealed class SocketRpcServer : ISocketRpcServer
{
    private readonly ISocketRpcHandler _handler;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private System.Net.Sockets.Socket? _listenSocket;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private bool _disposed;

    /// <inheritdoc />
    public string SocketPath { get; }

    /// <inheritdoc />
    public bool IsRunning => _listenSocket != null && _acceptTask != null && !_acceptTask.IsCompleted;

    /// <summary>
    /// Creates a new SocketRpcServer.
    /// </summary>
    /// <param name="socketPath">Path for the Unix domain socket file</param>
    /// <param name="handler">Handler to dispatch requests to</param>
    /// <param name="logger">Logger for diagnostics</param>
    public SocketRpcServer(string socketPath, ISocketRpcHandler handler, ILogger logger)
    {
        SocketPath = socketPath ?? throw new ArgumentNullException(nameof(socketPath));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SocketRpcServer));
        if (IsRunning) throw new InvalidOperationException("Server is already running");

        // Delete existing socket file if present
        if (File.Exists(SocketPath))
        {
            File.Delete(SocketPath);
        }

        // Ensure directory exists
        var dir = Path.GetDirectoryName(SocketPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _listenSocket = new System.Net.Sockets.Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _listenSocket.Bind(new UnixDomainSocketEndPoint(SocketPath));
        _listenSocket.Listen(1); // Only 1 connection at a time

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _acceptTask = AcceptLoopAsync(_cts.Token);

        _logger.LogInformation("Socket RPC server started on {SocketPath}", SocketPath);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (_disposed) return;

        _cts?.Cancel();

        try
        {
            _listenSocket?.Close();
        }
        catch { /* ignore */ }

        if (_acceptTask != null)
        {
            try
            {
                await _acceptTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* expected */ }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Accept loop ended with exception");
            }
        }

        CleanupSocketFile();
        _logger.LogInformation("Socket RPC server stopped");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();

        try { _listenSocket?.Dispose(); } catch { /* ignore */ }

        CleanupSocketFile();
    }

    private void CleanupSocketFile()
    {
        try
        {
            if (File.Exists(SocketPath))
            {
                File.Delete(SocketPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to delete socket file {SocketPath}", SocketPath);
        }
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var client = await _listenSocket!.AcceptAsync(ct).ConfigureAwait(false);
                await HandleClientAsync(client, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
            {
                break; // Socket was closed
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error accepting client connection");
            }
        }
    }

    private async Task HandleClientAsync(System.Net.Sockets.Socket client, CancellationToken ct)
    {
        try
        {
            using var stream = new NetworkStream(client, ownsSocket: false);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            // Read single line JSON request
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            _logger.LogDebug("Socket RPC received: {Request}", line);

            SocketRpcResponse response;
            try
            {
                var request = JsonSerializer.Deserialize<SocketRpcRequest>(line, _jsonOptions);
                if (request == null || string.IsNullOrEmpty(request.Action))
                {
                    response = SocketRpcResponse.Fail("Invalid request: missing action");
                }
                else
                {
                    response = _handler.HandleRequest(request);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse RPC request");
                response = SocketRpcResponse.Fail($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling RPC request");
                response = SocketRpcResponse.Fail($"Internal error: {ex.Message}");
            }

            var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
            await writer.WriteLineAsync(responseJson).ConfigureAwait(false);

            _logger.LogDebug("Socket RPC response: {Response}", responseJson);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error handling client");
        }
    }
}
