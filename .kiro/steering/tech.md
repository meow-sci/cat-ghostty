# Technology Stack

## Build System & Framework

- Astro 5.x (static site generator with React integration)
- pnpm (package manager)
- TypeScript 5.x (strict mode enabled via `astro/tsconfigs/strict`)
- Vitest (testing framework)

## Frontend Libraries

- React 19.x with TypeScript
- Nanostores (state management)
  - `@nanostores/react` for React integration
  - `@nanostores/persistent` for persistence
  - `@nanostores/logger` for debugging
- Font packages:
  - `@fontsource-variable/inter`
  - `@fontsource-variable/oxanium`
  - `@fontsource-variable/source-code-pro`

## WebAssembly Integration

- libghostty-vt WASM library (`ghostty-vt.wasm`)
- Custom TypeScript wrappers for WASM C API
- Type definitions in `src/ts/ghostty-vt.d.ts`

## Common Commands

All commands run from `caTTY-ts/` directory:

```bash
pnpm install          # Install dependencies
pnpm dev              # Start dev server at localhost:4321
pnpm build            # Build production site to ./dist/
pnpm preview          # Preview production build
pnpm test             # Run tests in watch mode
pnpm test-run         # Run tests once
pnpm tsc              # Type check
```

## Deployment Configuration

- Site URL: `https://meow.science.fail`
- Base path: `/caTTY/`
- Trailing slashes: always enforced
