# Plan: Simple Terminal Emulator (80x24)

Build a minimum viable terminal emulator using libghostty-vt for sequence encoding/parsing and custom MVC architecture for terminal state and I/O.

## Steps

1. **Create Terminal Model Foundation** - Define `TerminalCell.cs` with char + SGR attributes (foreground/background colors, bold, italic, underline), `TerminalState.cs` with 80x24 screen buffer array, cursor position (row, col), and current text attributes. Include methods to get/set cells and move cursor.

2. **Implement CSI Sequence Handlers** - Create `CsiHandler.cs` with methods for cursor movement (CUU/CUD/CUF/CUB/CUP), erase functions (ED for display, EL for line), and character insertion/deletion. Each handler updates `TerminalState` based on parsed parameters.

3. **Build ANSI Parser State Machine** - Implement `AnsiParser.cs` following DEC ANSI parser states (ground, escape, csi_entry, csi_param, csi_intermediate, osc_string) from `dec-parser.md`. Parse incoming bytes, collect parameters, and dispatch to appropriate handlers (CSI, OSC, SGR) or print characters.

4. **Integrate OSC Parser** - Create `OscHandler.cs` using `GhosttyOsc` wrapper with `SafeHandle` pattern from `OscDemo.cs`. Handle window title changes (store in state), clipboard operations, and color operations. Reset parser between sequences.

5. **Integrate SGR Parser** - Create `SgrHandler.cs` using `GhosttySgr` wrapper with `SafeHandle` pattern from `SgrDemo.cs`. Parse SGR parameters to update current text attributes in `TerminalState`. Support colors (8/256/RGB), bold, italic, underline styles, and attribute resets.

6. **Implement Console View Renderer** - Create `ConsoleView.cs` that reads `TerminalState` and renders 80x24 grid to stdout. Apply ANSI SGR sequences to display colors/styles. Position cursor with Console.SetCursorPosition. Implement full screen redraw for MVP.

7. **Build Input Controller** - Create `InputController.cs` that reads from stdin using Console.ReadKey. Convert `ConsoleKeyInfo` to `GhosttyKeyEvent` structures. Use `GhosttyKey` encoder with `SafeHandle` pattern from `KeyDemo.cs` to generate escape sequences. Configure Kitty protocol flags.

8. **Create Output Controller** - Build `OutputController.cs` that receives output bytes (from PTY/process), feeds them to `AnsiParser`, which dispatches to OSC/SGR/CSI handlers, updating `TerminalState`. Trigger view refresh after state changes.

9. **Implement Simple Process I/O** - Create `ProcessManager.cs` that spawns a shell (bash/cmd) with stdin/stdout redirected. For MVP, use basic Process class without full PTY. Send encoded key sequences to process stdin, read output bytes asynchronously from stdout.

10. **Wire MVC Components** - Create `TerminalEmulator.cs` that instantiates Model (`TerminalState`), View (`ConsoleView`), Controllers (`InputController`, `OutputController`, `ProcessManager`). Implement main loop: read input → encode → send to process → read output → parse → update state → render view.

11. **Update Program Entry Point** - Modify `Program.cs` to add `--terminal` option that creates and runs `TerminalEmulator`. Initialize console (disable echo, raw mode if possible), run emulator loop, restore console on exit.

12. **Add Error Handling and Cleanup** - Implement proper disposal of all `SafeHandle` instances (key encoder, OSC parser, SGR parser). Handle process termination gracefully. Add try/catch for parser errors with fallback to safe states. Ensure console restoration in finally blocks.

## Further Considerations

1. **PTY vs Basic Process I/O** - MVP uses `System.Diagnostics.Process` with redirected streams. For full terminal functionality (cursor positioning, screen size), consider PtySharp or native PTY libraries in future iterations.

2. **Performance Optimization** - Current plan uses full screen redraw. Consider dirty rectangle tracking or diff-based rendering for smoother updates when moving beyond MVP.

3. **Scrollback Buffer** - Not included in MVP. To add: expand buffer beyond 24 lines, implement scroll up/down handlers (SU/SD CSI sequences), add view offset tracking.

4. **Alternative Character Sets (G0/G1/G2/G3)** - Not implemented in MVP. If needed, add character set state and translation tables following VT100 spec.

5. **Mouse Support** - libghostty-vt has mouse shape OSC support. For click/drag events, would need to capture Console mouse events and encode per Xterm mouse protocol (SGR mode 1006 recommended).
