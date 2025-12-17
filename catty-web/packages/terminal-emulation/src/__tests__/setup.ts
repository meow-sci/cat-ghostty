import fc from 'fast-check';

// Configure fast-check for property-based testing
// Set minimum number of iterations to 100 as specified in the design document
fc.configureGlobal({
  numRuns: 100,
  verbose: false,
  seed: 42, // Use fixed seed for reproducible tests
  endOnFailure: true,
});

// Export configured fast-check instance
export { fc };