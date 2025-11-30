using System.Diagnostics;
using System.Text;
using System.Collections.Concurrent;

namespace dotnet_exe_link_libghostty.Terminal;

/// <summary>
/// Manages a child process (shell) with redirected stdin/stdout.
/// Uses async background threads to read output without blocking.
/// </summary>
public class ProcessManager : IDisposable
{
    private Process? _process;
    private readonly ConcurrentQueue<byte> _outputQueue = new();
    private readonly ConcurrentQueue<byte> _errorQueue = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;
    
    /// <summary>
    /// Event fired when the process exits.
    /// </summary>
    public event EventHandler? ProcessExited;
    
    /// <summary>
    /// Returns true if the process is running.
    /// </summary>
    public bool IsRunning => _process != null && !_process.HasExited;
    
    /// <summary>
    /// Starts a shell process with redirected I/O.
    /// </summary>
    public void Start()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProcessManager));
        
        if (_process != null)
            throw new InvalidOperationException("Process already started");
        
        // Determine shell based on OS
        string shell;
        string shellArgs;
        
        if (OperatingSystem.IsWindows())
        {
            shell = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe";
            shellArgs = "";
        }
        else
        {
            // Unix-like systems
            shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";
            shellArgs = "-i"; // Interactive mode to get prompt
        }
        
        var startInfo = new ProcessStartInfo
        {
            FileName = shell,
            Arguments = shellArgs,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        
        // Set environment variables
        startInfo.Environment["TERM"] = "xterm-256color";
        startInfo.Environment["COLUMNS"] = "80";
        startInfo.Environment["LINES"] = "24";
        // Disable some shell features that might interfere
        startInfo.Environment["PS1"] = "$ "; // Simple prompt
        
        _process = new Process { StartInfo = startInfo };
        _process.EnableRaisingEvents = true;
        _process.Exited += OnProcessExited;
        
        _process.Start();
        
        // Start background threads to read output
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => ReadOutputLoop(_cancellationTokenSource.Token));
        Task.Run(() => ReadErrorLoop(_cancellationTokenSource.Token));
    }
    
    private async Task ReadOutputLoop(CancellationToken cancellationToken)
    {
        if (_process == null)
            return;
        
        try
        {
            var stream = _process.StandardOutput.BaseStream;
            var buffer = new byte[4096];
            
            while (!cancellationToken.IsCancellationRequested && !_process.HasExited)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        _outputQueue.Enqueue(buffer[i]);
                    }
                }
            }
        }
        catch (Exception)
        {
            // Silently handle read errors
        }
    }
    
    private async Task ReadErrorLoop(CancellationToken cancellationToken)
    {
        if (_process == null)
            return;
        
        try
        {
            var stream = _process.StandardError.BaseStream;
            var buffer = new byte[4096];
            
            while (!cancellationToken.IsCancellationRequested && !_process.HasExited)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        _errorQueue.Enqueue(buffer[i]);
                    }
                }
            }
        }
        catch (Exception)
        {
            // Silently handle read errors
        }
    }
    
    /// <summary>
    /// Sends encoded key data to the process stdin.
    /// </summary>
    public void SendInput(byte[] data)
    {
        if (_disposed || _process == null || data == null || data.Length == 0)
            return;
        
        try
        {
            _process.StandardInput.BaseStream.Write(data, 0, data.Length);
            _process.StandardInput.BaseStream.Flush();
        }
        catch (Exception)
        {
            // Silently ignore write errors (process may have exited)
        }
    }
    
    /// <summary>
    /// Reads available output from the process stdout.
    /// Returns null if no data is available.
    /// </summary>
    public byte[]? ReadOutput()
    {
        if (_outputQueue.IsEmpty)
            return null;
        
        var result = new List<byte>();
        while (_outputQueue.TryDequeue(out byte b) && result.Count < 4096)
        {
            result.Add(b);
        }
        
        return result.Count > 0 ? result.ToArray() : null;
    }
    
    /// <summary>
    /// Reads available error output from the process stderr.
    /// Returns null if no data is available.
    /// </summary>
    public byte[]? ReadError()
    {
        if (_errorQueue.IsEmpty)
            return null;
        
        var result = new List<byte>();
        while (_errorQueue.TryDequeue(out byte b) && result.Count < 4096)
        {
            result.Add(b);
        }
        
        return result.Count > 0 ? result.ToArray() : null;
    }
    
    private void OnProcessExited(object? sender, EventArgs e)
    {
        ProcessExited?.Invoke(this, EventArgs.Empty);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
            
            _cancellationTokenSource?.Dispose();
            _process?.Dispose();
            _disposed = true;
        }
    }
}
