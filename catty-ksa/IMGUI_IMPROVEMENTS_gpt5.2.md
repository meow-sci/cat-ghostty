# ImGui Render-Loop Optimization Analysis (caTTY)

## Goal / Ideal Architecture
We want the ImGui frame to be **paint-only**:
- ImGui render code should **only read** a stable, already-prepared terminal “frame” (cells, colors, cursor, selection overlays) and emit draw calls.
- Terminal emulation (UTF-8 decode, escape parsing, stateful buffer mutation) should happen **outside** the ImGui render loop.
- Any expensive “derived view” work (viewport composition, scrollback merging, layout/run building) should happen **on change**, not every frame.

In practice, this usually means:
- A **terminal update pump** that applies process output to the emulator on a controlled thread/tick.
- A **render snapshot** (immutable-ish) produced after updates, read by ImGui.
- A **dirty/invalidations** mechanism so we rebuild only what changed.

## Current Architecture (What Actually Happens)

### Data path (process output → terminal state)
- `ProcessManager` reads ConPTY output asynchronously and raises `DataReceived`.
- `TerminalSession` subscribes to `ProcessManager.DataReceived` and immediately forwards bytes to the emulator:
  - `TerminalSession.OnProcessDataReceived` → `Terminal.Write(e.Data.Span)` ([caTTY.Core/Terminal/TerminalSession.cs](caTTY.Core/Terminal/TerminalSession.cs))
- `TerminalEmulator.Write(ReadOnlySpan<byte>)` does parsing/state updates immediately:
  - `_parser.PushBytes(data)`
  - `OnScreenUpdated()` ([caTTY.Core/Terminal/TerminalEmulator.cs](caTTY.Core/Terminal/TerminalEmulator.cs))

**Implication:** terminal state mutation happens on the process output thread (not necessarily the UI thread).

### Data path (terminal state → ImGui draw)
- `TerminalTestApp` runs per-frame:
  - `_controller.Update(deltaTime)`
  - `_controller.Render()`
  inside the ImGui frame callback ([caTTY.TestApp/TerminalTestApp.cs](caTTY.TestApp/TerminalTestApp.cs))

- `TerminalController.Render()` calls `RenderTerminalCanvas()` → `RenderTerminalContent()`.
- `RenderTerminalContent()` performs heavy per-frame work:
  - Copies **every row** from `ScreenBuffer` into new `Cell[]` arrays (per frame)
  - Calls `ScrollbackManager.GetViewportRows(...)` which:
    - Allocates a `List<ReadOnlyMemory<Cell>>`
    - For scrollback rows, allocates new `Cell[]` and copies into it (per call)
  - Iterates every cell and calls `RenderCell()`
    - Allocates `cell.Character.ToString()` per drawn character
    - Does `ImGui.PushFont/PopFont` per character

See [caTTY.Display/Controllers/TerminalController.cs](caTTY.Display/Controllers/TerminalController.cs) (`RenderTerminalContent`, `RenderCell`).

**Implication:** the ImGui render loop is doing a lot of work that is (a) derived from terminal state and (b) allocation-heavy.

## How Close Are We To “Paint-Only”?

### Good news
- The controller has an `Update(deltaTime)` already, which is the right conceptual place to host non-render work.
- `TerminalEmulator` is headless and does not depend on ImGui (good layering).

### Gaps vs ideal
1. **Terminal emulation runs concurrently with rendering**
   - Because `TerminalSession.OnProcessDataReceived` calls `Terminal.Write` on the output pump thread.
   - This forces the display layer to defensively copy terminal buffers to avoid tearing/races.

2. **Viewport composition and buffer copying happen inside `RenderTerminalContent()`**
   - Every ImGui frame performs full-screen copies and allocations.
   - `ScrollbackManager.GetViewportRows()` currently copies scrollback lines into fresh arrays.

3. **Rendering is not batched**
   - Per-character `PushFont/PopFont` and `ToString()` are extremely expensive and dominate per-frame CPU/GC.

