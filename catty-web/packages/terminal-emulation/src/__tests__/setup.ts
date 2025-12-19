import fc from 'fast-check';

import { traceSettings } from "../terminal/traceSettings";

// Configure fast-check for property-based testing
// Set minimum number of iterations to 100 as specified in the design document
fc.configureGlobal({
  numRuns: 100,
  verbose: false,
  seed: 42, // Use fixed seed for reproducible tests
  endOnFailure: true,
});

// Many tests assert on emitted trace chunks.
// Runtime default is disabled for performance; tests opt-in.
traceSettings.enabled = true;

// Export configured fast-check instance
export { fc };