# StatefulTerminal refactor plan (1:1 behavior, smaller files)

## Goals

- Preserve *exact* current behavior and public API of `StatefulTerminal`.
- Reduce the size of any single file by splitting the class into cohesive modules.
- Prefer extracting **stateless functions** (pure w.r.t. module/global state) that operate on an explicit “terminal state” object, leaving `StatefulTerminal` as an orchestrator.
- Keep refactors incremental and testable after each step.

Non-goals (explicitly avoid during this refactor):

- Changing semantics (even if something looks “wrong” or “non-standard”).
- Reformatting unrelated code or renaming exported/public types.
- Introducing new features (mouse reporting, full DCS handling, theme color resolution, etc.).

## Current structure and how it works (high-level)

The file [packages/terminal-emulation/src/terminal/StatefulTerminal.ts](packages/terminal-emulation/src/terminal/StatefulTerminal.ts) contains:

- Data model types:
  - `ScreenCell`, `ScreenBuffer`, `ScreenSnapshot`, `CursorState`, `WindowProperties`.
- A nested helper class:
  - `AlternateScreenManager` with two `ScreenBuffer`s and a current-buffer switch.
- Helper functions/constants:
  - `DEFAULT_SGR_STATE`, `createCellGrid`, `cloneScreenCell`, `cloneScreenRow`, `clampInt`.
- The `StatefulTerminal` class itself:
  - Owns all terminal state (cursor, modes, scroll region, scrollback, SGR state, charset state, window props, etc.).
  - Owns a `Parser` instance and provides its handler callbacks.
  - Maintains listeners (`onUpdate`, `onChunk`, `onResponse`, `onDecMode`) and batching logic to avoid flicker.
  - Implements buffer mutation and rendering semantics (autowrap, scrollback, erase/selective erase, tab stops, etc.).
  - Implements protocol-level handlers:
    - CSI (`handleCsi`): the large switch statement
    - ESC (`handleEsc`)
    - OSC (`handleXtermOsc`)
    - SGR (`handleSgr`) + OSC query color response helpers

### Critical invariants/behavior to preserve

These are the “don’t change even slightly” rules that should guide the refactor:

1. **Update batching semantics**
   - `pushPtyText` wraps parser pushes in `withUpdateBatch`.
   - While `updateBatchDepth > 0`, `requestUpdate()` only marks dirty.
   - When the outermost batch exits, exactly one update is emitted if dirty.

2. **Trace emission semantics**
   - When `traceSettings.enabled === false`, `emitChunk`/`emitControlChunk` are no-ops.
   - When enabled, handler callbacks emit `TerminalTraceChunk` for normal bytes, control bytes, and parsed sequences *before* acting.

3. **Autowrap (`DECAWM`) semantics**
   - Writing a printable character in the last column sets `wrapPending` and does **not** immediately wrap.
   - The next printable character triggers the wrap to col 0, next row (and scroll if needed).
   - Many other actions explicitly clear `wrapPending`.

4. **Origin mode (`DECOM`) semantics**
   - Cursor addressing and clamping depend on `originMode` and the scroll region.
   - Toggling origin mode “homes” the cursor in the current region.

5. **Scrollback semantics**
   - Scrollback is appended **only** when:
     - not in alternate screen, and
     - scroll region is full-screen (`scrollTop === 0` and `scrollBottom === rows - 1`), and
     - a scroll occurs that removes a line.
   - `ED 3` (erase in display, mode 3) clears scrollback.
   - While alternate screen is active, `getViewportRows` returns screen cells only (no primary scrollback).

6. **Cell styling/protection semantics**
   - Every write sets `cell.sgrState = { ...currentSgrState }` (copy).
   - “Selective” erase checks `cell.isProtected !== true`.
   - `makeBlankCellWithCurrentSgr(isProtected)` uses current SGR state and protection.

7. **Character set / UTF-8 mode semantics**
   - If `utf8Mode` is enabled, `translateCharacter` returns input as-is.
   - If disabled and active charset is `"0"` (DEC Special Graphics), ASCII-like bytes map to Unicode line-drawing glyphs.