Overall: **we are not very close** to the paint-only target; the current implementation is correct-functionality-first and uses per-frame copying as a safety mechanism.

---

## Incremental Improvement Plan (Task List)

Each task is scoped so you can land it independently and keep the app working.

### 1) Measure the current per-frame costs (baseline)
- **Goal:** make the hot paths undeniable and track improvement.
- **Edit:** [caTTY.Display/Controllers/TerminalController.cs](caTTY.Display/Controllers/TerminalController.cs)
- **Change:**
  - Add lightweight timing around `RenderTerminalContent()` and its inner phases:
    - screenBuffer copy
    - `GetViewportRows`
    - cell loop render
  - Add allocation tracking via `GC.GetAllocatedBytesForCurrentThread()` deltas.
  - Print summary every N frames (e.g., 120) to avoid spam.

### 2) Stop mutating the terminal from the process output thread (queue output)
- **Goal:** make terminal emulation deterministic and keep mutation off the render thread.
- **Edit:** [caTTY.Core/Terminal/TerminalSession.cs](caTTY.Core/Terminal/TerminalSession.cs)
- **Change:**
  - Replace `Terminal.Write(e.Data.Span)` in `OnProcessDataReceived` with:
    - Copy data into an owned buffer (e.g., `byte[]` via pooling) and enqueue into a `ConcurrentQueue<byte[]>`.
  - Add a new method (example API):
    - `public int DrainPendingProcessOutput(int maxBytes = 256 * 1024)`
      - Dequeues queued chunks up to a budget and calls `Terminal.Write(...)`.
      - Returns bytes processed (useful for instrumentation).

### 3) Pump terminal updates in a pre-ImGui update phase (not inside painting)
- **Goal:** move emulation work out of the ImGui “draw UI” callback.
- **Edits:**
  - [caTTY.TestApp/Rendering/StandaloneImGui.cs](caTTY.TestApp/Rendering/StandaloneImGui.cs)
  - [caTTY.Display.Playground/Rendering/StandaloneImGui.cs](caTTY.Display.Playground/Rendering/StandaloneImGui.cs)
  - [caTTY.TestApp/TerminalTestApp.cs](caTTY.TestApp/TerminalTestApp.cs)
- **Change (recommended):**
  - Extend `StandaloneImGui.Run(...)` to accept **two callbacks**:
    - `onUpdate(deltaTime)` called after `Glfw.PollEvents()` but **before** `ImGui.NewFrame()`.
    - `onDrawUi()` called between `ImGui.NewFrame()` and `ImGui.Render()`.
  - Call `_controller.Update(deltaTime)` and session output draining from `onUpdate`.

### 4) Move “terminal-related work” out of `TerminalController.Render()`
- **Goal:** keep `Render()` as paint-only as possible.
- **Edit:** [caTTY.Display/Controllers/TerminalController.cs](caTTY.Display/Controllers/TerminalController.cs)
- **Change:**
  - Add a new method (example): `public void PumpSessions(float deltaTime)`
    - For all sessions (or at least active session): call `session.DrainPendingProcessOutput()`.
    - Run cursor blink update (currently in `Update`).
  - `Render()` should not trigger terminal mutation, parsing, or viewport assembly.

### 5) Introduce a stable render snapshot (stop per-frame copying)
- **Goal:** `RenderTerminalContent()` reads a snapshot prepared during update.
- **Edits:**
  - Add new type: [caTTY.Display/Types/TerminalRenderSnapshot.cs](caTTY.Display/Types/TerminalRenderSnapshot.cs) (new file)
  - Update: [caTTY.Display/Controllers/TerminalController.cs](caTTY.Display/Controllers/TerminalController.cs)
- **Change:**
  - Snapshot should include:
    - `ReadOnlyMemory<Cell>[] ViewportRows` for the currently visible rows
    - Cursor position + style + visibility
    - Flags for alt-screen, scrollback offset, etc.
  - Rebuild snapshot only when:
    - process output was drained (screen changed)
    - viewport offset changed (scrolling)
    - resize occurred
  - `RenderTerminalContent()` uses the cached snapshot and must not allocate `Cell[]` per row.

