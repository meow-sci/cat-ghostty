namespace caTTY.Core.Rpc;

/// <summary>
/// Represents the result of executing an RPC command.
/// </summary>
public record RpcResult
{
    /// <summary>
    /// Indicates whether the command executed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The response data for query commands, null for fire-and-forget commands.
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Error message if the command failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The time taken to execute the command.
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Creates a successful result with optional data.
    /// </summary>
    /// <param name="data">The response data</param>
    /// <param name="executionTime">The execution time</param>
    /// <returns>A successful RPC result</returns>
    public static RpcResult CreateSuccess(object? data = null, TimeSpan executionTime = default)
    {
        return new RpcResult
        {
            Success = true,
            Data = data,
            ExecutionTime = executionTime
        };
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <param name="executionTime">The execution time</param>
    /// <returns>A failed RPC result</returns>
    public static RpcResult CreateFailure(string errorMessage, TimeSpan executionTime = default)
    {
        return new RpcResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ExecutionTime = executionTime
        };
    }
}