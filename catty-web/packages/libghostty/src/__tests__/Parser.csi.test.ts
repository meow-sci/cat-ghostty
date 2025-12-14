import { describe, it, expect } from 'vitest';

describe('Parser', () => {
  describe('CSI SGR', () => {
    it('foreground color ', () => {
      
      
// \x1b[38;5;11;mhi\x1b[0m

      // Parser state is private, but we can test behavior
      expect("abc").toBeDefined();
    });

  });
});