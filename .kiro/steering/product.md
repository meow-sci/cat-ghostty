# Product Overview

caTTY is a terminal emulator which has a working TypeScript-based implementation with a browser based display working now.

caTTY will also have a C# based terminal emulator with a ImGui based display.


## Core Functionality

- Terminal key encoding (converting keyboard events to terminal escape sequences)
- OSC (Operating System Command) parsing
- SGR (Select Graphic Rendition) attribute parsing for text styling and colors
- DSC
- Headless terminal parsers
- Headless stateful terminal
- Platform specific displays

## Architecture

The project follows an MVC pattern:
- TypeScript version
  - Model: Headless TypeScript
  - Controller: Browser DOM event handling and data flow coordination
  - View: HTML `<pre>` element with CSS absolute positioning for character rendering
- C# version
  - Model: Headless C#
  - Controller: ImGui event handler and data flow coordination
  - View: ImGui native text rendering and drawing commands

## Features

- baseline ECMA-48 support
- a lot of xterm terminal emulation extensions (enough that most common TUIs work fully)

## Backend

- TypeScript version
  - WebSocket backend that establishes a real pty and hooks it up to the display controller to shuffle the data
- C# version
  - **Development**: Standalone console application with ConPTY process spawning
  - **Game Integration**: Hook into game's existing process management or create separate ConPTY process
  - **Process Management**: **Windows ConPTY (Pseudoconsole) exclusively** - Uses Microsoft's ConPTY APIs for true PTY functionality
  - **PTY Implementation**: `CreatePseudoConsole`, `ResizePseudoConsole`, pipe-based I/O following Microsoft's official documentation
  - **Data Flow**: Direct ConPTY pipe processing (no WebSocket overhead)
  - **Platform Support**: **Windows 10 1809+ only** - ConPTY is the exclusive PTY backend, no fallback to process redirection

## Game Mod Integration

- **Deployment**: Packaged as DLL and loaded by KSA game engine
- **UI Integration**: Renders within game's ImGui context
- **Resource Management**: Shares game's graphics context and memory pools
- **Input Handling**: Integrates with game's input system for keyboard/mouse events
- **Lifecycle**: Managed by game's mod loading system
- **Reference Implementation**: Complete working example in `KsaExampleMod/` folder
  - Shows StarMap attribute-based mod implementation
  - Demonstrates ImGui integration with OnAfterUi/OnBeforeUi methods
  - Includes mod.toml metadata file structure
  - Shows proper resource cleanup and Harmony patching patterns

  