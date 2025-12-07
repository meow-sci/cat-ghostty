/**
 * Property-based tests for Kitty Graphics Protocol parsing.
 * These tests verify that graphics commands are correctly parsed and processed.
 * 
 * @vitest-environment jsdom
 */

import { describe, it, expect, beforeAll } from 'vitest';
import * as fc from 'fast-check';
import { KittyGraphicsParser } from '../graphics/KittyGraphicsParser.js';

describe('KittyGraphicsParser Property Tests', () => {
  // Mock createImageBitmap for testing (not available in jsdom by default)
  beforeAll(() => {
    // Define ImageBitmap constructor for instanceof checks
    if (!globalThis.ImageBitmap) {
      (globalThis as any).ImageBitmap = class ImageBitmap {
        width = 1;
        height = 1;
        close() {}
      };
    }
    
    if (!globalThis.createImageBitmap) {
      globalThis.createImageBitmap = async (blob: Blob): Promise<ImageBitmap> => {
        // Create a mock ImageBitmap instance with actual dimensions from the blob
        const mockBitmap = new (globalThis as any).ImageBitmap();
        // For testing purposes, we'll just use 1x1 dimensions
        // In a real implementation, we'd decode the blob to get actual dimensions
        return mockBitmap;
      };
    }
  });

  /**
   * Feature: headless-terminal-emulator, Property 75: Graphics command parsing
   * For any valid Kitty graphics escape sequence, the parser should extract the action and payload correctly
   * Validates: Requirements 26.1
   */
  it('Property 75: Graphics command parsing', () => {
    fc.assert(
      fc.property(
        // Generate different types of graphics commands
        fc.oneof(
          // Transmission command
          fc.record({
            action: fc.constant('t' as const),
            imageId: fc.option(fc.integer({ min: 1, max: 1000 }), { nil: undefined }),
            format: fc.option(fc.constantFrom('24', '32', '100'), { nil: undefined }),
            width: fc.option(fc.integer({ min: 1, max: 2000 }), { nil: undefined }),
            height: fc.option(fc.integer({ min: 1, max: 2000 }), { nil: undefined }),
            more: fc.option(fc.boolean(), { nil: undefined }),
          }),
          // Display command
          fc.record({
            action: fc.constant('d' as const),
            imageId: fc.option(fc.integer({ min: 1, max: 1000 }), { nil: undefined }),
            placementId: fc.option(fc.integer({ min: 1, max: 1000 }), { nil: undefined }),
            x: fc.option(fc.integer({ min: 0, max: 200 }), { nil: undefined }),
            y: fc.option(fc.integer({ min: 0, max: 200 }), { nil: undefined }),
            rows: fc.option(fc.integer({ min: 1, max: 100 }), { nil: undefined }),
            cols: fc.option(fc.integer({ min: 1, max: 100 }), { nil: undefined }),
            sourceX: fc.option(fc.integer({ min: 0, max: 2000 }), { nil: undefined }),
            sourceY: fc.option(fc.integer({ min: 0, max: 2000 }), { nil: undefined }),
            sourceWidth: fc.option(fc.integer({ min: 1, max: 2000 }), { nil: undefined }),
            sourceHeight: fc.option(fc.integer({ min: 1, max: 2000 }), { nil: undefined }),
            zIndex: fc.option(fc.integer({ min: -100, max: 100 }), { nil: undefined }),
            unicodePlaceholder: fc.option(fc.integer({ min: 0x1F000, max: 0x1F999 }), { nil: undefined }),
          }),
          // Delete command
          fc.record({
            action: fc.constant('D' as const),
            imageId: fc.option(fc.integer({ min: 1, max: 1000 }), { nil: undefined }),
            placementId: fc.option(fc.integer({ min: 1, max: 1000 }), { nil: undefined }),
          })
        ),
        fc.string({ maxLength: 100 }), // payload
        (command, payload) => {
        const parser = new KittyGraphicsParser();
        
        // Build control data string from command
        const controlPairs: string[] = [];
        
        // Add action
        controlPairs.push(`a=${command.action}`);
        
        // Add optional parameters based on command type
        if ('imageId' in command && command.imageId !== undefined) {
          controlPairs.push(`i=${command.imageId}`);
        }
        if ('placementId' in command && command.placementId !== undefined) {
          controlPairs.push(`p=${command.placementId}`);
        }
        if ('format' in command && command.format !== undefined) {
          controlPairs.push(`f=${command.format}`);
        }
        if ('width' in command && command.width !== undefined) {
          controlPairs.push(`s=${command.width}`);
        }
        if ('height' in command && command.height !== undefined) {
          controlPairs.push(`v=${command.height}`);
        }
        if ('x' in command && command.x !== undefined) {
          controlPairs.push(`x=${command.x}`);
        }
        if ('y' in command && command.y !== undefined) {
          controlPairs.push(`y=${command.y}`);
        }
        if ('rows' in command && command.rows !== undefined) {
          controlPairs.push(`r=${command.rows}`);
        }
        if ('cols' in command && command.cols !== undefined) {
          controlPairs.push(`c=${command.cols}`);
        }
        if ('sourceX' in command && command.sourceX !== undefined) {
          controlPairs.push(`X=${command.sourceX}`);
        }
        if ('sourceY' in command && command.sourceY !== undefined) {
          controlPairs.push(`Y=${command.sourceY}`);
        }
        if ('sourceWidth' in command && command.sourceWidth !== undefined) {
          controlPairs.push(`w=${command.sourceWidth}`);
        }
        if ('sourceHeight' in command && command.sourceHeight !== undefined) {
          controlPairs.push(`h=${command.sourceHeight}`);
        }
        if ('more' in command && command.more !== undefined) {
          controlPairs.push(`m=${command.more ? '1' : '0'}`);
        }
        if ('zIndex' in command && command.zIndex !== undefined) {
          controlPairs.push(`z=${command.zIndex}`);
        }
        if ('unicodePlaceholder' in command && command.unicodePlaceholder !== undefined) {
          controlPairs.push(`U=${command.unicodePlaceholder}`);
        }
        
        const controlData = controlPairs.join(',');
        const sequence = `${controlData};${payload}`;
        
        // Parse the sequence
        const result = parser.parseGraphicsCommand(sequence);
        
        // Verify parsing succeeded
        expect(result).not.toBeNull();
        if (!result) return;
        
        // Verify action is correct
        expect(result.params.action).toBe(command.action);
        
        // Verify payload is correct
        expect(result.payload).toBe(payload);
        
        // Verify all provided parameters are parsed correctly
        if ('imageId' in command && command.imageId !== undefined) {
          expect(result.params.imageId).toBe(command.imageId);
        }
        if ('placementId' in command && command.placementId !== undefined) {
          expect(result.params.placementId).toBe(command.placementId);
        }
        if ('format' in command && command.format !== undefined) {
          expect(result.params.format).toBe(command.format);
        }
        if ('width' in command && command.width !== undefined) {
          expect(result.params.width).toBe(command.width);
        }
        if ('height' in command && command.height !== undefined) {
          expect(result.params.height).toBe(command.height);
        }
        if ('x' in command && command.x !== undefined) {
          expect(result.params.x).toBe(command.x);
        }
        if ('y' in command && command.y !== undefined) {
          expect(result.params.y).toBe(command.y);
        }
        if ('rows' in command && command.rows !== undefined) {
          expect(result.params.rows).toBe(command.rows);
        }
        if ('cols' in command && command.cols !== undefined) {
          expect(result.params.cols).toBe(command.cols);
        }
        if ('sourceX' in command && command.sourceX !== undefined) {
          expect(result.params.sourceX).toBe(command.sourceX);
        }
        if ('sourceY' in command && command.sourceY !== undefined) {
          expect(result.params.sourceY).toBe(command.sourceY);
        }
        if ('sourceWidth' in command && command.sourceWidth !== undefined) {
          expect(result.params.sourceWidth).toBe(command.sourceWidth);
        }
        if ('sourceHeight' in command && command.sourceHeight !== undefined) {
          expect(result.params.sourceHeight).toBe(command.sourceHeight);
        }
        if ('more' in command && command.more !== undefined) {
          expect(result.params.more).toBe(command.more);
        }
        if ('zIndex' in command && command.zIndex !== undefined) {
          expect(result.params.zIndex).toBe(command.zIndex);
        }
        if ('unicodePlaceholder' in command && command.unicodePlaceholder !== undefined) {
          expect(result.params.unicodePlaceholder).toBe(command.unicodePlaceholder);
        }
      }),
      { numRuns: 100 }
    );
  });

  it('Property 75: Graphics command parsing handles invalid sequences gracefully', () => {
    fc.assert(
      fc.property(
        fc.string({ maxLength: 100 }),
        (invalidSequence) => {
          const parser = new KittyGraphicsParser();
          
          // Parse invalid sequence (no action parameter)
          const result = parser.parseGraphicsCommand(invalidSequence);
          
          // Should return null for invalid sequences
          // (sequences without an action parameter)
          if (!invalidSequence.includes('a=t') && 
              !invalidSequence.includes('a=d') && 
              !invalidSequence.includes('a=D')) {
            expect(result).toBeNull();
          }
        }),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 76: Image data decoding
   * For any base64-encoded image data in a supported format, the terminal should decode it to a displayable image
   * Validates: Requirements 26.2, 30.1, 30.2, 30.3
   */
  it('Property 76: Image data decoding', async () => {
    // Sample valid images in different formats (1x1 pixel images)
    const validImages = {
      png: 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFBQIAX8jx0gAAAABJRU5ErkJggg==',
      jpeg: '/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCwAA8A/9k=',
      gif: 'R0lGODlhAQABAPAAAP8AAP///yH5BAAAAAAALAAAAAABAAEAAAICRAEAOw=='
    };

    await fc.assert(
      fc.asyncProperty(
        fc.constantFrom('png', 'jpeg', 'gif'),
        async (format) => {
          const parser = new KittyGraphicsParser();
          const imageData = validImages[format];
          
          // Decode the image
          const result = await parser.decodeImageData(imageData);
          
          // Verify the result has all required properties
          expect(result).toBeDefined();
          expect(result.bitmap).toBeDefined();
          expect(result.format).toBe(format);
          expect(result.width).toBeGreaterThan(0);
          expect(result.height).toBeGreaterThan(0);
          
          // Verify bitmap is an ImageBitmap instance
          expect(result.bitmap).toBeInstanceOf(ImageBitmap);
          
          // Verify dimensions match bitmap dimensions
          expect(result.width).toBe(result.bitmap.width);
          expect(result.height).toBe(result.bitmap.height);
        }
      ),
      { numRuns: 100 }
    );
  });

  it('Property 76: Image data decoding handles invalid data gracefully', async () => {
    await fc.assert(
      fc.asyncProperty(
        fc.oneof(
          // Empty string
          fc.constant(''),
          // Invalid base64 (contains invalid characters)
          fc.string({ minLength: 1, maxLength: 50 }).filter(s => {
            try {
              atob(s);
              return false; // Valid base64, skip
            } catch {
              return true; // Invalid base64, use it
            }
          }),
          // Valid base64 but not an image (random bytes)
          fc.array(fc.integer({ min: 0, max: 255 }), { minLength: 10, maxLength: 100 }).map(bytes => {
            const binaryString = String.fromCharCode(...bytes);
            return btoa(binaryString);
          }).filter(base64 => {
            // Filter out any that accidentally create valid image headers
            try {
              const binaryString = atob(base64);
              const bytes = new Uint8Array(binaryString.length);
              for (let i = 0; i < binaryString.length; i++) {
                bytes[i] = binaryString.charCodeAt(i);
              }
              // Check if it's not a valid image format
              const isPNG = bytes.length >= 8 && bytes[0] === 0x89 && bytes[1] === 0x50;
              const isJPEG = bytes.length >= 3 && bytes[0] === 0xff && bytes[1] === 0xd8;
              const isGIF = bytes.length >= 6 && String.fromCharCode(...bytes.slice(0, 3)) === 'GIF';
              return !isPNG && !isJPEG && !isGIF;
            } catch {
              return true;
            }
          })
        ),
        async (invalidData) => {
          const parser = new KittyGraphicsParser();
          
          // Attempt to decode invalid data
          // Should throw an error
          await expect(parser.decodeImageData(invalidData)).rejects.toThrow();
        }
      ),
      { numRuns: 100 }
    );
  });

  it('Property 76: Image data decoding rejects unsupported format codes', async () => {
    await fc.assert(
      fc.asyncProperty(
        // Generate unsupported format codes (not 24, 32, or 100)
        fc.integer({ min: 0, max: 999 }).filter(n => {
          const s = n.toString();
          return s !== '24' && s !== '32' && s !== '100';
        }),
        async (formatCode) => {
          const parser = new KittyGraphicsParser();
          // Use a valid PNG image
          const validPNG = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFBQIAX8jx0gAAAABJRU5ErkJggg==';
          
          // Should throw error for unsupported format code
          await expect(
            parser.decodeImageData(validPNG, formatCode.toString())
          ).rejects.toThrow('Unsupported image format');
        }
      ),
      { numRuns: 100 }
    );
  });
});
