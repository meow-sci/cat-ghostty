/**
 * Unit tests for ScrollbackBuffer.
 * These tests verify the circular buffer implementation for scrollback history.
 */

import { describe, it, expect } from 'vitest';
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
