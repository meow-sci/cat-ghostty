# Important locations

- `src/` the astro web app which contains `app/src/ts/terminal/TerminalController.ts`, which is the glue code for the terminal ui/display
- `packages/terminal-emulation/` the terminal-emulation typescript package which contains the headless terminal implementation classes
  - `packages/terminal-emulation/src/terminal/Parser.ts` headless parser
  - `packages/terminal-emulation/src/terminal/StatefulTerminal.ts` headless stateful terminal logic
  - adjacent files are imported by these main entrypoints as needed

# Coding style

- use good TypeScript practices
  - never use "any" type, use type narrowing and type guards as needed
  - prefer defining types and interfaces instead of inline anonymous types
  - prefer explicit types on function inputs and return types
- use two spaces for indentation
- prefer double quote " char for strings, use ' to avoid escaping and use ` for templated strings
- prefer standalone stateuless side-effect free pure functional style code when possible.  reduce size and complexity of stateful classes by moving any stateless / pure functions outside

# Unit tests

- Test all, from project root: `pnpm run test`
- Test app (astro web project), from project root: `pnpm -C app run test`
- Test terminal-emulation lib, from project root: `pnpm -C packages/terminal-emulation run test`

# Compilation tests

- Test all, from project root: `pnpm run tsc`
- Test app (astro web project), from project root: `pnpm -C app run tsc`
- Test terminal-emulation lib, from project root: `pnpm -C packages/terminal-emulation run tsc`

# Bookkeeping

- Always update TERMINAL_SPEC_COVERAGE.md with feature coverage
- If necessary update FEATURE_GAPS.md when features mentioned in it are updated

# Final summary requirements

- When finished, in addition to the summary of work done, include a markdown formatted block suitable for a git commit message with both a < 80 char subject line and then a good markdown formatted summary of what was implemented

