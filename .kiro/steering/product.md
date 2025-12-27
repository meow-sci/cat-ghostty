# caTTY Terminal Emulator

**Dual Implementation**: TypeScript (browser/WebSocket) + C# (ImGui/ConPTY)
**Status**: TypeScript is the working reference; C# targets feature parity.

## Core Features
- Terminal key encoding, OSC/SGR/DSC parsing, headless parsers
- ECMA-48 baseline + xterm extensions (full TUI compatibility)
- MVC architecture: headless model, platform-specific controller/view

## Implementations
**TypeScript**: Headless logic + DOM controller + HTML `<pre>` view + WebSocket PTY backend
**C#**: Headless logic + ImGui controller + native rendering + Windows ConPTY backend

## C# Backend
- **ConPTY exclusive** (Windows 10 1809+, no fallback)
- **Development**: Standalone console app with ConPTY spawning
- **Production**: KSA game mod DLL with ImGui integration
- **Reference**: `KsaExampleMod/` shows StarMap attributes, mod.toml, resource cleanup