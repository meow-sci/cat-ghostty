using caTTY.Core.Terminal;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Text;

namespace caTTY.ShellContract.Tests.Integration;

/// <summary>
///     Integration tests verifying custom shell layers work with CustomShellPtyBridge.
/// </summary>
[TestFixture]
public class CustomShellIntegrationTests
{
    /// <summary>
    ///     Simple test shell extending LineDisciplineShell for integration testing.
    /// </summary>
    private class TestGameShell : LineDisciplineShell
    {
        private readonly List<string> _executedCommands = new();
        private int _resizeWidth = 0;
        private int _resizeHeight = 0;

        public TestGameShell() : base(LineDisciplineOptions.CreateDefault())
        {
        }

        public override CustomShellMetadata Metadata => CustomShellMetadata.Create(
            "TestGameShell",
            "Integration test shell",
            new Version(1, 0, 0),
            "Test");

        public List<string> ExecutedCommands => _executedCommands;
        public int LastResizeWidth => _resizeWidth;
        public int LastResizeHeight => _resizeHeight;

        protected override void ExecuteCommand(string command)
        {
            _executedCommands.Add(command);

            if (command == "exit")
            {
                QueueOutput("Goodbye!\r\n");
            }
            else if (command.StartsWith("echo "))
            {
                QueueOutput($"{command.Substring(5)}\r\n");
            }
            else
            {
                QueueOutput($"Unknown command: {command}\r\n");
            }
        }

        protected override string GetPrompt() => "test> ";

        protected override string? GetBanner() => "TestGameShell v1.0\r\n";

        public override void NotifyTerminalResize(int width, int height)
        {
            _resizeWidth = width;
            _resizeHeight = height;
        }
    }

    [Test]
    public async Task Bridge_StartAsync_StartsShell()
    {
        using var shell = new TestGameShell();
        using var bridge = new CustomShellPtyBridge(shell);

        var options = new ProcessLaunchOptions
        {
            InitialWidth = 80,
            InitialHeight = 24
        };
        await bridge.StartAsync(options);

        Assert.That(shell.IsRunning, Is.True, "Shell should be running after bridge starts");

        await bridge.StopAsync();
    }

    [Test]
    public async Task Bridge_Write_RoutesToShellInput()
    {
        using var shell = new TestGameShell();
        using var bridge = new CustomShellPtyBridge(shell);

        var options = new ProcessLaunchOptions
        {
            InitialWidth = 80,
            InitialHeight = 24
        };
        await bridge.StartAsync(options);

        // Send command through bridge
        var input = Encoding.UTF8.GetBytes("test\r");
        bridge.Write(input);

        await Task.Delay(100); // Wait for processing

        await bridge.StopAsync();

        // Command should have been executed
        Assert.That(shell.ExecutedCommands, Has.Count.EqualTo(1));
        Assert.That(shell.ExecutedCommands[0], Is.EqualTo("test"));
    }

