# Terminal Emulator

A simple, minimum viable terminal emulator implemented in C# using libghostty-vt.

## Architecture

This implementation follows a clean MVC (Model-View-Controller) architecture:

### Model
- **TerminalCell**: Represents a single cell with character and SGR attributes (colors, bold, italic, underline, etc.)
- **TerminalState**: Maintains an 80x24 screen buffer, cursor position, and current text attributes

### View
- **ConsoleView**: Renders the terminal state to console stdout using ANSI escape sequences

### Controllers
- **InputController**: Captures keyboard input and encodes it using libghostty-vt's key encoder with Kitty protocol
- **OutputController**: Processes output bytes through the ANSI parser and updates terminal state
- **ProcessManager**: Manages the child shell process with redirected stdin/stdout

### Parsers and Handlers
- **AnsiParser**: State machine implementing DEC ANSI parser for escape sequences
- **CsiHandler**: Handles CSI (Control Sequence Introducer) sequences for cursor movement and display clearing
- **OscHandler**: Handles OSC (Operating System Command) sequences using libghostty-vt
- **SgrHandler**: Handles SGR (Select Graphic Rendition) sequences for text styling using libghostty-vt

## Features

- **Hard-coded terminal size**: 80 columns × 24 rows
- **Key encoding**: Full keyboard support with Kitty protocol via libghostty-vt
- **ANSI sequence parsing**: Complete DEC ANSI parser state machine
- **Text attributes**: Support for colors (8/256/RGB), bold, italic, underline styles
- **Cursor movement**: Full CSI sequence support for cursor positioning
- **Display operations**: Clear screen, clear line, scrolling

## Running the Terminal Emulator

```bash
# Build the project
dotnet build

# Run the terminal emulator
dotnet run -- --terminal
```

The emulator will:
1. Start a shell process (bash on Unix/Linux/macOS, cmd on Windows)
2. Display the terminal output in an 80×24 grid
3. Capture keyboard input and send it to the shell
4. Parse and render all output with proper colors and text attributes

Press Ctrl+C or exit the shell to close the terminal emulator.

## Limitations (MVP)

This is a minimum viable implementation with the following intentional limitations:

- **No PTY support**: Uses basic Process I/O instead of pseudo-terminal
- **Fixed size**: Hard-coded to 80×24, no resizing
- **No scrollback**: Only displays the current 24 lines
- **Full screen redraw**: No optimization for partial updates
- **Basic process I/O**: Shell apps expecting full terminal features may not work correctly

## Future Enhancements

To move beyond MVP, consider:

1. **PTY Integration**: Use PtySharp or native PTY libraries for full terminal functionality
2. **Dynamic sizing**: Support window resize with SIGWINCH
3. **Scrollback buffer**: Implement history with scroll up/down
4. **Performance**: Add dirty rectangle tracking for optimized rendering
5. **Mouse support**: Capture and encode mouse events
6. **Alternative character sets**: Implement G0/G1/G2/G3 translation tables

## Code Organization

```
src/Terminal/
├── TerminalCell.cs          # Cell data structure
├── TerminalState.cs          # Terminal state model
├── ConsoleView.cs            # View renderer
├── InputController.cs        # Keyboard input handling
├── OutputController.cs       # Output processing
├── ProcessManager.cs         # Shell process management
├── AnsiParser.cs            # ANSI escape sequence parser
├── CsiHandler.cs            # CSI sequence handler
├── OscHandler.cs            # OSC sequence handler (libghostty-vt)
├── SgrHandler.cs            # SGR sequence handler (libghostty-vt)
└── TerminalEmulator.cs      # Main orchestration
```

## libghostty-vt Integration

This terminal emulator leverages libghostty-vt for:
- **Key encoding**: Converts keyboard events to terminal escape sequences
- **OSC parsing**: Parses Operating System Command sequences
- **SGR parsing**: Parses text styling sequences

The native library must be present in the `lib/` directory for the application to run.
