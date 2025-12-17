# Product Overview

**caTTY** is a web-based terminal emulator built with modern web technologies. The project consists of:

- **Frontend**: Web-based terminal interface built with Astro and React
- **Backend**: WebSocket server that spawns and manages PTY (pseudo-terminal) processes
- **Terminal Emulation**: Custom ANSI/VT100 parser and terminal state management

The system allows users to interact with shell processes through a web browser, providing a full terminal experience with proper escape sequence handling, cursor management, and text formatting.

## Key Features

- Real-time terminal emulation via WebSocket connection
- ANSI escape sequence parsing (CSI, OSC, SGR commands)
- Terminal resizing and state management
- Trace/debug capabilities for terminal sequences
- Cross-platform shell support (bash, PowerShell)