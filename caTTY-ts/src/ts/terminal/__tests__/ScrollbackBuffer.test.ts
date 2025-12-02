/**
 * Unit tests for ScrollbackBuffer.
 * These tests verify the circular buffer implementation for scrollback history.
 */

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { ScrollbackBuffer } from '../ScrollbackBuffer.js';
import type { Line } from '../types.js';

describe('ScrollbackBuffer', () => {
  // Helper to create a test line
  const createLine = (content: string): Line => ({
    cells: [
      {
        char: content,
        width: 1,
        fg: { type: 'default' },
        bg: { type: 'default' },
        bold: false,
        italic: false,
        underline: 0,
        inverse: false,
        strikethrough: false,
      },
    ],
    wrapped: false,
  });

  it('should initialize with correct max size', () => {
    const buffer = new ScrollbackBuffer(100);
    expect(buffer.getMaxSize()).toBe(100);
    expect(buffer.getSize()).toBe(0);
  });

  it('should throw error for negative max size', () => {
    expect(() => new ScrollbackBuffer(-1)).toThrow('ScrollbackBuffer maxSize must be non-negative');
  });

  it('should allow zero max size (no scrollback)', () => {
    const buffer = new ScrollbackBuffer(0);
    expect(buffer.getMaxSize()).toBe(0);
    expect(buffer.getSize()).toBe(0);
  });

  it('should push and retrieve lines', () => {
    const buffer = new ScrollbackBuffer(10);
    const line1 = createLine('Line 1');
    const line2 = createLine('Line 2');

    buffer.push(line1);
    buffer.push(line2);

    expect(buffer.getSize()).toBe(2);
    expect(buffer.get(0)).toBe(line1);
    expect(buffer.get(1)).toBe(line2);
  });

  it('should not store lines when max size is 0', () => {
    const buffer = new ScrollbackBuffer(0);
    const line = createLine('Test');

    buffer.push(line);

    expect(buffer.getSize()).toBe(0);
    expect(buffer.get(0)).toBeUndefined();
  });

  it('should remove oldest lines when exceeding max size', () => {
    const buffer = new ScrollbackBuffer(3);
    const line1 = createLine('Line 1');
    const line2 = createLine('Line 2');
    const line3 = createLine('Line 3');
    const line4 = createLine('Line 4');

    buffer.push(line1);
    buffer.push(line2);
    buffer.push(line3);
    buffer.push(line4); // This should remove line1

    expect(buffer.getSize()).toBe(3);
    expect(buffer.get(0)).toBe(line2);
    expect(buffer.get(1)).toBe(line3);
    expect(buffer.get(2)).toBe(line4);
  });

  it('should handle circular buffer wraparound', () => {
    const buffer = new ScrollbackBuffer(3);

    // Fill buffer
    buffer.push(createLine('A'));
    buffer.push(createLine('B'));
    buffer.push(createLine('C'));

    // Add more to trigger wraparound
    buffer.push(createLine('D'));
    buffer.push(createLine('E'));

    expect(buffer.getSize()).toBe(3);
    expect(buffer.get(0)?.cells[0].char).toBe('C');
    expect(buffer.get(1)?.cells[0].char).toBe('D');
    expect(buffer.get(2)?.cells[0].char).toBe('E');
  });

  it('should return undefined for out of bounds access', () => {
    const buffer = new ScrollbackBuffer(10);
    buffer.push(createLine('Line 1'));

    expect(buffer.get(-1)).toBeUndefined();
    expect(buffer.get(1)).toBeUndefined();
    expect(buffer.get(100)).toBeUndefined();
  });

  it('should clear all lines', () => {
    const buffer = new ScrollbackBuffer(10);
    buffer.push(createLine('Line 1'));
    buffer.push(createLine('Line 2'));
    buffer.push(createLine('Line 3'));

    expect(buffer.getSize()).toBe(3);

    buffer.clear();

    expect(buffer.getSize()).toBe(0);
    expect(buffer.get(0)).toBeUndefined();
  });

  it('should maintain max size after clear', () => {
    const buffer = new ScrollbackBuffer(10);
    buffer.push(createLine('Line 1'));
    buffer.clear();

    expect(buffer.getMaxSize()).toBe(10);
    expect(buffer.getSize()).toBe(0);
  });

  it('should handle many push operations correctly', () => {
    const buffer = new ScrollbackBuffer(5);

    // Push 10 lines
    for (let i = 0; i < 10; i++) {
      buffer.push(createLine(`Line ${i}`));
    }

    // Should only have last 5 lines
    expect(buffer.getSize()).toBe(5);
    expect(buffer.get(0)?.cells[0].char).toBe('Line 5');
    expect(buffer.get(4)?.cells[0].char).toBe('Line 9');
  });
});

