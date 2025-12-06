# Project Structure

## Root Layout

```
caTTY-ts/               # Main Astro project
caTTY-node-pty/         # vanilla TypeScript project which runs a WebSocket <> PTY process bridge
ghostty-examples/       # Reference implementations (HTML demos)
```

## caTTY-ts Directory Structure

### Core Directories

- `ghostty-c-api/include` - contains the C header files for the libghostty-vt functionality.  includes very useful reference documentation inside the comments about behavior.

- `caTTY-ts` - Astro project to host and run the terminal from a web page in a browser
  - `src/pages/` - Astro pages (file-based routing)
    - `index.astro` - Homepage
    - `demos/` - Interactive demo pages
      - `keyencode/` - Key encoding demo
      - `sgr/` - SGR parsing demo
      - `osc/` - OSC parsing demo
    - `terminal/` - Full terminal emulator page
  - `src/ts/` - Headless TypeScript logic (pure, framework-agnostic)
    - `terminal/` - Core terminal functionality
      - `keyencode/` - Key encoding utilities
      - `sgr/` - SGR constants and types
      - `osc/` - OSC type definitions
      - `wasm/` - WASM loading and integration
    - `state/` - State management utilities
  - `src/styles/` - Global CSS
  - `src/layouts/` - Astro layout components
  - `public/` - Static assets (including `ghostty-vt.wasm`)

- `caTTY-node-pty` - vanilla typescript project which will run a websocket server where each new connection will launch a pty process and stream the data back/forth between the websocket client and the underlying pty process


### Demo Page Pattern

Each demo follows this structure:
```
demos/<feature>/
  index.astro              # Astro page entry point
  _<feature>demo.css       # Demo-specific styles
  _ts/
    <Feature>DemoPage.tsx  # Root React component
    <Feature>Demo.tsx      # Main demo component
    <Feature>DemoState.ts  # Nanostores state
    pure/                  # Pure TypeScript logic
      Stateful<Feature>Parser.ts
    ui/                    # UI-specific utilities
```

## Code Organization Principles

### Headless Design

All business logic in `src/ts/` must be:
- 100% TypeScript (no framework dependencies)
- Headless (no DOM/browser APIs)
- Testable in isolation
- Framework-agnostic

### WASM Integration

- WASM wrappers encapsulate all C API complexity
- TypeScript consumers get type-safe, idiomatic APIs
- Buffer management and memory allocation hidden from callers
- Pattern: allocate → use → free (handled in wrapper)

### Page Components

- Astro pages (`.astro`) are minimal entry points
- React components (`_ts/` directories) contain UI logic
- Prefix with `_` for non-routable directories
- CSS files prefixed with `_` for component-specific styles

## Reference Materials

- `ghostty-examples/web/` - Working HTML demos for WASM integration patterns
- `WASM_NOTES.md` - Complete list of libghostty-vt WASM exports
- `src/DESIGN.md` - Detailed architecture documentation