### 6) Make scrollback viewport retrieval non-allocating / cacheable
- **Goal:** eliminate `ScrollbackManager.GetViewportRows(...)` allocating and copying per call.
- **Edits:**
  - [caTTY.Core/Managers/IScrollbackManager.cs](caTTY.Core/Managers/IScrollbackManager.cs)
  - [caTTY.Core/Managers/ScrollbackManager.cs](caTTY.Core/Managers/ScrollbackManager.cs)
- **Change options (pick one):**
  - **Option A (minimal API change):** add overload that fills a provided list:
    - `void GetViewportRows(ReadOnlyMemory<Cell>[] screenBuffer, bool isAlt, int requestedRows, List<ReadOnlyMemory<Cell>> destination)`
    - Reuse the list instance across frames (owned by controller/snapshot builder).
  - **Option B (better long-term):** store scrollback lines as stable `Cell[]` arrays so you can return `ReadOnlyMemory<Cell>` without copying.

### 7) Provide a non-copying way to read screen rows
- **Goal:** stop doing this in render:
  - `GetRow(i)` → copy to new `Cell[]`.
- **Edits (likely):**
  - [caTTY.Core/Managers/IScreenBuffer.cs](caTTY.Core/Managers/IScreenBuffer.cs) (or wherever `GetRow` lives)
  - Implementation in screen buffer classes
- **Change:**
  - Add `ReadOnlyMemory<Cell> GetRowMemory(int row)` (or equivalent) returning stable backing memory.
  - Snapshot builder uses `GetRowMemory` instead of allocating per row.

### 8) Batch text rendering (remove per-character font pushes and string allocations)
- **Goal:** reduce ImGui draw-call overhead and GC pressure.
- **Edit:** [caTTY.Display/Controllers/TerminalController.cs](caTTY.Display/Controllers/TerminalController.cs)
- **Change:**
  - Build per-row “runs” during snapshot build:
    - group contiguous cells with same font (bold/italic) and same foreground color
  - Render backgrounds (rects) separately (can remain per-cell initially).
  - Render text via `drawList.AddText(...)` per run:
    - Avoid `cell.Character.ToString()`; build a pooled `StringBuilder` per run or use stack buffers if possible.
  - Avoid `ImGui.PushFont/PopFont` per character:
    - push per run, draw run, pop.

### 9) Use `ScreenUpdatedEventArgs.UpdatedRegion` to do dirty-row updates
- **Goal:** avoid rebuilding the entire snapshot/run list when only a portion changed.
- **Edits:**
  - [caTTY.Core/Terminal/TerminalEventArgs.cs](caTTY.Core/Terminal/TerminalEventArgs.cs)
  - [caTTY.Core/Terminal/TerminalEmulator.cs](caTTY.Core/Terminal/TerminalEmulator.cs) (where region info is raised)
  - [caTTY.Display/Controllers/TerminalController.cs](caTTY.Display/Controllers/TerminalController.cs)
- **Change:**
  - Track dirty rows in the snapshot builder.
  - Recompute runs only for dirty rows.

### 10) Tests to lock in the architecture
- **Goal:** ensure the separation (queue → pump → snapshot → render) stays intact.
- **Edits:**
  - Add new tests in [caTTY.Core.Tests](caTTY.Core.Tests) for:
    - `TerminalSession` output queue + drain ordering
    - “drain budget” behavior
  - Add display-layer unit tests in [caTTY.Display.Tests](caTTY.Display.Tests) for:
    - snapshot rebuild triggers on resize/scroll/output

---

## Summary of Key Architectural Decision
The biggest win for “paint-only ImGui” is:
- **Queue process output bytes** as they arrive.
- **Apply them to `TerminalEmulator` in a controlled update phase** (ideally before `ImGui.NewFrame`).
- **Build a render snapshot once per change**, and have ImGui render read that snapshot without copying.

If you want, I can follow up by implementing Tasks 1–3 (instrumentation + output queue + pre-ImGui update hook) as the first concrete step.
