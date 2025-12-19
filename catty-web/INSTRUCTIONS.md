# Coding style

- use good TypeScript practices
  - never use "any" type, use type narrowing and type guards as needed
  - prefer defining types and interfaces instead of inline anonymous types
  - prefer explicit types on function inputs and return types
- use two spaces for indentation
- prefer double quote " char for strings, use ' to avoid escaping and use ` for templated strings


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
