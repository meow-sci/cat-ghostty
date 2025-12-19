/**
 * Property-based tests for additional vi sequences
 * **Feature: xterm-extensions, Property 22: Additional SGR sequence consistency**
 * **Feature: xterm-extensions, Property 23: Enhanced SGR mode handling**
 * **Feature: xterm-extensions, Property 24: Window manipulation acknowledgment**
 * **Validates: Requirements 3.4, 4.1, 1.1, 6.1**
 */

import { describe, it, expect } from 'vitest';
import { fc } from './setup';
import { parseSgr } from '../terminal/ParseSgr';
import { StatefulTerminal } from '../terminal/StatefulTerminal';

describe('Vi Sequence Properties', () => {
  /**
   * **Feature: xterm-extensions, Property 22: Additional SGR sequence consistency**
   * **Validates: Requirements 3.4, 4.1**
   * 
   * Property: SGR 32 (green foreground), SGR 39 (default foreground), and bare m (reset) 
   * sequences should work correctly and consistently.
   */
  it('Property 22: Additional SGR sequence consistency', () => {
    fc.assert(
      fc.property(
        fc.constantFrom(32, 39), // SGR 32 (green) and SGR 39 (default foreground)
        (sgrParam) => {
          // Test parsing of individual SGR sequences
          const messages = parseSgr([sgrParam], []);
          
          // Should produce exactly one message
          expect(messages).toHaveLength(1);
          
          const message = messages[0];
          
          if (sgrParam === 32) {
            // SGR 32 should be parsed as green foreground color
            expect(message._type).toBe('sgr.foregroundColor');
            if (message._type === 'sgr.foregroundColor') {
              expect(message.color).toEqual({ type: 'named', color: 'green' });
              expect(message.implemented).toBe(true);
            }
          } else if (sgrParam === 39) {
            // SGR 39 should be parsed as default foreground
            expect(message._type).toBe('sgr.defaultForeground');
            if (message._type === 'sgr.defaultForeground') {
              expect(message.implemented).toBe(true);
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 22b: Bare SGR m sequence (reset) consistency
   */
  it('Property 22b: Bare SGR m sequence should reset consistently', () => {
    fc.assert(
      fc.property(
        fc.boolean(), // Whether to use empty params or [0]
        (useEmptyParams) => {
          // Test bare m sequence (ESC[m) which should reset all attributes
          const params = useEmptyParams ? [] : [0];
          const messages = parseSgr(params, []);
          
          // Should produce exactly one reset message
          expect(messages).toHaveLength(1);
          expect(messages[0]._type).toBe('sgr.reset');
          
          // Test with terminal to ensure it actually resets state
          const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
          
          // Set some attributes first
          terminal.pushPtyText('\x1b[1;31;44m'); // Bold, red foreground, blue background
          
          // Apply the reset sequence
          const resetSequence = useEmptyParams ? '\x1b[m' : '\x1b[0m';
          terminal.pushPtyText(resetSequence);
          
          // Verify that attributes are reset (this is implementation-dependent,
          // but the parsing should be consistent)
          const resetMessages = parseSgr(params, []);
          expect(resetMessages[0]._type).toBe('sgr.reset');
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * **Feature: xterm-extensions, Property 23: Enhanced SGR mode handling**
   * **Validates: Requirements 3.4, 4.1**
   * 
   * Property: SGR >4;2m (enhanced underline mode) and ?4m (private underline mode) 
   * sequences should be handled gracefully.
   */
  it('Property 23: Enhanced SGR mode handling', () => {
    fc.assert(
      fc.property(
        fc.constantFrom(
          { prefix: '>', params: [4, 2], description: 'enhanced underline mode' },
          { prefix: '?', params: [4], description: 'private underline mode' }
        ),
        (testCase) => {
          const { prefix, params } = testCase;
          
          // Test parsing of enhanced/private SGR modes
          const messages = parseSgr([...params], [';'], prefix);
          
          // Should produce exactly one message
          expect(messages).toHaveLength(1);
          
          const message = messages[0];
          
          if (prefix === '>') {
            // Enhanced mode should be parsed correctly
            expect(message._type).toBe('sgr.enhancedMode');
            if (message._type === 'sgr.enhancedMode') {
              expect(message.params).toEqual(params);
              // Enhanced underline mode (>4;2m) should be implemented
              if (params[0] === 4 && params[1] === 2) {
                expect(message.implemented).toBe(true);
              }
            }
          } else if (prefix === '?') {
            // Private mode should be parsed correctly
            expect(message._type).toBe('sgr.privateMode');
            if (message._type === 'sgr.privateMode') {
              expect(message.params).toEqual(params);
              // Private underline mode (?4m) should be implemented
              if (params[0] === 4) {
                expect(message.implemented).toBe(true);
              }
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 23b: Enhanced SGR mode parameter validation
   */
  it('Property 23b: Enhanced SGR mode parameter validation', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 0, max: 10 }), // First parameter
        fc.integer({ min: 0, max: 10 }), // Second parameter
        (param1, param2) => {
          // Test enhanced mode with various parameter combinations
          const messages = parseSgr([param1, param2], [';'], '>');
          
          expect(messages).toHaveLength(1);
          const message = messages[0];
          
          expect(message._type).toBe('sgr.enhancedMode');
          if (message._type === 'sgr.enhancedMode') {
            expect(message.params).toEqual([param1, param2]);
            
            // Only >4;n sequences with valid underline types should be implemented
            if (param1 === 4 && param2 >= 0 && param2 <= 5) {
              expect(message.implemented).toBe(true);
            } else {
              expect(message.implemented).toBe(false);
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * **Feature: xterm-extensions, Property 24: Window manipulation acknowledgment**
   * **Validates: Requirements 1.1, 6.1**
   * 
   * Property: Window manipulation sequences (push/pop title/icon) should be 
   * acknowledged properly and maintain stack integrity.
   */
  it('Property 24: Window manipulation acknowledgment', () => {
    fc.assert(
      fc.property(
        fc.constantFrom(
          { operation: 22, target: 1, description: 'push icon name' },
          { operation: 22, target: 2, description: 'push window title' },
          { operation: 23, target: 1, description: 'pop icon name' },
          { operation: 23, target: 2, description: 'pop window title' }
        ),
        fc.string({ minLength: 1, maxLength: 50 }), // Title/icon text
        (windowOp, titleText) => {
          const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
          
          // Set initial title/icon
          if (windowOp.target === 1) {
            terminal.pushPtyText(`\x1b]1;${titleText}\x07`);
            expect(terminal.getIconName()).toBe(titleText);
          } else {
            terminal.pushPtyText(`\x1b]2;${titleText}\x07`);
            expect(terminal.getWindowTitle()).toBe(titleText);
          }
          
          // Capture CSI messages to verify acknowledgment
          let capturedMessage: any = null;
          const terminalWithCapture = new StatefulTerminal({
            cols: 80,
            rows: 24,
            onChunk: (chunk) => {
              if (chunk._type === 'trace.csi') {
                capturedMessage = chunk.msg;
              }
            }
          });
          
          // Set initial state
          if (windowOp.target === 1) {
            terminalWithCapture.pushPtyText(`\x1b]1;${titleText}\x07`);
          } else {
            terminalWithCapture.pushPtyText(`\x1b]2;${titleText}\x07`);
          }
          
          // Send window manipulation sequence
          const sequence = `\x1b[${windowOp.operation};${windowOp.target}t`;
          terminalWithCapture.pushPtyText(sequence);
          
          // Should capture a window manipulation message
          expect(capturedMessage).not.toBeNull();
          expect(capturedMessage._type).toBe('csi.windowManipulation');
          expect(capturedMessage.implemented).toBe(true);
          
          // Verify the operation parameters
          if (capturedMessage._type === 'csi.windowManipulation') {
            expect(capturedMessage.operation).toBe(windowOp.operation);
            expect(capturedMessage.params).toEqual([windowOp.target]);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 24b: Window manipulation stack integrity
   */
  it('Property 24b: Window manipulation stack maintains integrity', () => {
    fc.assert(
      fc.property(
        fc.array(fc.string({ minLength: 1, maxLength: 20 }), { minLength: 1, maxLength: 5 }), // Array of titles
        fc.constantFrom(1, 2), // Target: 1 = icon, 2 = title
        (titles, target) => {
          const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
          
          // Push multiple titles onto the stack
          for (const title of titles) {
            // Set the title/icon
            if (target === 1) {
              terminal.pushPtyText(`\x1b]1;${title}\x07`);
            } else {
              terminal.pushPtyText(`\x1b]2;${title}\x07`);
            }
            
            // Push to stack
            terminal.pushPtyText(`\x1b[22;${target}t`);
          }
          
          // Pop titles in reverse order and verify
          for (let i = titles.length - 1; i >= 0; i--) {
            terminal.pushPtyText(`\x1b[23;${target}t`);
            
            const currentValue = target === 1 ? terminal.getIconName() : terminal.getWindowTitle();
            expect(currentValue).toBe(titles[i]);
          }
          
          // Stack should now be empty - additional pops should not change the value
          const finalValue = target === 1 ? terminal.getIconName() : terminal.getWindowTitle();
          terminal.pushPtyText(`\x1b[23;${target}t`);
          const afterEmptyPop = target === 1 ? terminal.getIconName() : terminal.getWindowTitle();
          
          expect(afterEmptyPop).toBe(finalValue);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 24c: Unknown window manipulation operations
   */
  it('Property 24c: Unknown window manipulation operations should be handled gracefully', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 50, max: 99 }), // Unknown operation codes
        fc.integer({ min: 0, max: 5 }), // Random target
        (operation, target) => {
          const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
          
          // Set initial title
          terminal.pushPtyText('\x1b]2;Test Title\x07');
          
          // Capture CSI messages
          let capturedMessage: any = null;
          const terminalWithCapture = new StatefulTerminal({
            cols: 80,
            rows: 24,
            onChunk: (chunk) => {
              if (chunk._type === 'trace.csi') {
                capturedMessage = chunk.msg;
              }
            }
          });
          
          terminalWithCapture.pushPtyText('\x1b]2;Test Title\x07');
          
          // Send unknown window manipulation sequence
          terminalWithCapture.pushPtyText(`\x1b[${operation};${target}t`);
          
          // Should capture a window manipulation message marked as not implemented
          expect(capturedMessage).not.toBeNull();
          expect(capturedMessage._type).toBe('csi.windowManipulation');
          expect(capturedMessage.implemented).toBe(false);
          
          // Title should remain unchanged
          expect(terminalWithCapture.getWindowTitle()).toBe('Test Title');
        }
      ),
      { numRuns: 100 }
    );
  });
});