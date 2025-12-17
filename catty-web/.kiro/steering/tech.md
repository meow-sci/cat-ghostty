# Technology Stack

## Build System

- **Package Manager**: pnpm with workspace support
- **Monorepo Structure**: Multiple packages managed via `pnpm-workspace.yaml`
- **Build Tool**: Vite for bundling and development
- **TypeScript**: Strict type checking across all packages

## Frontend Stack

- **Framework**: Astro 5.x with React 19.x integration
- **State Management**: Nanostores with React bindings
- **Styling**: CSS with custom properties, Source Code Pro font for terminal
- **Testing**: Vitest with fast-check for property-based testing

## Backend Stack

- **Runtime**: Node.js with TypeScript
- **WebSocket**: ws library for real-time communication
- **PTY**: @lydell/node-pty for spawning shell processes
- **Development**: tsx for watch mode during development

## Key Libraries

- `@lydell/node-pty`: Native PTY bindings for terminal process management
- `nanostores`: Lightweight state management
- `fast-check`: Property-based testing framework
- `ws`: WebSocket server implementation

## Common Commands

### Development
```bash
# Install dependencies
pnpm install

# Start frontend dev server
cd app && pnpm dev

# Start backend WebSocket server
cd node-pty && pnpm dev

# Run all tests
pnpm test

# Run tests in a specific package
cd packages/terminal-emulation && pnpm test
```

### Building
```bash
# Build frontend
cd app && pnpm build

# Build backend
cd node-pty && pnpm build

# Type check
pnpm tsc
```

### Testing
```bash
# Run tests once
pnpm test-run

# Watch mode
pnpm test
```

## Catalog Dependencies

The workspace uses pnpm catalog for shared dependency versions:
- `@types/node`: 24.10.3
- `tsx`: 4.21.0
- `typescript`: 5.9.3
- `vite`: 7.2.7
- `vitest`: 4.0.15

## Code Style

- never use "as any" casts, always use type narrowing and type guards to narrow types as needed