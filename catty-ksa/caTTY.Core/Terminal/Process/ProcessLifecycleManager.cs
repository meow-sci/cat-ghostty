using System.Runtime.InteropServices;
using SysProcess = System.Diagnostics.Process;

namespace caTTY.Core.Terminal.Process;

/// <summary>
///     Manages process lifecycle operations including graceful shutdown.
///     Handles process creation validation and shutdown orchestration.
/// </summary>
internal static class ProcessLifecycleManager
{
    /// <summary>
    ///     Validates that a process started successfully after a brief delay.
    /// </summary>
    /// <param name="process">The process to validate</param>
    /// <param name="shellPath">The shell path (for error reporting)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that completes when validation is done</returns>
    /// <exception cref="ProcessStartException">Thrown if the process exited immediately</exception>
    internal static async Task ValidateProcessStartAsync(SysProcess process, string shellPath, CancellationToken cancellationToken)
    {
        // Wait a short time to ensure the process started successfully
        await Task.Delay(100, cancellationToken);

        // Check if process exited immediately
        try
        {
            if (process.HasExited)
            {
                int exitCode = process.ExitCode;
                throw new ProcessStartException(
                    $"Shell process exited immediately with code {exitCode}: {shellPath}", shellPath);
            }
        }
        catch (InvalidOperationException)
        {
            // Process has already exited and been disposed - let the exit handler deal with cleanup
        }
    }

    /// <summary>
    ///     Performs graceful shutdown of a process with fallback to forced termination.
    /// </summary>
    /// <param name="process">The process to stop</param>
    /// <param name="readCancellationSource">Cancellation source for read operations</param>
    /// <param name="outputReadTask">The output read task to wait for</param>
    /// <returns>A task that completes when the process has stopped</returns>
    internal static async Task StopProcessGracefullyAsync(
        SysProcess? process,
        CancellationTokenSource? readCancellationSource,
        Task? outputReadTask)
    {
        if (process == null)
        {
            return; // No process running
        }

        // Cancel read operations
        readCancellationSource?.Cancel();

        // Try graceful shutdown first
        if (!process.HasExited)
        {
            try
            {
                // For ConPTY processes, we can try CloseMainWindow first, then Kill if needed
                process.CloseMainWindow();

                // Wait a short time for graceful shutdown
                if (!process.WaitForExit(2000))
                {
                    process.Kill(true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }
        }

        // Wait for read task to complete
        if (outputReadTask != null)
        {
            try
            {
                await outputReadTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }
    }

    /// <summary>
    ///     Creates a Windows process using ConPTY with the specified command and startup info.
    /// </summary>
    /// <param name="commandLine">The command line to execute</param>
    /// <param name="workingDirectory">The working directory for the process</param>
    /// <param name="startupInfo">The startup information with attribute list</param>
    /// <returns>The process information structure</returns>
    /// <exception cref="ProcessStartException">Thrown if process creation fails</exception>
    internal static ConPtyNative.PROCESS_INFORMATION CreateProcess(
        string commandLine,
        string workingDirectory,
        ref ConPtyNative.STARTUPINFOEX startupInfo)
    {
        var processInfo = new ConPtyNative.PROCESS_INFORMATION();

        if (!ConPtyNative.CreateProcessW(
                null,
                commandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                ConPtyNative.EXTENDED_STARTUPINFO_PRESENT,
                IntPtr.Zero,
                workingDirectory,
                ref startupInfo,
                out processInfo))
        {
            int error = Marshal.GetLastWin32Error();
            throw new ProcessStartException($"Failed to create process: {error}");
        }

        return processInfo;
    }

    /// <summary>
    ///     Wraps a Windows process handle in a managed Process object with event handling.
    /// </summary>
    /// <param name="processInfo">The process information from CreateProcessW</param>
    /// <param name="onProcessExited">Event handler for process exit</param>
    /// <returns>A managed Process object</returns>
    internal static SysProcess WrapProcessHandle(ConPtyNative.PROCESS_INFORMATION processInfo, EventHandler onProcessExited)
    {
        var process = SysProcess.GetProcessById(processInfo.dwProcessId);
        process.EnableRaisingEvents = true;
        process.Exited += onProcessExited;

        // Close process and thread handles (we have the Process object now)
        ConPtyNative.CloseHandle(processInfo.hProcess);
        ConPtyNative.CloseHandle(processInfo.hThread);

        return process;
    }
}
