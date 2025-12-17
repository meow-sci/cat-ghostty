# Project Structure

## Workspace Organization

This is a pnpm monorepo with three main areas:

### `/app` - Frontend Application
- **Framework**: Astro with React integration
- **Purpose**: Web-based terminal interface
- **Key directories**:
  - `src/components/terminal/` - Terminal React components
  - `src/pages/terminal/` - Terminal page and routing
  - `src/ts/terminal/` - Terminal logic and controllers
  - `src/styles/` - CSS styling

### `/node-pty` - Backend Server
- **Purpose**: WebSocket server for PTY management
- **Key files**:
  - `src/BackendServer.ts` - Main WebSocket server class
  - `src/server.ts` - Server entry point
  - `src/__tests__/` - Integration and property tests

### `/packages` - Shared Libraries
Scoped packages under `@catty/` namespace:

- **`@catty/terminal-emulation`** - Core terminal parsing logic
  - ANSI/VT100 escape sequence parsing
  - Terminal state management
  - Comprehensive test suite with property-based testing
  
- **`@catty/controller`** - Terminal controller interfaces
- **`@catty/log`** - Logging utilities  
- **`@catty/state`** - State management helpers
- **`@catty/tsconfig`** - Shared TypeScript configuration

## Architecture Patterns

### Package Dependencies
- Frontend (`app`) depends on all `@catty/*` packages
- Backend (`node-pty`) is independent, only uses external dependencies
- Packages have minimal interdependencies (only `@catty/log` is shared)

### Code Organization
- **Types**: Comprehensive TypeScript interfaces exported from packages
- **Testing**: Property-based testing with fast-check for terminal emulation
- **Configuration**: Shared configs via workspace packages (`@catty/tsconfig`)

### File Naming Conventions
- **Components**: PascalCase (e.g., `Terminal.tsx`, `TerminalPage.tsx`)
- **Classes**: PascalCase (e.g., `BackendServer.ts`, `StatefulTerminal.ts`)
- **Types**: Descriptive interfaces with clear prefixes (e.g., `CsiMessage`, `SgrSequence`)
- **Tests**: `.test.ts` suffix with descriptive names (e.g., `Parser.csi.test.ts`)

### Import/Export Patterns
- Packages export comprehensive type definitions and classes via `index.ts`
- Clean separation between terminal emulation logic and UI components
- WebSocket communication isolated in backend server