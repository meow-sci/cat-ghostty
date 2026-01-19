using caTTY.CustomShells.GameStuffShell.Execution;
using caTTY.CustomShells.GameStuffShell.Lexing;
using caTTY.CustomShells.GameStuffShell.Parsing;

namespace caTTY.CustomShells.GameStuffShell.Programs;

/// <summary>
/// Implements a subshell that parses and executes a command string.
/// </summary>
public sealed class ShProgram : IProgram
{
    private readonly Executor _executor = new();

    /// <inheritdoc/>
    public string Name => "sh";

    public async Task<int> RunAsync(ProgramContext context, CancellationToken cancellationToken)
    {
        // sh requires -c flag and a command string
        if (context.Argv.Count < 3)
        {
            await context.Streams.Stderr.WriteAsync("sh: usage: sh -c <command>\n", cancellationToken);
            return 2; // Usage error
        }

        if (context.Argv[1] != "-c")
        {
            await context.Streams.Stderr.WriteAsync("sh: only -c flag is supported\n", cancellationToken);
            return 2; // Usage error
        }

        var commandString = context.Argv[2];

        // Lex the command string
        var lexer = new Lexer(commandString);
        IReadOnlyList<Token> tokens;
        try
        {
            tokens = lexer.Lex();
        }
        catch (LexerException ex)
        {
            await context.Streams.Stderr.WriteAsync($"sh: lexer error: {ex.Message}\n", cancellationToken);
            return 2;
        }
        catch (Exception ex)
        {
            await context.Streams.Stderr.WriteAsync($"sh: unexpected lexer error: {ex.Message}\n", cancellationToken);
            return 1;
        }

        // Parse the tokens
        var parser = new Parser(tokens);
        ListNode ast;
        try
        {
            ast = parser.ParseList();
        }
        catch (ParserException ex)
        {
            await context.Streams.Stderr.WriteAsync($"sh: parse error: {ex.Message}\n", cancellationToken);
            return 2;
        }
        catch (Exception ex)
        {
            await context.Streams.Stderr.WriteAsync($"sh: unexpected parser error: {ex.Message}\n", cancellationToken);
            return 1;
        }

        // Create execution context (inherit from parent, but create a new ExecContext)
        var execContext = new ExecContext(
            programResolver: context.ProgramResolver,
            gameApi: context.GameApi,
            environment: context.Environment,
            terminalWidth: context.TerminalWidth,
            terminalHeight: context.TerminalHeight,
            terminalOutputCallback: (text, isError) =>
            {
                // Write to parent's streams
                var targetStream = isError ? context.Streams.Stderr : context.Streams.Stdout;
                targetStream.WriteAsync(text, CancellationToken.None).GetAwaiter().GetResult();
            });

        // Execute the AST
        try
        {
            var exitCode = await _executor.ExecuteListAsync(ast, execContext, cancellationToken);
            return exitCode;
        }
        catch (Exception ex)
        {
            await context.Streams.Stderr.WriteAsync($"sh: execution error: {ex.Message}\n", cancellationToken);
            return 1;
        }
    }
}
