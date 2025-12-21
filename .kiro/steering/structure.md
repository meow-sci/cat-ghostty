# Project Structure

## Root Layout

### TypeScript caTTY implementation

```
- catty-web: pnpm workspace
  - catty-web/app: astro app for the TerminalController (glue) and display
  - catty-web/node-pty: backend websocket server for pty connection to real shells
  - catty-web/packages/log: logging helper
  - catty-web/packages/tsconfig: shared TypeScript config
  - catty-web/packages/terminal-emulation: all headless terminal logic (parsers, stateful terminal)
```

#### Key Locations

```
- catty-web\packages\terminal-emulation\src\terminal\Parser.ts: entrypoint for terminal emulation parsing
- catty-web\packages\terminal-emulation\src\terminal\StatefulTerminal.ts: entrypoint for stateful terminal
- catty-web\node-pty\src\BackendServer.ts: the backend websocket server for pty shells
- catty-web\app\src\ts\terminal\TerminalController.ts: the web/DOM specific controller for glue code to headless terminal logic
```

### Example ImGui application

```
- KsaImGuiTestWin/: A simple C# dotnet 10 console application which is properly setup to link against the necessary DLLs for the target ImGui C# based runtime environment.
```


## Code Organization Principles

### Headless Design

All business logic must be:
- 100% TypeScript / C#
- Headless parts (no DOM/browser APIs or ImGui code)
- Testable in isolation
- Framework-agnostic

### Display controller / glue

- For TypeScript, there is a TerminalController which is the web/DOM display specific bridge and glue code from the headless code
- For C#, there will be a TerminalController which is the ImGui display specific bridge and glue code

### Web/Astro Page Components

- Astro pages (`.astro`) are minimal entry points
- React components (`_ts/` directories) contain UI logic
- Prefix with `_` for non-routable directories
- CSS files prefixed with `_` for component-specific styles
