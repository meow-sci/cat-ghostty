using caTTY.CustomShells.Examples;
using caTTY.ShellContract;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Text;

namespace caTTY.CustomShells.Tests.Unit;

/// <summary>
///     Unit tests for example shell implementations.
/// </summary>
[TestFixture]
public class ExampleShellTests
{
    [Test]
    public async Task EchoShell_EchoesInputBytes()
    {
        using var shell = new EchoShell();
        var receivedData = new ConcurrentQueue<byte[]>();
        shell.OutputReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());

        // Send some bytes
        await shell.WriteInputAsync(Encoding.UTF8.GetBytes("hello"));
        await Task.Delay(100); // Wait for processing

        await shell.StopAsync();

        var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));
        Assert.That(allOutput, Does.Contain("hello"), "Input should be echoed");
    }

    [Test]
    public async Task EchoShell_HasInitialOutput()
    {
        using var shell = new EchoShell();
        var receivedData = new ConcurrentQueue<byte[]>();
        shell.OutputReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());
        shell.SendInitialOutput();
        await Task.Delay(100);

        await shell.StopAsync();

        var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));
        Assert.That(allOutput, Does.Contain("Echo Shell"), "Should display banner");
    }

    [Test]
    public async Task CalculatorShell_EvaluatesExpressions()
    {
        using var shell = new CalculatorShell();
        var receivedData = new ConcurrentQueue<byte[]>();
        shell.OutputReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());

        // Execute calculation
        await shell.WriteInputAsync(Encoding.UTF8.GetBytes("2 + 2\r"));
        await Task.Delay(100);

        await shell.StopAsync();

        var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));
        Assert.That(allOutput, Does.Contain("= 4"), "Should calculate 2 + 2 = 4");
    }

    [Test]
    public async Task CalculatorShell_HandlesInvalidExpressions()
    {
        using var shell = new CalculatorShell();
        var receivedData = new ConcurrentQueue<byte[]>();
        shell.OutputReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());

        // Send invalid expression
        await shell.WriteInputAsync(Encoding.UTF8.GetBytes("invalid\r"));
        await Task.Delay(100);

        await shell.StopAsync();

        var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));
        Assert.That(allOutput, Does.Contain("Error"), "Should show error for invalid expression");
    }

    [Test]
    public async Task CalculatorShell_HasPrompt()
    {
        using var shell = new CalculatorShell();
        var receivedData = new ConcurrentQueue<byte[]>();
        shell.OutputReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());
        shell.SendInitialOutput();
        await Task.Delay(100);

        await shell.StopAsync();

        var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));
        Assert.That(allOutput, Does.Contain("calc>"), "Should display calc> prompt");
    }

    [Test]
    public async Task RawShell_NoEchoMode()
    {
        using var shell = new RawShell();
        var receivedData = new ConcurrentQueue<byte[]>();
        shell.OutputReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());

        // Type characters (should NOT be echoed)
        await shell.WriteInputAsync(Encoding.UTF8.GetBytes("test"));
        await Task.Delay(100);

        await shell.StopAsync();

        var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));

        // Should NOT contain "test" echoed character-by-character
        // (RawShell only outputs when command is executed, not during typing)
        // Since we didn't press Enter, there should be no "test" in output
        Assert.That(allOutput, Does.Not.Contain("test"), "Raw mode should not echo input characters");
    }

    [Test]
    public async Task RawShell_ExecutesCommandsOnEnter()
    {
        using var shell = new RawShell();
        var receivedData = new ConcurrentQueue<byte[]>();
        shell.OutputReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());

        // Type command with Enter
        await shell.WriteInputAsync(Encoding.UTF8.GetBytes("hello\r"));
        await Task.Delay(100);

        await shell.StopAsync();

        var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));
        Assert.That(allOutput, Does.Contain("You typed: hello"), "Should execute command on Enter");
    }

    [Test]
    public async Task RawShell_HasBanner()
    {
        using var shell = new RawShell();
        var receivedData = new ConcurrentQueue<byte[]>();
        shell.OutputReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

        await shell.StartAsync(CustomShellStartOptions.CreateDefault());
        shell.SendInitialOutput();
        await Task.Delay(100);

        await shell.StopAsync();

        var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));
        Assert.That(allOutput, Does.Contain("Raw Shell"), "Should display banner");
    }

    [Test]
    public async Task AllExampleShells_HaveMetadata()
    {
        var echoShell = new EchoShell();
        var calcShell = new CalculatorShell();
        var rawShell = new RawShell();

        Assert.That(echoShell.Metadata.Name, Is.EqualTo("Echo Shell"));
        Assert.That(calcShell.Metadata.Name, Is.EqualTo("Calculator Shell"));
        Assert.That(rawShell.Metadata.Name, Is.EqualTo("Raw Shell"));

        echoShell.Dispose();
        calcShell.Dispose();
        rawShell.Dispose();
    }
}
