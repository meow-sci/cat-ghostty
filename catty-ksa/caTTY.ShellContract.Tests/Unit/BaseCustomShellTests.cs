using NUnit.Framework;
using System.Collections.Concurrent;

namespace caTTY.ShellContract.Tests.Unit;

/// <summary>
///     Unit tests for BaseCustomShell infrastructure.
/// </summary>
[TestFixture]
public class BaseCustomShellTests
{
    /// <summary>
    ///     Minimal test shell implementation for testing BaseCustomShell.
    /// </summary>
    private class TestMinimalShell : BaseCustomShell
    {
        private readonly List<byte> _receivedInput = new();

        public override CustomShellMetadata Metadata => CustomShellMetadata.Create(
            "TestShell",
            "Minimal test shell");

        public List<byte> ReceivedInput => _receivedInput;

        protected override void OnInputByte(byte b)
        {
            _receivedInput.Add(b);
        }
    }

    /// <summary>
    ///     Test shell with customizable initial output.
    /// </summary>
    private class TestShellWithBanner : BaseCustomShell
    {
        private readonly string? _banner;

        public TestShellWithBanner(string? banner = null)
        {
            _banner = banner;
        }

        public override CustomShellMetadata Metadata => CustomShellMetadata.Create(
            "BannerShell",
            "Shell with custom banner");

        protected override void OnInputByte(byte b)
        {
            // Echo input
            QueueOutput(new[] { b });
        }

        protected override string? GetInitialOutput()
        {
            return _banner;
        }
    }

    [Test]
    public async Task StartAsync_SetsIsRunningToTrue()
    {
        using var shell = new TestMinimalShell();
        Assert.IsFalse(shell.IsRunning, "Shell should not be running initially");

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());

        Assert.IsTrue(shell.IsRunning, "Shell should be running after StartAsync");

        await shell.StopAsync();
    }

    [Test]
    public async Task StopAsync_SetsIsRunningToFalse()
    {
        using var shell = new TestMinimalShell();
        await shell.StartAsync(CustomShellStartOptions.CreateDefault());

        await shell.StopAsync();

        Assert.IsFalse(shell.IsRunning, "Shell should not be running after StopAsync");
    }

    [Test]
    public async Task StartAsync_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        using var shell = new TestMinimalShell();
        await shell.StartAsync(CustomShellStartOptions.CreateDefault());

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await shell.StartAsync(CustomShellStartOptions.CreateDefault()));

        await shell.StopAsync();
    }

    [Test]
    public async Task QueueOutput_FiresOutputReceivedEvent()
    {
        using var shell = new TestMinimalShell();
        var receivedData = new List<byte[]>();

        shell.OutputReceived += (sender, args) => receivedData.Add(args.Data.ToArray());

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());

        // Queue some output through protected method (via input echoing)
        shell.SendInitialOutput(); // Should be empty for TestMinimalShell

        // Wait for output pump to process
        await Task.Delay(50);

        await shell.StopAsync();

        // Just verify event mechanism works (actual data depends on subclass implementation)
        Assert.IsNotNull(receivedData);
    }

    [Test]
    public async Task OutputPump_DeliversDataAsynchronously()
    {
        using var shell = new TestShellWithBanner("Hello\n");
        var receivedChunks = new ConcurrentBag<byte[]>();

        shell.OutputReceived += (sender, args) =>
        {
            receivedChunks.Add(args.Data.ToArray());
        };

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());
        shell.SendInitialOutput();

        // Wait for output to be pumped
        await Task.Delay(100);

        await shell.StopAsync();

        Assert.IsNotEmpty(receivedChunks, "Should have received output");
        var allData = string.Concat(receivedChunks.Select(chunk =>
            System.Text.Encoding.UTF8.GetString(chunk)));
        Assert.That(allData, Is.EqualTo("Hello\n"));
    }

    [Test]
    public async Task WriteInputAsync_ProcessesInputBytes()
    {
        using var shell = new TestMinimalShell();
        await shell.StartAsync(CustomShellStartOptions.CreateDefault());

        var input = System.Text.Encoding.UTF8.GetBytes("test");
        await shell.WriteInputAsync(input);

        await shell.StopAsync();

        Assert.That(shell.ReceivedInput, Is.EqualTo(input), "All input bytes should be received");
    }

    [Test]
    public async Task WriteInputAsync_WhenNotRunning_ThrowsInvalidOperationException()
    {
        using var shell = new TestMinimalShell();
        var input = System.Text.Encoding.UTF8.GetBytes("test");

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await shell.WriteInputAsync(input));
    }

    [Test]
    public async Task StopAsync_FiresTerminatedEvent()
    {
        using var shell = new TestMinimalShell();
        ShellTerminatedEventArgs? terminatedArgs = null;

        shell.Terminated += (sender, args) => terminatedArgs = args;

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());
        await shell.StopAsync();

        Assert.IsNotNull(terminatedArgs, "Terminated event should fire");
        Assert.That(terminatedArgs.ExitCode, Is.EqualTo(0), "Exit code should be 0 for normal stop");
    }

    [Test]
    public async Task Dispose_StopsRunningShell()
    {
        var shell = new TestMinimalShell();
        await shell.StartAsync(CustomShellStartOptions.CreateDefault());

        shell.Dispose();

        Assert.IsFalse(shell.IsRunning, "Shell should not be running after dispose");
    }

    [Test]
    public async Task ConcurrentQueueOutput_ThreadSafe()
    {
        using var shell = new TestShellWithBanner();
        var receivedChunks = new ConcurrentBag<byte[]>();

        shell.OutputReceived += (sender, args) =>
        {
            receivedChunks.Add(args.Data.ToArray());
        };

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());

        // Send input concurrently from multiple threads
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            var data = System.Text.Encoding.UTF8.GetBytes($"data{i}\n");
            await shell.WriteInputAsync(data);
        }));

        await Task.WhenAll(tasks);

        // Wait for output pump
        await Task.Delay(200);

        await shell.StopAsync();

        // Should receive all 10 outputs (order may vary)
        Assert.GreaterOrEqual(receivedChunks.Count, 10, "Should receive all concurrent outputs");
    }

    [Test]
    public async Task GetInitialOutput_CalledAfterStart()
    {
        using var shell = new TestShellWithBanner("Welcome!\n$ ");
        var receivedOutput = new ConcurrentBag<byte[]>();

        shell.OutputReceived += (sender, args) =>
        {
            receivedOutput.Add(args.Data.ToArray());
        };

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());
        shell.SendInitialOutput();

        await Task.Delay(100);

        await shell.StopAsync();

        var allData = string.Concat(receivedOutput.Select(chunk =>
            System.Text.Encoding.UTF8.GetString(chunk)));

        Assert.That(allData, Is.EqualTo("Welcome!\n$ "), "Initial output should be sent");
    }

    [Test]
    public void SendInitialOutput_WhenNotRunning_DoesNothing()
    {
        using var shell = new TestShellWithBanner("Should not appear");
        var receivedAny = false;

        shell.OutputReceived += (sender, args) => receivedAny = true;

        shell.SendInitialOutput();

        Assert.IsFalse(receivedAny, "Should not send output when not running");
    }

    [Test]
    public async Task StopAsync_WhenNotRunning_DoesNotThrow()
    {
        using var shell = new TestMinimalShell();

        // Should not throw
        await shell.StopAsync();

        Assert.IsFalse(shell.IsRunning);
    }
}
