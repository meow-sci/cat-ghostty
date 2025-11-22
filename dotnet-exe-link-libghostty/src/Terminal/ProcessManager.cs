using System.Diagnostics;
using System.Text;

namespace dotnet_exe_link_libghostty.Terminal;

/// <summary>
/// Manages a child process (shell) with redirected stdin/stdout.
/// For MVP, uses basic Process class without full PTY support.
/// </summary>
public class ProcessManager : IDisposable
{
    private Process? _process;
    private StreamWriter? _processInput;
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
            shellArgs = "";
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
        
        _process = new Process { StartInfo = startInfo };
        _process.EnableRaisingEvents = true;
        _process.Exited += OnProcessExited;
        
        _process.Start();
        _processInput = _process.StandardInput;
    }
    
    /// <summary>
    /// Sends encoded key data to the process stdin.
    /// </summary>
    public void SendInput(byte[] data)
    {
        if (_disposed || _processInput == null || data == null || data.Length == 0)
            return;
        
        try
        {
            // Convert bytes to string (assuming UTF-8 or ASCII)
            string text = Encoding.UTF8.GetString(data);
            _processInput.Write(text);
            _processInput.Flush();
        }
        catch (Exception)
        {
            // Silently ignore write errors (process may have exited)
        }
    }
    
    /// <summary>
    /// Reads available output from the process stdout.
    /// Returns null if no data is available or process has exited.
    /// </summary>
    public byte[]? ReadOutput(int maxBytes = 4096)
    {
        if (_disposed || _process == null || _process.HasExited)
            return null;
        
        try
        {
            var stdout = _process.StandardOutput;
            if (stdout.Peek() == -1)
                return null;
            
            var buffer = new char[maxBytes];
            int read = stdout.Read(buffer, 0, maxBytes);
            
            if (read > 0)
            {
                return Encoding.UTF8.GetBytes(buffer, 0, read);
            }
        }
        catch (Exception)
        {
            // Silently ignore read errors
        }
        
        return null;
    }
    
    /// <summary>
    /// Reads available error output from the process stderr.
    /// Returns null if no data is available.
    /// </summary>
    public byte[]? ReadError(int maxBytes = 4096)
    {
        if (_disposed || _process == null || _process.HasExited)
            return null;
        
        try
        {
            var stderr = _process.StandardError;
            if (stderr.Peek() == -1)
                return null;
            
            var buffer = new char[maxBytes];
            int read = stderr.Read(buffer, 0, maxBytes);
            
            if (read > 0)
            {
                return Encoding.UTF8.GetBytes(buffer, 0, read);
            }
        }
        catch (Exception)
        {
            // Silently ignore read errors
        }
        
        return null;
    }
    
    /// <summary>
    /// Asynchronously reads a line from stdout.
    /// Used for testing or simple scenarios.
    /// </summary>
    public async Task<byte[]?> ReadOutputAsync()
    {
        if (_disposed || _process == null || _process.HasExited)
            return null;
        
        try
        {
            var stdout = _process.StandardOutput;
            var line = await stdout.ReadLineAsync();
            
            if (line != null)
            {
                return Encoding.UTF8.GetBytes(line + "\n");
            }
        }
        catch (Exception)
        {
            // Silently ignore read errors
        }
        
        return null;
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
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
            
            _processInput?.Dispose();
            _process?.Dispose();
            _disposed = true;
        }
    }
}
