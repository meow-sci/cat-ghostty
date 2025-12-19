# Conversion Analysis: TypeScript to C# (ImGui)

## Executive Summary

The `terminal-emulation` package is well-architected for portability. The core logic (`StatefulTerminal`, `Parser`, `SgrStateProcessor`) is "headless" and free of DOM dependencies, making it a prime candidate for direct translation to C#.

The primary challenge lies in the `TerminalController` (the "glue" layer) and the styling system (`SgrStyleManager`, `ThemeManager`), which are tightly coupled to the DOM. However, significant chunks of logic within `TerminalController` (input encoding, scrolling math) are pure functions that can be extracted and reused.

## Architecture Analysis

### 1. Core Emulation (Keep/Translate)
The following components are clean and should be translated directly to C#:
- **`Parser.ts`**: Pure state machine. No dependencies.
- **`StatefulTerminal.ts`**: The main entry point. It manages state and delegates to helpers. It has no DOM dependencies.
- **`SgrStateProcessor.ts`**: Pure logic for state transitions.
- **`./stateful/*`**: All helper modules (cursor, bufferOps, etc.) appear to be pure logic.
- **`TerminalEmulationTypes.ts`**: Shared type definitions.

### 2. Styling & DOM (Discard/Reimplement)
The following components are specific to the web/DOM and should be **ignored** or reimplemented using ImGui concepts:
- **`DomStyleManager.ts`**: Manages `<style>` tags. Irrelevant for ImGui.
- **`SgrStyleManager.ts`**: Manages CSS classes. For ImGui, you will render text directly using the `SgrState` properties (color, bold, etc.) without intermediate CSS classes.
- **`TerminalTheme.ts`**: Manages CSS variables. In C#, this should be a simple `struct` or `class` holding color values (e.g., `ImVec4`).

### 3. Controller Glue (Refactor & Reuse)
`TerminalController.ts` contains a mix of UI event handling (DOM-specific) and logic for encoding inputs (universal). The universal parts should be extracted before or during the port.

## Refactoring Opportunities

To facilitate the port, the following logic should be extracted from `TerminalController.ts` into the `terminal-emulation` package as pure helper classes. This allows the C# port to simply "call the library" rather than rewriting complex encoding logic.

### A. Input Encoding (`InputEncoder`)
The logic to convert keyboard and mouse events into ANSI escape sequences is currently embedded in the controller.
- **Mouse Encoding**: `encodeMousePress`, `encodeMouseRelease`, `encodeMouseMotion`, `encodeMouseWheel`.
- **Key Encoding**: `encodeCtrlKey`, `xtermModifierParam`, and the switch statement handling Arrow/Function keys.

**Recommendation**: Create `packages/terminal-emulation/src/input/InputEncoder.ts` (and C# equivalent) to handle this.

### B. Mouse Mode Resolution
The logic to determine which mouse mode is active (1003 > 1002 > 1000) based on the set of enabled DEC modes is currently in `TerminalController`.
- **Recommendation**: Move this to `StatefulTerminal` or a helper. The terminal state should know its "effective" mouse reporting mode.

### C. Scroll Logic
The math to convert pixel/line deltas into scroll lines or arrow key sequences (`wheelScrollLinesFromDelta`, `altScreenWheelSequenceFromDelta`) is pure logic.
- **Recommendation**: Extract to `packages/terminal-emulation/src/input/ScrollLogic.ts`.

## Porting Strategy

1.  **Translate the Core**: Port `Parser`, `StatefulTerminal`, and `stateful/*` folder first. These are the hardest to get right but the easiest to translate line-by-line.
2.  **Mock the UI**: Create a minimal C# console app that feeds static strings to the `Parser` and inspects the `StatefulTerminal` state to verify correctness before hooking up ImGui.
3.  **Implement Input**: Port the extracted `InputEncoder` logic.
4.  **Build ImGui Controller**: Create the C# equivalent of `TerminalController`. Instead of managing DOM elements, it will:
    -   Iterate the `StatefulTerminal` grid.
    -   Submit `ImGui.TextColored` (or custom draw list commands) for each cell based on its `SgrState`.
    -   Pass `ImGui.GetIO().InputQueueCharacters` to the `InputEncoder` -> `Parser`.

## Potential Pitfalls

- **UTF-8 Handling**: The TypeScript parser uses `String.fromCharCode` and JS strings (UTF-16). C# strings are also UTF-16, but if you are dealing with raw byte streams from a PTY in C#, ensure you handle UTF-8 decoding correctly before or within the parser. The current `Parser.ts` has some `utf8Buffer` logic which should be preserved.
- **Performance**: The TypeScript version uses a "smart diff" for DOM updates. ImGui is immediate mode, so you will redraw the entire visible grid every frame. This is generally fast enough, but for large grids, you may need to optimize the drawing loop (e.g., batching same-colored cells).
