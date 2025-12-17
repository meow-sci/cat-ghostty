import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    // Property-based testing configuration
    globals: true,
    environment: 'node',
    // Increase timeout for property-based tests which run many iterations
    testTimeout: 10000,
    // Configure fast-check for property-based testing
    setupFiles: ['./src/__tests__/setup.ts'],
  },
});