8. **Response string formats**
   - CPR: `ESC [ row ; col R` (1-based)
   - DA / DSR / terminal size / charset query responses exactly as currently emitted.
   - OSC 10/11 query responses are `ESC ] 10 ; <rgb> BEL` and `ESC ] 11 ; <rgb> BEL`.

9. **Public API compatibility**
   - Preserve exported names and shapes:
     - `StatefulTerminal`, `ScreenSnapshot`, `StatefulTerminalOptions`, etc.
   - Preserve listener registration behavior (`onUpdate`, `onChunk`, etc.).

## Public surface area to keep stable

From [packages/terminal-emulation/src/terminal/StatefulTerminal.ts](packages/terminal-emulation/src/terminal/StatefulTerminal.ts):

- Constructor `new StatefulTerminal(options)`
- `cols`, `rows`, `cursorX`, `cursorY`
- Listener registration: `onUpdate`, `onDecMode`, `onChunk`, `onResponse`
- Snapshot/window/cursor/charset APIs:
  - `getSnapshot`, `setWindowTitle`, `setIconName`, `setTitleAndIcon`
  - `getWindowTitle`, `getIconName`, `getWindowProperties`
  - `getCursorState`, `setCursorVisibility`, `setCursorStyle`
  - `setApplicationCursorKeys`, `getApplicationCursorKeys`
  - `designateCharacterSet`, `getCharacterSet`, `getCurrentCharacterSet`, `switchCharacterSet`
  - `setUtf8Mode`, `isUtf8Mode`
  - `saveCursorState`, `restoreCursorState`
- Scrollback/viewport:
  - `isAlternateScreenActive`, `getScrollbackRowCount`, `getViewportRows`
- Input/reset:
  - `pushPtyText`, `reset`
- SGR:
  - `getCurrentSgrState`, `resetSgrState`

## Proposed target module layout

Keep the existing export point [packages/terminal-emulation/src/terminal/StatefulTerminal.ts](packages/terminal-emulation/src/terminal/StatefulTerminal.ts), but turn it into an orchestrator that delegates to extracted modules.

Recommended new directory:

- `packages/terminal-emulation/src/terminal/stateful/`

Suggested files (balanced: not too many, not too few):

1. `state.ts`
   - Defines `TerminalState` (internal), initialization helpers.
   - Holds what is currently scattered across private fields.

2. `screenTypes.ts`
   - Exports the current public types that belong to the screen model:
     - `ScreenCell`, `ScreenBuffer`, `WindowProperties`, `CursorState`, `ScreenSnapshot`.
   - Keep these exports re-exported from `StatefulTerminal.ts` to avoid breaking imports.

3. `screenGrid.ts`
   - `DEFAULT_SGR_STATE`, `createCellGrid`, `cloneScreenCell`, `cloneScreenRow`.

4. `alternateScreen.ts`
   - Moves `AlternateScreenManager` and its logic out of the main file.

5. `cursor.ts`
   - Cursor movement helpers that depend on modes:
     - `clampCursor`, `mapRowParamToCursorY`, `setOriginMode`, `setAutoWrapMode`.

6. `tabStops.ts`
   - Tab stop state + operations:
     - initialize, set/clear at cursor, clear all, forward/backward.

7. `bufferOps.ts`
   - Pure-ish operations that mutate the grid given explicit state:
     - `putChar`, `insertCharsInLine`, `deleteCharsInLine`
     - `eraseCharacters`, `clearLine`, `clearDisplay`
     - `scrollUp`, `scrollUpInRegion`, `scrollDownInRegion`
     - `deleteLinesInRegion`, `insertLinesInRegion`
   - These functions should accept a minimal context object (see below).

8. `handlers/csi.ts`
   - The CSI dispatch (currently `handleCsi`) moved into a table-based dispatcher.

9. `handlers/esc.ts`
   - ESC handling moved out.

10. `handlers/osc.ts`
   - Xterm OSC handling + query responses.

11. `responses.ts`
   - Device query response generation + OSC color conversion helpers.

