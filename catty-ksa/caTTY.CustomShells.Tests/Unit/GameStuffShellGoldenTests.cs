using caTTY.CustomShells.GameStuffShell.Execution;
using caTTY.CustomShells.GameStuffShell.Lexing;
using caTTY.CustomShells.GameStuffShell.Parsing;
using caTTY.CustomShells.GameStuffShell.Programs;
using NUnit.Framework;

namespace caTTY.CustomShells.Tests.Unit;

/// <summary>
/// Golden end-to-end integration tests that exercise the full shell pipeline:
/// lexer → parser → executor → programs.
/// </summary>
[TestFixture]
public class GameStuffShellGoldenTests
{
    [Test]
    public async Task Golden_EchoPipeXargs_ProducesMultipleLines()
    {
        // echo a b | xargs echo
        // Expected: "a\n" + "b\n" (xargs invokes echo twice)
        var result = await ExecuteCommandLine("echo a b | xargs echo");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.EqualTo("a\nb\n"));
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_RedirectionOrdering_2To1Then1ToNull()
    {
        // echo error 2>&1 1>/dev/null
        // Expected: stderr redirected to original stdout, then stdout redirected to /dev/null
        // Result: "error\n" goes to original stdout (which is our test stdout)
        var result = await ExecuteCommandLine("echo error 2>&1 1>/dev/null");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        // Redirections are applied left-to-right:
        // 1. 2>&1 copies FD 1 to FD 2 (stderr now points to original stdout)
        // 2. 1>/dev/null redirects FD 1 to /dev/null
        // Result: stdout is /dev/null, stderr is original stdout
        // Since echo writes to stdout (FD 1), and it's redirected to /dev/null, nothing appears
        Assert.That(result.Stdout, Is.Empty);
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_RedirectionOrdering_1ToNullThen2To1()
    {
        // echo error 1>/dev/null 2>&1
        // Expected: stdout redirected to /dev/null first, then stderr copies that
        // Result: both stdout and stderr go to /dev/null
        var result = await ExecuteCommandLine("echo error 1>/dev/null 2>&1");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.Empty);
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_WordJoining_AdjacentQuotedAndUnquoted()
    {
        // echo a"b"c'd'e
        // Expected: single word "abcde"
        var result = await ExecuteCommandLine("echo a\"b\"c'd'e");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.EqualTo("abcde\n"));
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_EmptyStringArguments_PreservedInQuotes()
    {
        // echo "" '' a
        // Expected: three arguments (two empty, one "a")
        var result = await ExecuteCommandLine("echo \"\" '' a");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.EqualTo("  a\n")); // Two empty strings become two spaces
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_ListSemantics_SequentialExecution()
    {
        // echo first ; echo second ; echo third
        var result = await ExecuteCommandLine("echo first ; echo second ; echo third");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.EqualTo("first\nsecond\nthird\n"));
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_AndIfLogic_RunsAfterSuccess()
    {
        // echo -n success && echo " continued"
        var result = await ExecuteCommandLine("echo -n success && echo \" continued\"");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.EqualTo("success continued\n"));
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_OrIfLogic_SkipsAfterSuccess()
    {
        // echo -n first || echo second
        var result = await ExecuteCommandLine("echo -n first || echo second");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.EqualTo("first"));
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_ComplexPipeline_EchoXargsWithFixedArgs()
    {
        // echo one two three | xargs echo -n
        // Expected: xargs invokes "echo -n one", "echo -n two", "echo -n three"
        // Result: "onetwothree" (no newlines due to -n)
        var result = await ExecuteCommandLine("echo one two three | xargs echo -n");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.EqualTo("onetwothree"));
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_SubshellExecution_SimpleCommand()
    {
        // sh -c "echo nested"
        var result = await ExecuteCommandLine("sh -c \"echo nested\"");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.EqualTo("nested\n"));
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_SubshellExecution_WithPipeline()
    {
        // sh -c "echo a b | xargs echo"
        var result = await ExecuteCommandLine("sh -c \"echo a b | xargs echo\"");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.EqualTo("a\nb\n"));
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_NestedSubshells_DoubleNesting()
    {
        // sh -c "sh -c 'echo deeply nested'"
        var result = await ExecuteCommandLine("sh -c \"sh -c 'echo deeply nested'\"");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.EqualTo("deeply nested\n"));
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_EscapeSequences_InSingleQuotes()
    {
        // echo 'hello\nworld'
        // Expected: backslash-n preserved literally (no interpretation)
        var result = await ExecuteCommandLine("echo 'hello\\nworld'");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.EqualTo("hello\\nworld\n"));
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_Cancellation_SleepProgram()
    {
        // Test that cancellation works correctly
        var cts = new CancellationTokenSource();
        var execTask = ExecuteCommandLineAsync("sleep 10000", cts.Token);

        await Task.Delay(50); // Let sleep start
        cts.Cancel();

        var result = await execTask;

        Assert.That(result.ExitCode, Is.EqualTo(0)); // Sleep exits cleanly on cancellation
    }

    [Test]
    public async Task Golden_MultipleRedirections_SameFileDescriptor()
    {
        // echo test 1>/dev/null 1>/dev/null
        // Last redirection wins
        var result = await ExecuteCommandLine("echo test 1>/dev/null 1>/dev/null");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.Empty);
        Assert.That(result.Stderr, Is.Empty);
    }

    [Test]
    public async Task Golden_PipelineWithRedirection_CombinedStreams()
    {
        // echo foo 2>&1 | xargs echo
        // Combines stderr with stdout, then pipes to xargs
        var result = await ExecuteCommandLine("echo foo 2>&1 | xargs echo");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Stdout, Is.EqualTo("foo\n"));
        Assert.That(result.Stderr, Is.Empty);
    }

    private async Task<ExecutionResult> ExecuteCommandLine(string commandLine)
    {
        return await ExecuteCommandLineAsync(commandLine, CancellationToken.None);
    }

    private async Task<ExecutionResult> ExecuteCommandLineAsync(string commandLine, CancellationToken cancellationToken)
    {
        // Set up program registry with all programs
        var registry = new ProgramRegistry();
        registry.Register(new EchoProgram());
        registry.Register(new SleepProgram());
        registry.Register(new XargsProgram());
        registry.Register(new ShProgram());

        // Set up streams
        var stdoutWriter = new BufferedStreamWriter();
        var stderrWriter = new BufferedStreamWriter();
        var terminalStdout = new BufferedStreamWriter();
        var terminalStderr = new BufferedStreamWriter();

        // Create execution context
        var execContext = new ExecContext(
            programResolver: registry,
            gameApi: null,
            environment: new Dictionary<string, string>(),
            terminalWidth: 80,
            terminalHeight: 24,
            terminalOutputCallback: (text, isError) =>
            {
                if (isError)
                {
                    terminalStderr.WriteAsync(text, CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    terminalStdout.WriteAsync(text, CancellationToken.None).GetAwaiter().GetResult();
                }
            });

        // Lex
        var lexer = new Lexer(commandLine);
        var tokens = lexer.Lex();

        // Parse
        var parser = new Parser(tokens);
        var ast = parser.ParseList();

        // Execute
        var executor = new Executor();
        var exitCode = await executor.ExecuteListAsync(ast, execContext, cancellationToken);

        return new ExecutionResult(
            exitCode,
            terminalStdout.GetContent(),
            terminalStderr.GetContent());
    }

    private record ExecutionResult(int ExitCode, string Stdout, string Stderr);
}
