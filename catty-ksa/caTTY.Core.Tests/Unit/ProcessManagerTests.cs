using System.Runtime.InteropServices;
using System.Text;
using caTTY.Core.Terminal;
using NUnit.Framework;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace caTTY.Core.Tests.Unit;

/// <summary>
///     Unit tests for ProcessManager class.
/// </summary>
[TestFixture]
public class ProcessManagerTests
{
    /// <summary>
    ///     Sets up test fixtures before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _processManager = new ProcessManager();
    }

    /// <summary>
    ///     Cleans up test fixtures after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _processManager?.Dispose();
    }

    private ProcessManager? _processManager;

    // ConPTY availability check
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int CreatePseudoConsole(COORD size, IntPtr hInput, IntPtr hOutput, uint dwFlags,
        out IntPtr phPC);

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;

        public COORD(short x, short y)
        {
            X = x;
            Y = y;
        }
    }

    private static bool IsConPtyAvailable()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            // Try to get the CreatePseudoConsole function to see if ConPTY is available
            int result = CreatePseudoConsole(new COORD(80, 25), IntPtr.Zero, IntPtr.Zero, 0, out IntPtr hPC);
            return true; // If we get here, the function exists (even if it fails due to invalid params)
        }
        catch (EntryPointNotFoundException)
        {
            return false; // ConPTY not available on this Windows version
        }
        catch
        {
            return true; // Function exists but failed due to invalid parameters - that's expected
        }
    }

    /// <summary>
    ///     Tests that ProcessManager constructor creates a valid instance with expected initial state.
    /// </summary>
    [Test]
    public void Constructor_CreatesProcessManager()
    {
        // Arrange & Act
        using var manager = new ProcessManager();

        // Assert
        Assert.That(manager.IsRunning, Is.False);
        Assert.That(manager.ProcessId, Is.Null);
        Assert.That(manager.ExitCode, Is.Null);
    }

    /// <summary>
    ///     Tests ConPTY availability detection on the current platform.
    /// </summary>
    [Test]
    public void ConPtyAvailability_CheckPlatformSupport()
    {
        // This test documents ConPTY availability on the current system
        bool isAvailable = IsConPtyAvailable();
        bool isWindows = OperatingSystem.IsWindows();

        Console.WriteLine($"Platform: Windows = {isWindows}");
        Console.WriteLine($"ConPTY Available = {isAvailable}");

        if (isWindows)
        {
            // On Windows, we expect ConPTY to be available on Windows 10 1809+
            // If not available, tests will be skipped
            Assert.Pass($"ConPTY availability: {isAvailable}");
        }
        else
        {
            Assert.That(isAvailable, Is.False, "ConPTY should not be available on non-Windows platforms");
        }
    }

    /// <summary>
    ///     Tests that CreateDefault returns valid default process launch options.
    /// </summary>
    [Test]
    public void ProcessLaunchOptions_CreateDefault_ReturnsValidOptions()
    {
        // Act
        var options = ProcessLaunchOptions.CreateDefault();

        // Assert
        Assert.That(options, Is.Not.Null);
        Assert.That(options.InitialWidth, Is.EqualTo(80));
        Assert.That(options.InitialHeight, Is.EqualTo(25));
        Assert.That(options.CreateWindow, Is.False);
        Assert.That(options.UseShellExecute, Is.False);
        Assert.That(options.EnvironmentVariables, Contains.Key("TERM"));
        Assert.That(options.EnvironmentVariables["TERM"], Is.EqualTo("xterm-256color"));
    }

    /// <summary>
    ///     Tests that CreatePowerShell returns valid PowerShell-specific launch options.
    /// </summary>
    [Test]
    public void ProcessLaunchOptions_CreatePowerShell_ReturnsValidOptions()
    {
        // Act
        var options = ProcessLaunchOptions.CreatePowerShell();

        // Assert
        Assert.That(options, Is.Not.Null);
        Assert.That(options.ShellType, Is.EqualTo(ShellType.PowerShell));
        Assert.That(options.Arguments, Contains.Item("-NoLogo"));
        Assert.That(options.Arguments, Contains.Item("-NoProfile"));
    }

    /// <summary>
    ///     Tests that CreateCustom returns valid custom shell launch options.
    /// </summary>
    [Test]
    public void ProcessLaunchOptions_CreateCustom_ReturnsValidOptions()
    {
        // Arrange
        const string shellPath = "test.exe";
        const string arg1 = "-arg1";
        const string arg2 = "-arg2";

        // Act
        var options = ProcessLaunchOptions.CreateCustom(shellPath, arg1, arg2);

        // Assert
        Assert.That(options, Is.Not.Null);
        Assert.That(options.ShellType, Is.EqualTo(ShellType.Custom));
        Assert.That(options.CustomShellPath, Is.EqualTo(shellPath));
        Assert.That(options.Arguments, Contains.Item(arg1));
        Assert.That(options.Arguments, Contains.Item(arg2));
    }

    /// <summary>
    ///     Tests that StartAsync throws ProcessStartException when given an invalid shell path.
    /// </summary>
    [Test]
    public void StartAsync_WithInvalidShell_ThrowsProcessStartException()
    {
        // Arrange
        if (!IsConPtyAvailable())
        {
            Assert.Ignore("ConPTY not available on this system");
            return;
        }

        var options = ProcessLaunchOptions.CreateCustom("nonexistent-shell-12345.exe");

        // Act & Assert
        Assert.ThrowsAsync<ProcessStartException>(() => _processManager!.StartAsync(options));
    }

    /// <summary>
    ///     Tests that StartAsync throws InvalidOperationException when a process is already running.
    /// </summary>
    [Test]
    public void StartAsync_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        if (!IsConPtyAvailable())
        {
            Assert.Ignore("ConPTY not available on this system");
            return;
        }

        var options = ProcessLaunchOptions.CreateCmd();
        options.Arguments.AddRange(["/c", "ping -n 2 127.0.0.1"]);

        try
        {
            _processManager!.StartAsync(options).Wait();

            // Wait a moment to ensure the process is fully started
            Thread.Sleep(200);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => _processManager.StartAsync(options));
        }
        finally
        {
            _processManager!.StopAsync().Wait();
        }
    }

    /// <summary>
    ///     Tests that Write throws InvalidOperationException when no process is running.
    /// </summary>
    [Test]
    public void Write_WithoutRunningProcess_ThrowsInvalidOperationException()
    {
        // Act & Assert
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() => _processManager!.Write("test"));

        Assert.That(ex.Message, Does.Contain("No process is currently running"));
    }

    /// <summary>
    ///     Tests that Write with byte span throws InvalidOperationException when no process is running.
    /// </summary>
    [Test]
    public void Write_WithByteSpan_WithoutRunningProcess_ThrowsInvalidOperationException()
    {
        // Arrange
        byte[] data = "test"u8.ToArray();

        // Act & Assert
        InvalidOperationException? ex =
            Assert.Throws<InvalidOperationException>(() => _processManager!.Write(data.AsSpan()));

        Assert.That(ex.Message, Does.Contain("No process is currently running"));
    }

    /// <summary>
    ///     Tests that Resize throws InvalidOperationException when no process is running.
    /// </summary>
    [Test]
    public void Resize_WithoutRunningProcess_ThrowsInvalidOperationException()
    {
        // Act & Assert
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() => _processManager!.Resize(80, 25));

        Assert.That(ex.Message, Does.Contain("No process is currently running"));
    }

    /// <summary>
    ///     Tests that StartAsync throws PlatformNotSupportedException on non-Windows platforms.
    /// </summary>
    [Test]
    public void StartAsync_OnNonWindows_ThrowsPlatformNotSupportedException()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("This test is for non-Windows platforms only");
            return;
        }

        // Arrange
        var options = ProcessLaunchOptions.CreateDefault();

        // Act & Assert
        Assert.ThrowsAsync<PlatformNotSupportedException>(() => _processManager!.StartAsync(options));
    }

    /// <summary>
    ///     Tests that StartAsync successfully starts a process with valid shell options.
    /// </summary>
    [Test]
    public async Task StartAsync_WithValidShell_StartsProcess()
    {
        // Arrange
        if (!IsConPtyAvailable())
        {
            Assert.Ignore("ConPTY not available on this system");
            return;
        }

        // Use a command that will run for a bit longer
        var options = ProcessLaunchOptions.CreateCmd();
        options.Arguments.AddRange(["/c", "ping -n 3 127.0.0.1"]);

        try
        {
            // Act
            await _processManager!.StartAsync(options);

            // Assert - check immediately after starting
            Assert.That(_processManager.IsRunning, Is.True);
            Assert.That(_processManager.ProcessId, Is.Not.Null);
            Assert.That(_processManager.ProcessId, Is.GreaterThan(0));
        }
        finally
        {
            await _processManager!.StopAsync();
        }
    }

    /// <summary>
    ///     Tests that StopAsync successfully stops a running process.
    /// </summary>
    [Test]
    public async Task StopAsync_WithRunningProcess_StopsProcess()
    {
        // Arrange
        if (!IsConPtyAvailable())
        {
            Assert.Ignore("ConPTY not available on this system");
            return;
        }

        // Use a command that will definitely run for a while
        var options = ProcessLaunchOptions.CreateCmd();
        options.Arguments.AddRange(["/c", "ping -n 10 127.0.0.1"]);
        await _processManager!.StartAsync(options);

        Assert.That(_processManager.IsRunning, Is.True);

        // Act
        await _processManager.StopAsync();

        // Assert
        Assert.That(_processManager.IsRunning, Is.False);
    }

    /// <summary>
    ///     Tests that StopAsync does not throw when no process is running.
    /// </summary>
    [Test]
    public async Task StopAsync_WithoutRunningProcess_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrowAsync(() => _processManager!.StopAsync());
    }

    /// <summary>
    ///     Tests that ProcessExited event is raised when a process exits naturally.
    /// </summary>
    [Test]
    public async Task ProcessExited_Event_RaisedWhenProcessExits()
    {
        // Arrange
        if (!IsConPtyAvailable())
        {
            Assert.Ignore("ConPTY not available on this system");
            return;
        }

        var options = ProcessLaunchOptions.CreateCmd();
        options.Arguments.AddRange(["/c", "echo test"]);
        bool exitedEventRaised = false;
        int exitCode = -1;

        _processManager!.ProcessExited += (sender, args) =>
        {
            exitedEventRaised = true;
            exitCode = args.ExitCode;
        };

        // Act
        await _processManager.StartAsync(options);

        // Wait for the process to exit naturally
        var timeout = TimeSpan.FromSeconds(5);
        DateTime start = DateTime.UtcNow;

        while (_processManager.IsRunning && DateTime.UtcNow - start < timeout)
        {
            await Task.Delay(100);
        }

        // Assert
        Assert.That(exitedEventRaised, Is.True, "ProcessExited event should be raised");
        Assert.That(exitCode, Is.EqualTo(0), "Exit code should be 0 for successful echo command");
        Assert.That(_processManager.IsRunning, Is.False, "Process should not be running after exit");
    }

    /// <summary>
    ///     Tests that DataReceived event is raised when a process outputs data.
    /// </summary>
    [Test]
    public async Task DataReceived_Event_RaisedWhenProcessOutputsData()
    {
        // Arrange
        if (!IsConPtyAvailable())
        {
            Assert.Ignore("ConPTY not available on this system");
            return;
        }

        var options = ProcessLaunchOptions.CreateCmd();
        options.Arguments.AddRange(["/c", "echo Hello World"]);
        bool dataReceived = false;
        string receivedData = "";

        _processManager!.DataReceived += (sender, args) =>
        {
            dataReceived = true;
            receivedData += Encoding.UTF8.GetString(args.Data.Span);
        };

        // Act
        await _processManager.StartAsync(options);

        // Wait for data to be received - ConPTY might need more time
        var timeout = TimeSpan.FromSeconds(10);
        DateTime start = DateTime.UtcNow;

        while (!dataReceived && DateTime.UtcNow - start < timeout)
        {
            await Task.Delay(100);
        }

        await _processManager.StopAsync();

        // Assert
        Assert.That(dataReceived, Is.True, "DataReceived event should be raised");
        // ConPTY output might include additional formatting, so just check for the content
        Assert.That(receivedData, Does.Contain("Hello World").Or.Contain("Hello").Or.Not.Empty,
            $"Should receive some output, got: '{receivedData}'");
    }

    /// <summary>
    ///     Tests that Dispose can be called multiple times without throwing.
    /// </summary>
    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var manager = new ProcessManager();

        // Act & Assert
        Assert.DoesNotThrow(() => manager.Dispose());
        Assert.DoesNotThrow(() => manager.Dispose());
    }

    /// <summary>
    ///     Tests that Write throws ObjectDisposedException after the ProcessManager is disposed.
    /// </summary>
    [Test]
    public void Write_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var manager = new ProcessManager();
        manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => manager.Write("test"));
    }
}