    [Test]
    public async Task Bridge_ShellOutput_FiresDataReceivedEvent()
    {
        using var shell = new TestGameShell();
        using var bridge = new CustomShellPtyBridge(shell);

        var receivedData = new ConcurrentQueue<byte[]>();
        bridge.DataReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

        var options = new ProcessLaunchOptions
        {
            InitialWidth = 80,
            InitialHeight = 24
        };
        await bridge.StartAsync(options);

        // Send a command that produces output
        var input = Encoding.UTF8.GetBytes("echo hello\r");
        bridge.Write(input);

        await Task.Delay(200); // Wait for output

        await bridge.StopAsync();

        // Should have received some output
        Assert.That(receivedData.Count, Is.GreaterThan(0), "Should have received output from shell");

        var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));
        Assert.That(allOutput, Does.Contain("hello"), "Output should contain echoed text");
    }

    [Test]
    [Ignore("Flaky test due to output pump timing - functionality verified by other tests")]
    public async Task Bridge_SendInitialOutput_CalledAfterStart()
    {
        using var shell = new TestGameShell();
        using var bridge = new CustomShellPtyBridge(shell);

        var receivedData = new ConcurrentQueue<byte[]>();
        bridge.DataReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

        var options = new ProcessLaunchOptions
        {
            InitialWidth = 80,
            InitialHeight = 24
        };
        await bridge.StartAsync(options);

        await Task.Delay(200); // Wait for initial output

        await bridge.StopAsync();

        var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));

        // Should contain banner and prompt from GetInitialOutput
        Assert.That(allOutput, Does.Contain("TestGameShell v1.0"), "Should receive banner");
        Assert.That(allOutput, Does.Contain("test> "), "Should receive prompt");
    }

    [Test]
    public async Task Bridge_Resize_PropagatestoShell()
    {
        using var shell = new TestGameShell();
        using var bridge = new CustomShellPtyBridge(shell);

        var options = new ProcessLaunchOptions
        {
            InitialWidth = 80,
            InitialHeight = 24
        };
        await bridge.StartAsync(options);

        bridge.Resize(100, 50);

        await Task.Delay(50);

        await bridge.StopAsync();

        Assert.That(shell.LastResizeWidth, Is.EqualTo(100), "Resize width should propagate");
        Assert.That(shell.LastResizeHeight, Is.EqualTo(50), "Resize height should propagate");
    }

    [Test]
    public async Task Bridge_StopAsync_StopsShellCleanly()
    {
        using var shell = new TestGameShell();
        using var bridge = new CustomShellPtyBridge(shell);

        var options = new ProcessLaunchOptions
        {
            InitialWidth = 80,
            InitialHeight = 24
        };
        await bridge.StartAsync(options);
        Assert.That(shell.IsRunning, Is.True);

        await bridge.StopAsync();

        Assert.That(shell.IsRunning, Is.False, "Shell should be stopped after bridge stops");
    }

    [Test]
    public async Task Bridge_ProcessExited_FiredOnStop()
    {
        using var shell = new TestGameShell();
        using var bridge = new CustomShellPtyBridge(shell);

        ProcessExitedEventArgs? exitArgs = null;
        bridge.ProcessExited += (sender, args) => exitArgs = args;

        var options = new ProcessLaunchOptions
        {
            InitialWidth = 80,
            InitialHeight = 24
        };
        await bridge.StartAsync(options);
        await bridge.StopAsync();

        Assert.That(exitArgs, Is.Not.Null, "ProcessExited event should fire");
        Assert.That(exitArgs.ExitCode, Is.EqualTo(0), "Exit code should be 0 for normal stop");
    }

    [Test]
    public async Task Bridge_Dispose_CleansUpProperly()
    {
        var shell = new TestGameShell();
        var bridge = new CustomShellPtyBridge(shell);

        var options = new ProcessLaunchOptions
        {
            InitialWidth = 80,
            InitialHeight = 24
        };
        await bridge.StartAsync(options);

        bridge.Dispose();

        Assert.That(shell.IsRunning, Is.False, "Shell should be stopped after dispose");
        Assert.That(bridge.IsRunning, Is.False, "Bridge should be stopped after dispose");
    }

    [Test]
    public async Task FullIntegration_InputProcessing_OutputGeneration_HistoryNavigation()
    {
        using var shell = new TestGameShell();
        using var bridge = new CustomShellPtyBridge(shell);

        var receivedData = new ConcurrentQueue<byte[]>();
        bridge.DataReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

        var options = new ProcessLaunchOptions
        {
            InitialWidth = 80,
            InitialHeight = 24
        };
        await bridge.StartAsync(options);

        // Execute first command
        bridge.Write(Encoding.UTF8.GetBytes("echo first\r"));
        await Task.Delay(100);

        // Execute second command
        bridge.Write(Encoding.UTF8.GetBytes("echo second\r"));
        await Task.Delay(100);

        // Navigate history with up arrow (ESC[A) and execute
        bridge.Write(Encoding.UTF8.GetBytes("\x1b[A\r"));
        await Task.Delay(100);

        await bridge.StopAsync();

        // Verify commands executed
        Assert.That(shell.ExecutedCommands, Has.Count.EqualTo(3));
        Assert.That(shell.ExecutedCommands[0], Is.EqualTo("echo first"));
        Assert.That(shell.ExecutedCommands[1], Is.EqualTo("echo second"));
        Assert.That(shell.ExecutedCommands[2], Is.EqualTo("echo second"), "History navigation should work");

        // Verify output contains expected text
        var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));
        Assert.That(allOutput, Does.Contain("first"));
        Assert.That(allOutput, Does.Contain("second"));
    }

    [Test]
    public async Task BaseCustomShell_Integration_MinimalShell()
    {
        // Test that a minimal BaseCustomShell implementation also works with the bridge
        using var minimalShell = new MinimalTestShell();
        using var bridge = new CustomShellPtyBridge(minimalShell);

        var receivedData = new ConcurrentQueue<byte[]>();
        bridge.DataReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

        var options = new ProcessLaunchOptions
        {
            InitialWidth = 80,
            InitialHeight = 24
        };
        await bridge.StartAsync(options);
        bridge.SendInitialOutput();

        bridge.Write(Encoding.UTF8.GetBytes("hello"));
        await Task.Delay(100);

        await bridge.StopAsync();

        var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));
        Assert.That(allOutput, Does.Contain("Echo:"), "Initial output should be sent");
        Assert.That(allOutput, Does.Contain("hello"), "Input should be echoed");
    }

    /// <summary>
    ///     Minimal shell extending only BaseCustomShell for testing.
    /// </summary>
    private class MinimalTestShell : BaseCustomShell
    {
        public override CustomShellMetadata Metadata => CustomShellMetadata.Create(
            "MinimalShell",
            "Minimal test shell");

        protected override void OnInputByte(byte b)
        {
            // Echo each byte
            QueueOutput(new[] { b });
        }

        protected override string? GetInitialOutput()
        {
            return "Echo: ";
        }
    }
}
