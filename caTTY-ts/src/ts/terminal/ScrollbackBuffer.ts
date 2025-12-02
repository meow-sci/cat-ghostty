/**
 * ScrollbackBuffer - Circular buffer for terminal scrollback history.
 * 
 * This class manages historical lines that have scrolled off the top of the
 * visible screen. It uses a circular buffer implementation for efficient
 * memory management with a configurable maximum size.
 * 
 * Requirements: 8.1, 8.2, 8.5
 */

import type { Line } from './types';

/**
 * A circular buffer that stores scrollback lines with a maximum size limit.
 * When the buffer is full, the oldest lines are automatically removed.
 */
export class ScrollbackBuffer {
  private buffer: Line[];
  private maxSize: number;
  private startIndex: number;
  private length: number;

  /**
   * Creates a new scrollback buffer.
   * @param maxSize Maximum number of lines to store (must be >= 0)
   */
  constructor(maxSize: number) {
    if (maxSize < 0) {
      throw new Error('ScrollbackBuffer maxSize must be non-negative');
    }
    
    this.buffer = [];
    this.maxSize = maxSize;
    this.startIndex = 0;
    this.length = 0;
  }

  /**
   * Adds a line to the scrollback buffer.
   * If the buffer is at maximum capacity, the oldest line is removed.
   * 
   * @param line The line to add to scrollback
   */
  push(line: Line): void {
    if (this.maxSize === 0) {
      // No scrollback allowed
      return;
    }

    if (this.length < this.maxSize) {
      // Buffer not yet full, just append
      this.buffer.push(line);
      this.length++;
    } else {
      // Buffer is full, overwrite oldest entry (circular)
      const writeIndex = this.startIndex % this.maxSize;
      this.buffer[writeIndex] = line;
      this.startIndex = (this.startIndex + 1) % this.maxSize;
    }
  }

  /**
   * Retrieves a line from the scrollback buffer.
   * 
   * @param index The index of the line to retrieve (0 = oldest line)
   * @returns The line at the specified index, or undefined if index is out of bounds
   */
  get(index: number): Line | undefined {
    if (index < 0 || index >= this.length) {
      return undefined;
    }

    const actualIndex = (this.startIndex + index) % this.maxSize;
    return this.buffer[actualIndex];
  }

  /**
   * Clears all lines from the scrollback buffer.
   */
  clear(): void {
    this.buffer = [];
    this.startIndex = 0;
    this.length = 0;
  }

  /**
   * Returns the current number of lines in the scrollback buffer.
   */
  getSize(): number {
    return this.length;
  }

  /**
   * Returns the maximum capacity of the scrollback buffer.
   */
  getMaxSize(): number {
    return this.maxSize;
  }
}