## The key refactor technique: explicit internal “TerminalState” + “TerminalContext”

To extract stateless functions cleanly (and avoid TypeScript `private` access issues), introduce an internal state object and pass it to helpers.

### `TerminalState` (internal)

Make a single object storing everything that is currently many private fields:

- Cursor: `cursorX`, `cursorY`, `savedCursor`, `wrapPending`, `cursorStyle`, `cursorVisible`
- Modes: `originMode`, `autoWrapMode`, `applicationCursorKeys`, `utf8Mode`
- Region/tab/window: `scrollTop`, `scrollBottom`, `tabStops`, `windowProperties`, `titleStack`, `iconNameStack`
- Charset: `characterSets`
- Styling: `currentSgrState`, `currentCharacterProtection`
- Buffers: `alternateScreenManager`, `scrollback`
- Update batching: `updateBatchDepth`, `updateDirty`

`StatefulTerminal` keeps `cols`/`rows`, `parser`, and listeners; it owns a single `private state: TerminalState`.

### `TerminalContext` (internal)

For operations that need access to non-state dependencies, pass a second “context” object:

- `cols`, `rows`
- `log`
- `emitResponse`, `emitDecMode` (or return events to be emitted)

This avoids helpers importing logger or reaching into the class.

## Incremental refactor steps (actionable sequence)

Each step below should keep functionality 1:1 and be small enough to review.

### Step 0 — Baseline verification and guardrails

Status: **DONE** (implemented, 2025-12-19)

- Verified baseline tests: `pnpm run test` (root) passes.
- Verified baseline compilation: `pnpm run tsc` (root) passes.
- Guardrails already present in existing test suite (no new test added in this step):
  - `packages/terminal-emulation/src/__tests__/DecPrivateModes.test.ts`
  - `packages/terminal-emulation/src/__tests__/TabStopControls.test.ts`
  - `packages/terminal-emulation/src/__tests__/Scrollback.test.ts`
  - `packages/terminal-emulation/src/__tests__/AlternateScreen.test.ts`
  - `packages/terminal-emulation/src/__tests__/StatefulTerminal.cursor.property.test.ts`

- Run tests before any changes:
  - `pnpm test`
- (Optional but recommended) add a “refactor safety” snapshot test that:
  - Feeds a representative stream of bytes and asserts snapshot invariants.
  - Only if there isn’t already sufficient coverage.

Acceptance: baseline tests are green and you have a known-good starting point.

### Step 1 — Extract and re-export public types (no behavior change)

Status: **DONE** (implemented, 2025-12-19)

- Create `packages/terminal-emulation/src/terminal/stateful/screenTypes.ts` and move:
  - `ScreenCell`, `WindowProperties`, `ScreenBuffer`, `CursorState`, `ScreenSnapshot`, `DecModeEvent`, `StatefulTerminalOptions` (or keep options where it’s used).
- Update `StatefulTerminal.ts` to import these types and re-export as needed.

Why first: it shrinks the top of the file and stabilizes shared types for other modules.

Acceptance:
- TypeScript compiles.
- `pnpm -C packages/terminal-emulation test` passes.

### Step 2 — Extract `AlternateScreenManager` into its own file

Status: **DONE** (implemented, 2025-12-19)

- Create `packages/terminal-emulation/src/terminal/stateful/alternateScreen.ts`.
- Move `AlternateScreenManager` and the `ScreenBuffer` initialization logic.
- Keep constructor signature identical (cols/rows) to minimize diffs.

Acceptance:
- All tests pass.
- No behavioral changes in alternate-screen tests (e.g., `AlternateScreen.test.ts`).

### Step 3 — Extract grid helpers (`createCellGrid`, cloning, `DEFAULT_SGR_STATE`)

Status: **DONE** (implemented, 2025-12-19)

- Create `packages/terminal-emulation/src/terminal/stateful/screenGrid.ts`.
- Move:
  - `DEFAULT_SGR_STATE` (still `Object.freeze(createDefaultSgrState())`)
  - `createCellGrid`, `cloneScreenCell`, `cloneScreenRow`
