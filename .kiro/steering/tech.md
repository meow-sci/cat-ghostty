---
inclusion: always
---

# Technology Stack

## Build System & Framework

- **Astro 5.x** - Static site generator with React integration
- **pnpm** - Package manager (always use pnpm, never npm or yarn)
- **TypeScript 5.x** - Strict mode enabled via `astro/tsconfigs/strict`
- **Vitest** - Testing framework with property-based testing support

## Frontend Stack

- **React 19.x** with TypeScript for UI components
- **Nanostores** for state management:
  - Use `atom()` for simple state, `map()` for objects
  - Import `useStore()` from `@nanostores/react` for React integration
  - Use `@nanostores/persistent` for localStorage persistence
  - Enable `@nanostores/logger` in development for debugging

## WebAssembly Integration Rules

- **WASM Library**: `ghostty-vt.wasm` (libghostty-vt)
- **Type Definitions**: `src/ts/ghostty-vt.d.ts`
- **Dependency Injection**: Always pass WASM instance to class constructors that need it
- **Testing**: Load WASM bundle in test setup and provide to instances
- **Memory Management**: Wrappers must handle allocate → use → free pattern
- **API Design**: Hide C API complexity, expose idiomatic TypeScript interfaces

## Working Directory

All commands run from `caTTY-ts/` directory:

```bash
pnpm install          # Install dependencies
pnpm dev              # Start dev server at localhost:4321
pnpm build            # Build production site to ./dist/
pnpm preview          # Preview production build
pnpm test             # Run tests in watch mode
pnpm test-run         # Run tests once (use in CI or for quick checks)
pnpm tsc              # Type check without emitting files
```

## Code Style Requirements

- Use TypeScript strict mode (no `any` types without justification)
- Prefer `const` over `let`, avoid `var`
- Use functional programming patterns where appropriate
- Write pure functions in `src/ts/` (no side effects, no DOM access)
- Use descriptive variable names (avoid single-letter except in loops/lambdas)
- Add JSDoc comments for public APIs and complex logic

## Testing Conventions

- Place tests in `__tests__/` directories adjacent to source files
- Name test files: `<FileName>.test.ts` or `<FileName>.property.test.ts`
- Use Vitest's `describe`, `it`, `expect` API
- Property-based tests for stateful components (use `fast-check` if needed)
- Always test WASM integration with actual WASM instance

## Deployment Configuration

- **Site URL**: `https://meow.science.fail`
- **Base Path**: `/caTTY/`
- **Trailing Slashes**: Always enforced (configured in `astro.config.ts`)