describe('ScrollbackBuffer Property Tests', () => {
  // Helper to create a test line with identifiable content
  const createLine = (content: string): Line => ({
    cells: [
      {
        char: content,
        width: 1,
        fg: { type: 'default' },
        bg: { type: 'default' },
        bold: false,
        italic: false,
        underline: 0,
        inverse: false,
        strikethrough: false,
      },
    ],
    wrapped: false,
  });

  /**
   * Feature: headless-terminal-emulator, Property 25: Scrollback captures scrolled content
   * For any terminal with scrollback enabled, when content scrolls off the top of the screen, it should be retrievable from the scrollback buffer
   * Validates: Requirements 8.1, 8.5
   */
  it('Property 25: Scrollback captures scrolled content', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 1, max: 100 }), // maxSize
        fc.integer({ min: 1, max: 50 }),  // number of lines to push
        (maxSize, numLines) => {
          const buffer = new ScrollbackBuffer(maxSize);
          
          // Push lines with identifiable content
          const pushedLines: Line[] = [];
          for (let i = 0; i < numLines; i++) {
            const line = createLine(`Line ${i}`);
            pushedLines.push(line);
            buffer.push(line);
          }
          
          // Determine how many lines should be in the buffer
          const expectedSize = Math.min(numLines, maxSize);
          expect(buffer.getSize()).toBe(expectedSize);
          
          // Verify we can retrieve the lines that should be in the buffer
          // The buffer should contain the last 'expectedSize' lines
          const startIndex = Math.max(0, numLines - maxSize);
          for (let i = 0; i < expectedSize; i++) {
            const retrievedLine = buffer.get(i);
            const expectedLine = pushedLines[startIndex + i];
            
            expect(retrievedLine).toBeDefined();
            expect(retrievedLine?.cells[0].char).toBe(expectedLine.cells[0].char);
          }
          
          // Verify we cannot retrieve lines beyond the buffer size
          expect(buffer.get(expectedSize)).toBeUndefined();
          expect(buffer.get(expectedSize + 1)).toBeUndefined();
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 26: Scrollback buffer size limit
   * For any scrollback buffer with maximum size M, when more than M lines are added, the oldest lines should be removed to maintain size M
   * Validates: Requirements 8.2
   */
  it('Property 26: Scrollback buffer size limit', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 1, max: 50 }),  // maxSize
        fc.integer({ min: 1, max: 100 }), // number of lines to push (potentially more than maxSize)
        (maxSize, numLines) => {
          const buffer = new ScrollbackBuffer(maxSize);
          
          // Push lines
          for (let i = 0; i < numLines; i++) {
            buffer.push(createLine(`Line ${i}`));
          }
          
          // Verify buffer size never exceeds maxSize
          expect(buffer.getSize()).toBeLessThanOrEqual(maxSize);
          expect(buffer.getSize()).toBe(Math.min(numLines, maxSize));
          
          // If we pushed more than maxSize lines, verify oldest lines were removed
          if (numLines > maxSize) {
            // The first line in the buffer should be line (numLines - maxSize)
            const firstLine = buffer.get(0);
            expect(firstLine?.cells[0].char).toBe(`Line ${numLines - maxSize}`);
            
            // The last line in the buffer should be line (numLines - 1)
            const lastLine = buffer.get(maxSize - 1);
            expect(lastLine?.cells[0].char).toBe(`Line ${numLines - 1}`);
            
            // Verify we cannot retrieve the oldest lines that should have been removed
            // (This is implicit in the above checks, but we verify the buffer size)
            expect(buffer.getSize()).toBe(maxSize);
          }
          
          // Verify all retrievable lines are the most recent ones
          const startIndex = Math.max(0, numLines - maxSize);
          for (let i = 0; i < buffer.getSize(); i++) {
            const line = buffer.get(i);
            expect(line?.cells[0].char).toBe(`Line ${startIndex + i}`);
          }
        }
      ),
      { numRuns: 100 }
    );
  });
});