- Ensure the note about not mutating `DEFAULT_SGR_STATE` remains true (and visible).

Acceptance:
- No change in scrollback cloning behavior.

### Step 4 — Introduce internal `TerminalState` while keeping method bodies intact

Status: **DONE** (implemented, 2025-12-19)

This is the most “mechanical” step, but it unlocks the rest.

- Create `packages/terminal-emulation/src/terminal/stateful/state.ts`:
  - Define `TerminalState` interface/type.
  - Define `createInitialTerminalState({ cols, rows, scrollbackLimit, alternateScreenManager, ... })`.
- In `StatefulTerminal.ts`:
  - Replace the many private fields with `private state: TerminalState`.
  - Route former fields through `TerminalState` via accessors to keep method bodies stable.
  - Keep all methods in the same file for now.

Guidance:
- Do this as a series of small commits or “find/replace” sets.
- Avoid renaming semantics: keep property names as close as possible.

Acceptance:
- Tests pass.
- Runtime behavior matches.
- `pnpm run tsc` passes.

### Step 5 — Extract tab stops into a dedicated module

- Create `packages/terminal-emulation/src/terminal/stateful/tabStops.ts`.
- Move:
  - `initializeTabStops`, `setTabStopAtCursor`, `clearTabStopAtCursor`, `clearAllTabStops`, `cursorForwardTab`, `cursorBackwardTab`.
- Convert these from methods into functions:
  - Inputs: `{ cols, cursorX, tabStops }` and parameters.
  - Output: updated `{ cursorX, tabStops, wrapPending }` or direct mutation of `TerminalState`.

Recommendation (to keep diffs small):
- Use mutation of `TerminalState` initially.
- Later, consider returning a new state subset if you want more purity.

Acceptance:
- Tab-related tests (and any parser integration tests) pass.

### Step 6 — Extract character set + translation logic

- Create `packages/terminal-emulation/src/terminal/stateful/charset.ts`.
- Move:
  - `designateCharacterSet`, `getCharacterSet`, `getCurrentCharacterSet`, `switchCharacterSet`
  - `setUtf8Mode`, `isUtf8Mode`
  - `translateCharacter` and the DEC Special Graphics map
  - `generateCharacterSetQueryResponse`

Keep note:
- The DEC Special Graphics lookup table can be a module constant (no behavior change).

Acceptance:
- Character set query behavior unchanged.

### Step 7 — Extract scrollback + viewport logic

- Create `packages/terminal-emulation/src/terminal/stateful/scrollback.ts`.
- Move:
  - `pushScrollbackRow`, `clearScrollback`, `getViewportRows`

Key requirement:
- Preserve the alternate-screen special-case in `getViewportRows`.

Acceptance:
- Scrollback tests pass (including ED3 clearing).

### Step 8 — Extract buffer mutation ops (screen editing primitives)

- Create `packages/terminal-emulation/src/terminal/stateful/bufferOps.ts`.
- Move functions that are semantically “screen editing primitives”:
  - `makeBlankCellWithCurrentSgr`
  - `insertCharsInLine`, `deleteCharsInLine`, `putChar`
  - `carriageReturn`, `lineFeed`, `backspace`, `tab` (tab might remain with tabStops)
  - `clear`, `scrollUp`
  - `clearLine`, `clearLineSelective`, `eraseCharacters`
  - `setScrollRegion`
  - `scrollUpInRegion`, `scrollDownInRegion`
  - `deleteLinesInRegion`, `insertLinesInRegion`
  - `clearDisplay`, `clearDisplaySelective`

Design suggestion:
- Define a minimal “ops context” type for these functions:
  - `cols`, `rows`
  - `cells` getter (current buffer)
  - `isAlternateScreenActive()`
  - `pushScrollbackRow` callback
  - `currentSgrState`, `currentCharacterProtection`
- This keeps the ops independent from the `StatefulTerminal` class.

Acceptance:
- Cursor positioning / scroll region / erase tests pass.

### Step 9 — Extract device query responses + OSC color conversion

