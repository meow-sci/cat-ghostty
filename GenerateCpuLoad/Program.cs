using System;
using System.Threading;
using System.Threading.Tasks;

namespace GenerateCpuLoad;

internal static class Program
{
    // Set to -1 to use all logical processors (default).
    public static int ThreadsToUse = 12;

    // Work granularity; higher values increase per-iteration work.
    public static int InnerIterations = 1000;

    private static async Task Main(string[] args)
    {
        if (ThreadsToUse <= 0)
            ThreadsToUse = Environment.ProcessorCount;

        Console.WriteLine($"GenerateCpuLoad starting. Threads: {ThreadsToUse}, InnerIterations: {InnerIterations}");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("Cancellation requested...");
        };

        var tasks = new Task[ThreadsToUse];
        for (int i = 0; i < ThreadsToUse; i++)
        {
            tasks[i] = Task.Run(() => BusyLoop(cts.Token), cts.Token);
        }

        Console.WriteLine("Press Ctrl+C to stop.");
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("Stopped.");
    }

    private static void BusyLoop(CancellationToken ct)
    {
        double x = 1.0;
        while (!ct.IsCancellationRequested)
        {
            for (int i = 0; i < InnerIterations; i++)
            {
                x += Math.Sqrt(i + x);
            }
        }

        // Prevent optimizer from removing loop
        if (x == double.MinValue) Console.WriteLine(x);
    }
}
