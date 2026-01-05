# ImGui Render-Loop Optimization Analysis (caTTY) - v2

> **Last Updated:** January 2026  
> **Status:** Planning / Analysis Complete  
> **Supersedes:** IMGUI_IMPROVEMENTS.md

## Goal / Ideal Architecture

We want the ImGui frame to be **paint-only**:
- ImGui render code should **only read** a stable, already-prepared terminal "frame" (cells, colors, cursor, selection overlays) and emit draw calls.
- Terminal emulation (UTF-8 decode, escape parsing, stateful buffer mutation) should happen **outside** the ImGui render loop.
- Any expensive "derived view" work (viewport composition, scrollback merging, layout/run building) should happen **on change**, not every frame.

In practice, this usually means:
- A **terminal update pump** that applies process output to the emulator on a controlled thread/tick.
- A **render snapshot** (immutable-ish) produced after updates, read by ImGui.
- A **dirty/invalidations** mechanism so we rebuild only what changed.

---

## Current Architecture (What Actually Happens)

### Data path (process output → terminal state)
- `ProcessManager` reads ConPTY output asynchronously and raises `DataReceived`.
- `TerminalSession` subscribes to `ProcessManager.DataReceived` and immediately forwards bytes to the emulator:
  - `TerminalSession.OnProcessDataReceived` → `Terminal.Write(e.Data.Span)` ([TerminalSession.cs#L235](caTTY.Core/Terminal/TerminalSession.cs#L235))
- `TerminalEmulator.Write(ReadOnlySpan<byte>)` does parsing/state updates immediately:
  - `_parser.PushBytes(data)`
  - `OnScreenUpdated()` ([TerminalEmulator.cs](caTTY.Core/Terminal/TerminalEmulator.cs))

**Implication:** Terminal state mutation happens on the process output thread (not necessarily the UI thread). This creates potential race conditions between emulation and rendering.

### Data path (terminal state → ImGui draw)
- `TerminalTestApp` runs per-frame inside the ImGui callback ([TerminalTestApp.cs#L113-L118](caTTY.TestApp/TerminalTestApp.cs#L113-L118)):
  ```csharp
  StandaloneImGui.Run((deltaTime) => 
  {
      _controller.Update(deltaTime);  // Only handles cursor blinking!
      _controller.Render();           // All heavy work happens here
  });
  ```
- `TerminalController.Render()` calls `RenderTerminalCanvas()` → `RenderTerminalContent()`.
- `RenderTerminalContent()` performs heavy per-frame work ([TerminalController.cs#L1848-L1915](caTTY.Display/Controllers/TerminalController.cs#L1848-L1915)):
  1. **Screen buffer copy** - Copies **every row** from `ScreenBuffer` into new `Cell[]` arrays:
     ```csharp
     for (int i = 0; i < activeSession.Terminal.Height; i++)
     {
         var rowSpan = activeSession.Terminal.ScreenBuffer.GetRow(i);
         var rowArray = new Cell[rowSpan.Length];  // ALLOCATION per row per frame!
         rowSpan.CopyTo(rowArray);
         screenBuffer[i] = rowArray.AsMemory();
     }
     ```
  2. **GetViewportRows** - Calls `ScrollbackManager.GetViewportRows(...)` which:
     - Allocates a new `List<ReadOnlyMemory<Cell>>` per call
     - For scrollback rows, allocates new `Cell[]` and copies into it
  3. **Cell loop render** - Iterates every cell and calls `RenderCell()`:
     - Allocates `cell.Character.ToString()` per drawn character
     - Does `ImGui.PushFont/PopFont` per character

### Additional Allocation Sources (Not in Original Analysis)

1. **ScreenBuffer.GetRow() internally allocates** ([ScreenBuffer.cs#L152-L165](caTTY.Core/Types/ScreenBuffer.cs#L152-L165)):
   ```csharp
   public ReadOnlySpan<Cell> GetRow(int row)
   {
       // Create an array for the row data since we can't create spans from 2D arrays directly
       var rowData = new Cell[Width];  // ALLOCATION per GetRow call!
       for (int col = 0; col < Width; col++)
       {
           rowData[col] = _cells[row, col];
       }
       return new ReadOnlySpan<Cell>(rowData);
   }
   ```
   This means even before `RenderTerminalContent` copies the data, `GetRow()` has already allocated.

2. **GameMod has separate direct data path** ([TerminalMod.cs#L188](caTTY.GameMod/TerminalMod.cs#L188)):
   ```csharp
   _processManager.DataReceived += OnProcessDataReceived;
   ```
   Any changes to `TerminalSession` must also be applied to `TerminalMod`.

3. **Double allocation in viewport retrieval** - `ScrollbackManager.GetViewportRows()` allocates for scrollback lines ([ScrollbackManager.cs#L230-L234](caTTY.Core/Managers/ScrollbackManager.cs#L230-L234)):
   ```csharp
   var line = GetLine(globalRow);
   var lineArray = new Cell[line.Length];  // ALLOCATION
   line.CopyTo(lineArray);
   result.Add(lineArray.AsMemory());
   ```

**Implication:** The ImGui render loop is doing a lot of work that is (a) derived from terminal state and (b) allocation-heavy. For an 80x24 terminal, each frame allocates approximately:
- 24 arrays from `GetRow()` (in ScreenBuffer)
- 24 arrays from the copy in `RenderTerminalContent`
- Variable scrollback allocations
- ~1920 string allocations from `ToString()` (worst case: all cells have characters)

---

## How Close Are We To "Paint-Only"?

### Good News
- The controller has an `Update(deltaTime)` method already, which is the right conceptual place to host non-render work.
- `TerminalEmulator` is headless and does not depend on ImGui (good layering).
- `ScreenUpdatedEventArgs.UpdatedRegion` infrastructure exists for dirty tracking ([TerminalEventArgs.cs#L15-L28](caTTY.Core/Terminal/TerminalEventArgs.cs#L15-L28)).
- `ScrollbackManager` already uses `ArrayPool<Cell>.Shared` for its internal storage.

### Gaps vs Ideal

| Gap | Impact | Priority |
|-----|--------|----------|
| Terminal emulation runs concurrently with rendering | Race conditions, forces defensive copying | **High** |
| `ScreenBuffer.GetRow()` allocates internally | Hidden allocation source | **High** |
| Viewport composition happens in `Render()` | Per-frame work | **High** |
| Per-character `PushFont/PopFont` and `ToString()` | Major GC pressure | **High** |
| No render snapshot concept | Forced to rebuild view every frame | **Medium** |
| `UpdatedRegion` exists but isn't used | Missed optimization opportunity | **Medium** |
| GameMod has separate code path | Duplication of fixes needed | **Low** |

**Overall Assessment:** We are **not close** to the paint-only target. The current implementation prioritizes correctness over performance and uses per-frame copying as a safety mechanism against race conditions.

---

## Incremental Improvement Plan (Task List)

Each task is scoped so you can land it independently and keep the app working. Tasks are ordered by dependency and impact.

### Phase 1: Instrumentation & Thread Safety

#### Task 1: Measure Current Per-Frame Costs (Baseline)
- **Goal:** Make the hot paths undeniable and track improvement over time.
- **Files to Edit:**
  - [caTTY.Display/Controllers/TerminalController.cs](caTTY.Display/Controllers/TerminalController.cs)
- **Changes:**
  - Add a `RenderMetrics` class to track timing and allocations
  - Add lightweight timing around `RenderTerminalContent()` phases:
    - Screen buffer copy phase
    - `GetViewportRows` phase
    - Cell render loop phase
  - Add allocation tracking via `GC.GetAllocatedBytesForCurrentThread()` deltas
  - Print summary every N frames (e.g., 120) to avoid console spam
  - Add conditional compilation `#if CATTY_PERF_METRICS` to disable in release
- **Example API:**
  ```csharp
  private class RenderMetrics
  {
      public long ScreenBufferCopyMs;
      public long GetViewportRowsMs;
      public long CellRenderLoopMs;
      public long TotalAllocatedBytes;
      public int FrameCount;
  }
  ```

#### Task 2: Queue Process Output (Thread Safety)
- **Goal:** Make terminal emulation deterministic and keep mutation off the render thread.
- **Files to Edit:**
  - [caTTY.Core/Terminal/TerminalSession.cs](caTTY.Core/Terminal/TerminalSession.cs)
  - [caTTY.GameMod/TerminalMod.cs](caTTY.GameMod/TerminalMod.cs) (apply same pattern)
- **Changes:**
  - Add a `ConcurrentQueue<byte[]>` field and an `ArrayPool<byte>` for efficient buffer reuse
  - Replace `Terminal.Write(e.Data.Span)` in `OnProcessDataReceived` with:
    ```csharp
    private void OnProcessDataReceived(object? sender, DataReceivedEventArgs e)
    {
        // Rent a buffer and copy the data
        var buffer = _bytePool.Rent(e.Data.Length);
        e.Data.Span.CopyTo(buffer);
        _pendingOutput.Enqueue((buffer, e.Data.Length));
    }
    ```
  - Add new public method:
    ```csharp
    /// <summary>
    /// Drains pending process output into the terminal emulator.
    /// Call this from the UI thread before rendering.
    /// </summary>
    /// <param name="maxBytes">Maximum bytes to process (budget)</param>
    /// <returns>Number of bytes actually processed</returns>
    public int DrainPendingProcessOutput(int maxBytes = 256 * 1024)
    ```
  - Ensure proper buffer return to pool after processing

#### Task 3: Pump Terminal Updates in Pre-ImGui Phase
- **Goal:** Move emulation work out of the ImGui "draw UI" callback.
- **Files to Edit:**
  - [caTTY.TestApp/Rendering/StandaloneImGui.cs](caTTY.TestApp/Rendering/StandaloneImGui.cs)
  - [caTTY.Display.Playground/Rendering/StandaloneImGui.cs](caTTY.Display.Playground/Rendering/StandaloneImGui.cs)
  - [caTTY.TestApp/TerminalTestApp.cs](caTTY.TestApp/TerminalTestApp.cs)
- **Changes:**
  - Extend `StandaloneImGui.Run(...)` to accept **two callbacks** (or use a struct):
    ```csharp
    public static void Run(Action<float> onUpdate, Action onDrawUi)
    {
        // ...
        while (!window!.ShouldClose)
        {
            Glfw.PollEvents();
            
            float deltaTime = GetDeltaTime();
            onUpdate(deltaTime);  // Terminal pumping happens here
            
            ImGuiBackend.NewFrame();
            ImGui.NewFrame();
            ImGuiHelper.StartFrame();
            
            onDrawUi();  // Pure rendering
            
            ImGui.Render();
            // ...
        }
    }
    ```
  - Update `TerminalTestApp.RunAsync()`:
    ```csharp
    StandaloneImGui.Run(
        onUpdate: (deltaTime) => {
            _controller.Update(deltaTime);  // Drain output, update cursor, etc.
        },
        onDrawUi: () => {
            _controller.Render();  // Pure painting
        }
    );
    ```

### Phase 2: Eliminate Per-Frame Allocations

#### Task 4: Fix ScreenBuffer.GetRow() Allocation
- **Goal:** Stop `GetRow()` from allocating a new array every call.
- **Files to Edit:**
  - [caTTY.Core/Types/IScreenBuffer.cs](caTTY.Core/Types/IScreenBuffer.cs)
  - [caTTY.Core/Types/ScreenBuffer.cs](caTTY.Core/Types/ScreenBuffer.cs)
  - [caTTY.Core/Types/DualScreenBuffer.cs](caTTY.Core/Types/DualScreenBuffer.cs)
- **Changes:**
  - **Option A (Recommended):** Change internal storage from `Cell[,]` to `Cell[][]` (jagged array):
    ```csharp
    private Cell[][] _rows;  // Instead of Cell[,] _cells
    
    public ReadOnlySpan<Cell> GetRow(int row)
    {
        if (row < 0 || row >= Height) return ReadOnlySpan<Cell>.Empty;
        return _rows[row].AsSpan();  // No allocation!
    }
    ```
  - **Option B (Minimal change):** Add `GetRowMemory(int row)` returning `ReadOnlyMemory<Cell>`:
    ```csharp
    public ReadOnlyMemory<Cell> GetRowMemory(int row);
    ```
    Keep the backing storage but expose stable memory references.
  - Update all callers to use the non-allocating version

#### Task 5: Make Scrollback Viewport Retrieval Non-Allocating
- **Goal:** Eliminate `GetViewportRows()` allocating and copying per call.
- **Files to Edit:**
  - [caTTY.Core/Managers/IScrollbackManager.cs](caTTY.Core/Managers/IScrollbackManager.cs)
  - [caTTY.Core/Managers/ScrollbackManager.cs](caTTY.Core/Managers/ScrollbackManager.cs)
- **Changes:**
  - Add overload that fills a provided list (avoid allocation):
    ```csharp
    void FillViewportRows(
        ReadOnlyMemory<Cell>[] screenBuffer, 
        bool isAlt, 
        int requestedRows, 
        List<ReadOnlyMemory<Cell>> destination);
    ```
  - Scrollback lines are already stored as `Cell[]` arrays - return `ReadOnlyMemory<Cell>` directly without copying:
    ```csharp
    // Instead of:
    var lineArray = new Cell[line.Length];
    line.CopyTo(lineArray);
    result.Add(lineArray.AsMemory());
    
    // Do:
    result.Add(_lines[actualIndex].AsMemory(0, _columns));
    ```
  - Controller reuses a single `List<ReadOnlyMemory<Cell>>` instance across frames

#### Task 6: Move Terminal Work Out of Render()
- **Goal:** Keep `Render()` as paint-only as possible.
- **Files to Edit:**
  - [caTTY.Display/Controllers/TerminalController.cs](caTTY.Display/Controllers/TerminalController.cs)
- **Changes:**
  - Move these operations from `Render()` to `Update()`:
    - Drain pending process output via `session.DrainPendingProcessOutput()`
    - Cursor blink update (already in `Update`)
    - Window resize detection and handling (defer actual resize)
    - Input processing (queue input, apply next frame)
  - Add deferred resize handling:
    ```csharp
    private bool _pendingResize;
    private int _pendingWidth, _pendingHeight;
    
    // In Render(): detect size change, set flag
    // In Update(): if flag set, apply resize
    ```
  - `Render()` should only:
    - Read cached viewport data
    - Issue ImGui draw calls
    - Detect (but not apply) size changes

### Phase 3: Render Snapshot & Batching

#### Task 7: Introduce Render Snapshot
- **Goal:** `RenderTerminalContent()` reads a snapshot prepared during update, not live terminal state.
- **Files to Create/Edit:**
  - **Create:** [caTTY.Display/Types/TerminalRenderSnapshot.cs](caTTY.Display/Types/TerminalRenderSnapshot.cs)
  - **Edit:** [caTTY.Display/Controllers/TerminalController.cs](caTTY.Display/Controllers/TerminalController.cs)
- **TerminalRenderSnapshot Structure:**
  ```csharp
  public class TerminalRenderSnapshot
  {
      // Viewport data (reused list, not reallocated)
      public List<ReadOnlyMemory<Cell>> ViewportRows { get; }
      
      // Cursor state
      public int CursorRow { get; set; }
      public int CursorCol { get; set; }
      public CursorStyle CursorStyle { get; set; }
      public bool CursorVisible { get; set; }
      
      // Terminal state
      public bool IsAlternateScreenActive { get; set; }
      public int ViewportOffset { get; set; }
      public int TerminalWidth { get; set; }
      public int TerminalHeight { get; set; }
      
      // Dirty tracking
      public bool IsDirty { get; set; }
      public HashSet<int> DirtyRows { get; }  // For incremental updates
  }
  ```
- **Changes:**
  - Build snapshot in `Update()` when:
    - Process output was drained (screen changed)
    - Viewport offset changed (scrolling)
    - Resize occurred
  - `RenderTerminalContent()` reads from snapshot only
  - Add `_needsSnapshotRebuild` flag triggered by screen updates

#### Task 8: Batch Text Rendering
- **Goal:** Reduce ImGui draw-call overhead and GC pressure from per-character operations.
- **Files to Edit:**
  - [caTTY.Display/Controllers/TerminalController.cs](caTTY.Display/Controllers/TerminalController.cs)
- **Changes:**
  - Build per-row "runs" during snapshot build (or lazy in render):
    ```csharp
    public struct TextRun
    {
        public int StartCol;
        public int Length;
        public uint ForegroundColor;  // Pre-converted for ImGui
        public uint BackgroundColor;
        public ImFontPtr Font;
        public bool HasUnderline;
        public bool HasStrikethrough;
    }
    ```
  - Group contiguous cells with same font (bold/italic) and same foreground color
  - Render backgrounds separately (can remain per-cell initially)
  - Use pooled `StringBuilder` or `Span<char>` for building run text:
    ```csharp
    // Instead of per-character ToString():
    Span<char> runBuffer = stackalloc char[run.Length];
    for (int i = 0; i < run.Length; i++)
        runBuffer[i] = cells[run.StartCol + i].Character;
    drawList.AddText(pos, run.ForegroundColor, new string(runBuffer));
    ```
  - `PushFont/PopFont` per run, not per character:
    ```csharp
    ImGui.PushFont(run.Font, fontSize);
    drawList.AddText(pos, color, runText);
    ImGui.PopFont();
    ```

#### Task 9: Dirty Row Tracking
- **Goal:** Avoid rebuilding the entire snapshot/run list when only a portion changed.
- **Files to Edit:**
  - [caTTY.Core/Terminal/TerminalEmulator.cs](caTTY.Core/Terminal/TerminalEmulator.cs)
  - [caTTY.Display/Controllers/TerminalController.cs](caTTY.Display/Controllers/TerminalController.cs)
- **Changes:**
  - Ensure `ScreenUpdatedEventArgs.UpdatedRegion` is populated correctly when raising `ScreenUpdated`
  - In controller, track dirty rows via event handler:
    ```csharp
    private void OnScreenUpdated(object? sender, ScreenUpdatedEventArgs e)
    {
        if (e.UpdatedRegion != null)
        {
            for (int row = e.UpdatedRegion.Value.StartRow; row <= e.UpdatedRegion.Value.EndRow; row++)
                _snapshot.DirtyRows.Add(row);
        }
        else
        {
            _snapshot.IsDirty = true;  // Full refresh
        }
    }
    ```
  - During snapshot rebuild, only recompute runs for dirty rows

### Phase 4: Testing & Validation

#### Task 10: Add Architecture Tests
- **Goal:** Ensure the separation (queue → pump → snapshot → render) stays intact.
- **Files to Create/Edit:**
  - [caTTY.Core.Tests/Unit/Terminal/TerminalSessionOutputQueueTests.cs](caTTY.Core.Tests/Unit/Terminal/TerminalSessionOutputQueueTests.cs) (new)
  - [caTTY.Display.Tests/Unit/Controllers/TerminalControllerSnapshotTests.cs](caTTY.Display.Tests/Unit/Controllers/TerminalControllerSnapshotTests.cs) (new)
- **Test Cases for TerminalSession:**
  - Output queue ordering is preserved
  - Drain budget is respected
  - Buffers are returned to pool after drain
  - Concurrent enqueue/drain is safe
- **Test Cases for TerminalController:**
  - Snapshot is rebuilt on screen update
  - Snapshot is rebuilt on scroll
  - Snapshot is rebuilt on resize
  - Render uses snapshot data, not live terminal state
  - Dirty row tracking works correctly

---

## Implementation Priority Matrix

| Task | Impact | Effort | Dependencies | Recommended Order |
|------|--------|--------|--------------|-------------------|
| Task 1 (Metrics) | Low | Low | None | 1st (enables measurement) |
| Task 2 (Queue Output) | High | Medium | None | 2nd |
| Task 3 (Pre-ImGui Pump) | High | Low | Task 2 | 3rd |
| Task 4 (Fix GetRow) | High | Medium | None | 4th |
| Task 5 (Scrollback Non-Alloc) | Medium | Medium | Task 4 | 5th |
| Task 6 (Move Work from Render) | High | Medium | Tasks 2, 3 | 6th |
| Task 7 (Render Snapshot) | High | High | Tasks 4, 5, 6 | 7th |
| Task 8 (Batch Text) | High | High | Task 7 | 8th |
| Task 9 (Dirty Rows) | Medium | Medium | Task 7 | 9th |
| Task 10 (Tests) | Low | Medium | All above | Ongoing |

---

## Summary of Key Architectural Decisions

The biggest wins for "paint-only ImGui" are:

1. **Queue process output bytes** as they arrive (Task 2)
   - Eliminates threading concerns
   - Allows controlled batch processing

2. **Fix the double-allocation in row retrieval** (Tasks 4, 5)
   - `ScreenBuffer.GetRow()` → jagged array backing
   - `ScrollbackManager.GetViewportRows()` → return memory directly

3. **Build a render snapshot once per change** (Task 7)
   - Snapshot built in `Update()`, read in `Render()`
   - No live terminal access during rendering

4. **Batch text rendering** (Task 8)
   - Per-run font push instead of per-character
   - Pooled string building instead of `ToString()` per cell

Expected improvement after all tasks: **60-80% reduction in per-frame allocations**, **significant reduction in GC pressure**, and **cleaner separation of concerns**.

---

## Comparison With Original Analysis

This updated analysis expands on the original `IMGUI_IMPROVEMENTS.md` with:

| Addition | Description |
|----------|-------------|
| **ScreenBuffer.GetRow() allocation** | Identified hidden allocation in the 2D array → span conversion |
| **GameMod code path** | Noted that TerminalMod needs same fixes as TerminalSession |
| **Priority matrix** | Added effort/impact assessment and dependency graph |
| **Phased approach** | Reorganized tasks into logical phases |
| **Code examples** | Added concrete implementation snippets for each task |
| **Metrics class** | Added specific instrumentation guidance |
| **UpdatedRegion usage** | Noted existing infrastructure that's not being used |
| **ArrayPool usage** | Noted ScrollbackManager already uses pooling (partial win) |

---

## Quick Start: First Three Tasks

To begin implementation, start with Tasks 1-3 which provide immediate benefits with low risk:

```bash
# Suggested branch names
git checkout -b feature/imgui-perf-metrics      # Task 1
git checkout -b feature/output-queue            # Task 2  
git checkout -b feature/pre-imgui-pump          # Task 3
```

Each can be merged independently and provides incremental improvement.
