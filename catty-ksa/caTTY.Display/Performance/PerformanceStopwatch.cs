using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace caTTY.Display.Performance;

/// <summary>
/// High-precision performance measurement tool for tracking rendering performance.
/// Uses QueryPerformanceCounter via Stopwatch.GetTimestamp() for microsecond precision.
/// </summary>
public class PerformanceStopwatch
{
    private struct TimingRecord
    {
        public string TaskName;
        public long StartTicks;
        public long EndTicks;
    }

    private struct AggregatedTiming
    {
        public string TaskName;
        public double TotalMilliseconds;
        public int Count;
        public double AverageMicroseconds;
    }

    private readonly List<TimingRecord> _timings = new();
    private readonly Dictionary<string, long> _activeTimings = new();
    private readonly object _lock = new();
    private int _frameCount = 0;

    /// <summary>
    /// Runtime toggle for performance tracing. Default: false (no overhead when disabled).
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Auto-dump frequency in frames. Default: 60 frames (~1 second at 60 FPS).
    /// </summary>
    public int DumpIntervalFrames { get; set; } = 60;

    /// <summary>
    /// Record high-precision start timestamp for a task (no-op if !Enabled).
    /// </summary>
    /// <param name="taskName">Name of the task to measure</param>
    public void Start(string taskName)
    {
        if (!Enabled) return;

        var ticks = Stopwatch.GetTimestamp();
        lock (_lock)
        {
            _activeTimings[taskName] = ticks;
        }
    }

    /// <summary>
    /// Record high-precision end timestamp for a task (no-op if !Enabled).
    /// </summary>
    /// <param name="taskName">Name of the task to measure</param>
    public void Stop(string taskName)
    {
        if (!Enabled) return;

        var endTicks = Stopwatch.GetTimestamp();
        lock (_lock)
        {
            if (_activeTimings.TryGetValue(taskName, out var startTicks))
            {
                _timings.Add(new TimingRecord
                {
                    TaskName = taskName,
                    StartTicks = startTicks,
                    EndTicks = endTicks
                });
                _activeTimings.Remove(taskName);
            }
        }
    }

    /// <summary>
    /// Called at end of each frame. Auto-dumps to console if frame count >= DumpIntervalFrames.
    /// </summary>
    public void OnFrameEnd()
    {
        if (!Enabled) return;

        _frameCount++;
        if (_frameCount >= DumpIntervalFrames)
        {
            DumpToConsole();
            Reset();
        }
    }

    /// <summary>
    /// Clear all stored timings and reset frame counter.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _timings.Clear();
            _activeTimings.Clear();
            _frameCount = 0;
        }
    }

    /// <summary>
    /// Aggregate timings and return formatted ASCII table string.
    /// </summary>
    /// <returns>Formatted performance summary</returns>
    public string GetSummary()
    {
        List<TimingRecord> timingsCopy;
        int frameCount;

        lock (_lock)
        {
            timingsCopy = new List<TimingRecord>(_timings);
            frameCount = _frameCount;
        }

        if (timingsCopy.Count == 0)
        {
            return "No performance data collected.";
        }

        // Aggregate by task name
        var aggregated = timingsCopy
            .GroupBy(t => t.TaskName)
            .Select(group =>
            {
                var totalTicks = group.Sum(t => t.EndTicks - t.StartTicks);
                var count = group.Count();
                var totalMs = totalTicks * 1000.0 / Stopwatch.Frequency;
                var avgUs = totalTicks * 1_000_000.0 / Stopwatch.Frequency / count;

                return new AggregatedTiming
                {
                    TaskName = group.Key,
                    TotalMilliseconds = totalMs,
                    Count = count,
                    AverageMicroseconds = avgUs
                };
            })
            .OrderByDescending(a => a.TotalMilliseconds)
            .ToList();

        // Calculate total render time
        var totalRenderTime = aggregated
            .Where(a => a.TaskName == "TerminalController.Render")
            .Sum(a => a.TotalMilliseconds);
        var avgFrameTime = frameCount > 0 ? totalRenderTime / frameCount : 0;

        // Build formatted table
        var sb = new StringBuilder();
        sb.AppendLine($"Performance Summary ({frameCount} frames, {avgFrameTime:F2}ms average frame time)");
        sb.AppendLine("┌─────────────────────────────────┬──────────────┬───────┬──────────────┐");
        sb.AppendLine("│ Task Name                       │ Total (ms)   │ Count │ Avg (µs)     │");
        sb.AppendLine("├─────────────────────────────────┼──────────────┼───────┼──────────────┤");

        foreach (var timing in aggregated)
        {
            var taskName = timing.TaskName.Length > 31
                ? timing.TaskName.Substring(0, 28) + "..."
                : timing.TaskName;

            sb.AppendLine(
                $"│ {taskName,-31} │ {timing.TotalMilliseconds,12:F2} │ {timing.Count,5} │ {timing.AverageMicroseconds,12:F2} │");
        }

        sb.AppendLine("└─────────────────────────────────┴──────────────┴───────┴──────────────┘");

        return sb.ToString();
    }

    /// <summary>
    /// Calls GetSummary() and writes to Console.WriteLine() with separators and timestamp.
    /// </summary>
    public void DumpToConsole()
    {
        var summary = GetSummary();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        Console.WriteLine("================================================================================");
        Console.WriteLine($"[{timestamp}] {summary}");
        Console.WriteLine("================================================================================");
    }
}