- Create `packages/terminal-emulation/src/terminal/stateful/responses.ts`.
- Move:
  - `generateDeviceAttributesPrimaryResponse`
  - `generateDeviceAttributesSecondaryResponse`
  - `generateCursorPositionReport`
  - `generateDeviceStatusReportResponse`
  - `generateTerminalSizeResponse`
  - `generateForegroundColorResponse`, `generateBackgroundColorResponse`
  - `getEffectiveForegroundColor`, `getEffectiveBackgroundColor`
  - `convertSgrColorToOscFormat`, `convertIndexedColorToOscFormat`, `convertNamedColorToOscFormat`

Note:
- Keep the exact fallback strings (`rgb:aaaa/aaaa/aaaa`, `rgb:0000/0000/0000`).

Acceptance:
- OSC color query tests continue to pass.

### Step 10 — Extract protocol handlers (CSI / ESC / OSC)

This step makes the biggest readability win, since `handleCsi` is the largest “blob”.

#### 10a — Create a narrow `TerminalActions` interface

To avoid helpers reaching into class internals, define an internal interface implemented by `StatefulTerminal` (or backed by `TerminalState`):

- Cursor/mode actions:
  - `clampCursor()`, `setOriginMode(enable)`, `setAutoWrapMode(enable)`
- Buffer ops:
  - `clearLine(mode)`, `clearDisplay(mode)`, `insertCharsInLine(n)`, etc.
- Response emission:
  - `emitResponse(str)`
  - `emitDecMode(...)`

Then `handlers/csi.ts` can be a pure dispatcher:

- Input: `TerminalActions`, `CsiMessage`
- Output: void

#### 10b — Move CSI switch to `handlers/csi.ts`

- Replace the giant switch with:
  - a `const csiHandlers: Record<CsiMessage['_type'], (actions, msg) => void>`
  - or a `switch` in the new file if you prefer minimal code churn.

#### 10c — Move ESC and OSC similarly

- `handlers/esc.ts`: `handleEsc(actions, msg)`
- `handlers/osc.ts`: `handleXtermOsc(actions, msg)`

Acceptance:
- Integration tests that cover CSI/ESC/OSC behavior remain green.

### Step 11 — Cleanup pass: reduce `StatefulTerminal.ts` to orchestration

After extractions, `StatefulTerminal.ts` should mainly:

- Define `StatefulTerminal` public API.
- Own:
  - `cols`, `rows`
  - the `Parser` and its wiring
  - listener sets + batching
  - internal `TerminalState`
- Delegate:
  - “do work” functions to extracted modules.

At this point, the file should be substantially smaller and maintainable.

Acceptance:
- `pnpm test` passes.

## Suggested “commit sizing” (practical advice)

To keep the refactor safe:

- One extraction per commit (types, alt screen, grid, state object, etc.).
- Run `pnpm -C packages/terminal-emulation test` after each commit.
- Only run full `pnpm test` at milestones (Step 4, Step 8, Step 11).

## Risk areas and how to avoid accidental behavior changes

- **Accidental deep-copy vs shallow-copy changes**
  - The code frequently does `{ ...this.currentSgrState }`.
  - Ensure extracted helpers keep copying the same way.

- **Mutability of `cells` and snapshots**
  - `getSnapshot().cells` currently returns `this.cells` (live backing array).
  - Do not change this unless you also update all consumers and tests (out of scope).

- **Off-by-one in row/col conversions**
  - CPR, cursor position, and scroll region use 1-based inputs.
  - Keep conversions exactly.

- **Scrollback only for primary+full-screen region**
  - Preserve the condition in `scrollUp`.

- **Update batching**
  - Keep `requestUpdate()` and `withUpdateBatch()` logic in the class (or an extracted helper that’s still used identically).

## Optional follow-ups (only after the refactor is stable)

These are intentionally not part of the refactor, but become easier afterward:

- Add a small, dedicated test suite for `handlers/csi.ts` mapping.
- Add property tests for “state equivalence” across random byte streams.
- Consider separating “model” vs “view” snapshot generation if you later want immutable snapshots.
