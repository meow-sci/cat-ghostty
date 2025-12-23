using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using caTTY.Core.Terminal;

namespace caTTY.Core.Tests.Unit;

/// <summary>
/// Unit tests for ProcessManager class.
/// </summary>
[TestFixture]
public class ProcessManagerTests
{
    private ProcessManager? _processManager;

    [SetUp]
    public void SetUp()
    {
        _processManager = new ProcessManager();
    }

    [TearDown]
    public void TearDown()
    {
        _processManager?.Dispose();
    }

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

    [Test]
    public void StartAsync_WithInvalidShell_ThrowsProcessStartException()
    {
        // Arrange
        var options = ProcessLaunchOptions.CreateCustom("nonexistent-shell-12345.exe");

        // Act & Assert
        Assert.ThrowsAsync<ProcessStartException>(
            () => _processManager!.StartAsync(options));
    }

    [Test]
    public void StartAsync_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = ProcessLaunchOptions.CreateCmd();
        
        try
        {
            _processManager!.StartAsync(options).Wait();
            
            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _processManager.StartAsync(options));
        }
        finally
        {
            _processManager!.StopAsync().Wait();
        }
    }

    [Test]
    public void Write_WithoutRunningProcess_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => _processManager!.Write("test"));
        
        Assert.That(ex.Message, Does.Contain("No process is currently running"));
    }

    [Test]
    public void Write_WithByteSpan_WithoutRunningProcess_ThrowsInvalidOperationException()
    {
        // Arrange
        var data = "test"u8.ToArray();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => _processManager!.Write(data.AsSpan()));
        
        Assert.That(ex.Message, Does.Contain("No process is currently running"));
    }

    [Test]
    public void Resize_WithoutRunningProcess_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => _processManager!.Resize(80, 25));
        
        Assert.That(ex.Message, Does.Contain("No process is currently running"));
    }

    [Test]
    public async Task StartAsync_WithValidShell_StartsProcess()
    {
        // Arrange - use a command that will run for a bit longer
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

    [Test]
    public async Task StopAsync_WithRunningProcess_StopsProcess()
    {
        // Arrange - use a command that will definitely run for a while
        var options = ProcessLaunchOptions.CreateCmd();
        options.Arguments.AddRange(["/c", "ping -n 10 127.0.0.1"]);
        await _processManager!.StartAsync(options);
        
        Assert.That(_processManager.IsRunning, Is.True);

        // Act
        await _processManager.StopAsync();

        // Assert
        Assert.That(_processManager.IsRunning, Is.False);
    }

    [Test]
    public async Task StopAsync_WithoutRunningProcess_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrowAsync(() => _processManager!.StopAsync());
    }

    [Test]
    public async Task ProcessExited_Event_RaisedWhenProcessExits()
    {
        // Arrange
        var options = ProcessLaunchOptions.CreateCmd();
        options.Arguments.AddRange(["/c", "echo test"]);
        var exitedEventRaised = false;
        var exitCode = -1;

        _processManager!.ProcessExited += (sender, args) =>
        {
            exitedEventRaised = true;
            exitCode = args.ExitCode;
        };

        // Act
        await _processManager.StartAsync(options);
        
        // Wait for the process to exit naturally
        var timeout = TimeSpan.FromSeconds(5);
        var start = DateTime.UtcNow;
        
        while (_processManager.IsRunning && DateTime.UtcNow - start < timeout)
        {
            await Task.Delay(100);
        }

        // Assert
        Assert.That(exitedEventRaised, Is.True, "ProcessExited event should be raised");
        Assert.That(exitCode, Is.EqualTo(0), "Exit code should be 0 for successful echo command");
        Assert.That(_processManager.IsRunning, Is.False, "Process should not be running after exit");
    }

    [Test]
    public async Task DataReceived_Event_RaisedWhenProcessOutputsData()
    {
        // Arrange
        var options = ProcessLaunchOptions.CreateCmd();
        options.Arguments.AddRange(["/c", "echo Hello World"]);
        var dataReceived = false;
        var receivedData = "";

        _processManager!.DataReceived += (sender, args) =>
        {
            dataReceived = true;
            receivedData += System.Text.Encoding.UTF8.GetString(args.Data.Span);
        };

        // Act
        await _processManager.StartAsync(options);
        
        // Wait for data to be received
        var timeout = TimeSpan.FromSeconds(5);
        var start = DateTime.UtcNow;
        
        while (!dataReceived && DateTime.UtcNow - start < timeout)
        {
            await Task.Delay(100);
        }

        await _processManager.StopAsync();

        // Assert
        Assert.That(dataReceived, Is.True, "DataReceived event should be raised");
        Assert.That(receivedData, Does.Contain("Hello World"), "Should receive the echo output");
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var manager = new ProcessManager();

        // Act & Assert
        Assert.DoesNotThrow(() => manager.Dispose());
        Assert.DoesNotThrow(() => manager.Dispose());
    }

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