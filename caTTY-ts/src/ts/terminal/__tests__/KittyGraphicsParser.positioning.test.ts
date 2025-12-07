/**
 * Property-based tests for Kitty Graphics Protocol image placement positioning.
 * These tests verify that image placements are correctly positioned and sized.
 * 
 * @vitest-environment jsdom
 */

import { describe, it, expect, beforeAll } from 'vitest';
import * as fc from 'fast-check';
import { KittyGraphicsParser } from '../graphics/KittyGraphicsParser.js';
import type { ImageData, GraphicsParams } from '../types.js';

describe('KittyGraphicsParser Positioning Tests', () => {
  // Mock ImageBitmap for testing
  beforeAll(() => {
    if (!globalThis.ImageBitmap) {
      (globalThis as any).ImageBitmap = class ImageBitmap {
        width: number;
        height: number;
        
        constructor(width = 1, height = 1) {
          this.width = width;
          this.height = height;
        }
        
        close() {}
      };
    }
  });

  /**
   * Feature: headless-terminal-emulator, Property 79: Grid coordinate positioning
   * For any image placement with specified rows and columns, the image should appear at those exact grid coordinates
   * Validates: Requirements 27.1
   */
  it('Property 79: Grid coordinate positioning', () => {
    fc.assert(
      fc.property(
        // Generate cursor position
        fc.integer({ min: 0, max: 200 }),
        fc.integer({ min: 0, max: 200 }),
        // Generate explicit grid coordinates
        fc.option(fc.integer({ min: 0, max: 200 }), { nil: undefined }),
        fc.option(fc.integer({ min: 0, max: 200 }), { nil: undefined }),
        // Generate image ID
        fc.integer({ min: 1, max: 10000 }),
        (cursorRow, cursorCol, explicitRow, explicitCol, imageId) => {
          const parser = new KittyGraphicsParser();
          
          // Create params with explicit coordinates
          const params: GraphicsParams = {
            action: 'd',
            imageId,
            y: explicitRow,
            x: explicitCol,
          };
          
          // Handle display
          const placement = parser.handleDisplay(params, cursorRow, cursorCol);
          
          // Verify placement uses explicit coordinates if provided, otherwise cursor position
          expect(placement.row).toBe(explicitRow !== undefined ? explicitRow : cursorRow);
          expect(placement.col).toBe(explicitCol !== undefined ? explicitCol : cursorCol);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 80: Pixel to cell conversion
   * For any image placement with pixel dimensions, the terminal should convert them to cell dimensions based on cell size
   * Validates: Requirements 27.2
   */
  it('Property 80: Pixel to cell conversion', () => {
    fc.assert(
      fc.property(
        // Generate pixel dimensions
        fc.integer({ min: 1, max: 2000 }),
        fc.integer({ min: 1, max: 2000 }),
        // Generate cell dimensions
        fc.integer({ min: 1, max: 50 }),
        fc.integer({ min: 1, max: 50 }),
        // Generate cursor position
        fc.integer({ min: 0, max: 200 }),
        fc.integer({ min: 0, max: 200 }),
        // Generate image ID
        fc.integer({ min: 1, max: 10000 }),
        (pixelWidth, pixelHeight, cellWidth, cellHeight, cursorRow, cursorCol, imageId) => {
          const parser = new KittyGraphicsParser();
          
          // Create params with pixel dimensions
          const params: GraphicsParams = {
            action: 'd',
            imageId,
            width: pixelWidth,
            height: pixelHeight,
          };
          
          // Handle display with cell dimensions
          const placement = parser.handleDisplay(
            params,
            cursorRow,
            cursorCol,
            undefined,
            cellWidth,
            cellHeight
          );
          
          // Calculate expected cell dimensions (rounded up)
          const expectedWidth = Math.ceil(pixelWidth / cellWidth);
          const expectedHeight = Math.ceil(pixelHeight / cellHeight);
          
          // Verify conversion
          expect(placement.width).toBe(expectedWidth);
          expect(placement.height).toBe(expectedHeight);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 80: Explicit cell dimensions take precedence
   * For any placement with both pixel and cell dimensions, cell dimensions should be used
   * Validates: Requirements 27.2
   */
  it('Property 80: Explicit cell dimensions take precedence over pixel dimensions', () => {
    fc.assert(
      fc.property(
        // Generate both pixel and cell dimensions
        fc.integer({ min: 1, max: 2000 }),
        fc.integer({ min: 1, max: 2000 }),
        fc.integer({ min: 1, max: 100 }),
        fc.integer({ min: 1, max: 100 }),
        // Generate cell size
        fc.integer({ min: 1, max: 50 }),
        fc.integer({ min: 1, max: 50 }),
        // Generate cursor position
        fc.integer({ min: 0, max: 200 }),
        fc.integer({ min: 0, max: 200 }),
        // Generate image ID
        fc.integer({ min: 1, max: 10000 }),
        (pixelWidth, pixelHeight, cellCols, cellRows, cellWidth, cellHeight, cursorRow, cursorCol, imageId) => {
          const parser = new KittyGraphicsParser();
          
          // Create params with both pixel and cell dimensions
          const params: GraphicsParams = {
            action: 'd',
            imageId,
            width: pixelWidth,
            height: pixelHeight,
            cols: cellCols,
            rows: cellRows,
          };
          
          // Handle display
          const placement = parser.handleDisplay(
            params,
            cursorRow,
            cursorCol,
            undefined,
            cellWidth,
            cellHeight
          );
          
          // Verify cell dimensions take precedence
          expect(placement.width).toBe(cellCols);
          expect(placement.height).toBe(cellRows);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 81: Source rectangle cropping
   * For any image placement with a source rectangle, only that region of the image should be displayed
   * Validates: Requirements 27.3
   */
  it('Property 81: Source rectangle cropping', () => {
    fc.assert(
      fc.property(
        // Generate source rectangle parameters
        fc.integer({ min: 0, max: 2000 }),
        fc.integer({ min: 0, max: 2000 }),
        fc.integer({ min: 1, max: 2000 }),
        fc.integer({ min: 1, max: 2000 }),
        // Generate cursor position
        fc.integer({ min: 0, max: 200 }),
        fc.integer({ min: 0, max: 200 }),
        // Generate image ID
        fc.integer({ min: 1, max: 10000 }),
        (sourceX, sourceY, sourceWidth, sourceHeight, cursorRow, cursorCol, imageId) => {
          const parser = new KittyGraphicsParser();
          
          // Create params with source rectangle
          const params: GraphicsParams = {
            action: 'd',
            imageId,
            sourceX,
            sourceY,
            sourceWidth,
            sourceHeight,
          };
          
          // Handle display
          const placement = parser.handleDisplay(params, cursorRow, cursorCol);
          
          // Verify source rectangle is preserved
          expect(placement.sourceX).toBe(sourceX);
          expect(placement.sourceY).toBe(sourceY);
          expect(placement.sourceWidth).toBe(sourceWidth);
          expect(placement.sourceHeight).toBe(sourceHeight);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 82: Native dimension fallback
   * For any image placement without specified dimensions, the terminal should use the image's native dimensions
   * Validates: Requirements 27.4
   */
  it('Property 82: Native dimension fallback', () => {
    fc.assert(
      fc.property(
        // Generate image dimensions
        fc.integer({ min: 1, max: 2000 }),
        fc.integer({ min: 1, max: 2000 }),
        // Generate cell dimensions
        fc.integer({ min: 1, max: 50 }),
        fc.integer({ min: 1, max: 50 }),
        // Generate cursor position
        fc.integer({ min: 0, max: 200 }),
        fc.integer({ min: 0, max: 200 }),
        // Generate image ID
        fc.integer({ min: 1, max: 10000 }),
        (imageWidth, imageHeight, cellWidth, cellHeight, cursorRow, cursorCol, imageId) => {
          const parser = new KittyGraphicsParser();
          
          // Create mock image data
          const mockBitmap = new (globalThis as any).ImageBitmap(imageWidth, imageHeight);
          const imageData: ImageData = {
            id: imageId,
            data: mockBitmap,
            width: imageWidth,
            height: imageHeight,
            format: 'png',
            hasAlpha: true,
          };
          
          // Create params without dimensions
          const params: GraphicsParams = {
            action: 'd',
            imageId,
          };
          
          // Handle display with image data
          const placement = parser.handleDisplay(
            params,
            cursorRow,
            cursorCol,
            imageData,
            cellWidth,
            cellHeight
          );
          
          // Calculate expected cell dimensions from native image dimensions
          const expectedWidth = Math.ceil(imageWidth / cellWidth);
          const expectedHeight = Math.ceil(imageHeight / cellHeight);
          
          // Verify native dimensions are used
          expect(placement.width).toBe(expectedWidth);
          expect(placement.height).toBe(expectedHeight);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 82: Native dimension fallback without cell size
   * For any image without dimensions or cell size, use reasonable defaults
   * Validates: Requirements 27.4
   */
  it('Property 82: Native dimension fallback without cell size uses defaults', () => {
    fc.assert(
      fc.property(
        // Generate image dimensions
        fc.integer({ min: 1, max: 2000 }),
        fc.integer({ min: 1, max: 2000 }),
        // Generate cursor position
        fc.integer({ min: 0, max: 200 }),
        fc.integer({ min: 0, max: 200 }),
        // Generate image ID
        fc.integer({ min: 1, max: 10000 }),
        (imageWidth, imageHeight, cursorRow, cursorCol, imageId) => {
          const parser = new KittyGraphicsParser();
          
          // Create mock image data
          const mockBitmap = new (globalThis as any).ImageBitmap(imageWidth, imageHeight);
          const imageData: ImageData = {
            id: imageId,
            data: mockBitmap,
            width: imageWidth,
            height: imageHeight,
            format: 'png',
            hasAlpha: true,
          };
          
          // Create params without dimensions
          const params: GraphicsParams = {
            action: 'd',
            imageId,
          };
          
          // Handle display without cell dimensions (should use fallback)
          const placement = parser.handleDisplay(
            params,
            cursorRow,
            cursorCol,
            imageData
          );
          
          // Verify dimensions are calculated (using fallback of 10px/cell width, 20px/cell height)
          const expectedWidth = Math.ceil(imageWidth / 10);
          const expectedHeight = Math.ceil(imageHeight / 20);
          
          expect(placement.width).toBe(expectedWidth);
          expect(placement.height).toBe(expectedHeight);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 83: Screen boundary clipping
   * For any image placement that extends beyond screen boundaries, the image should be clipped at the edges
   * Validates: Requirements 27.5
   */
  it('Property 83: Screen boundary clipping', () => {
    fc.assert(
      fc.property(
        // Generate screen dimensions
        fc.integer({ min: 10, max: 200 }),
        fc.integer({ min: 10, max: 200 }),
        // Generate placement position (may be near edge)
        fc.integer({ min: 0, max: 200 }),
        fc.integer({ min: 0, max: 200 }),
        // Generate placement dimensions (may exceed screen)
        fc.integer({ min: 1, max: 100 }),
        fc.integer({ min: 1, max: 100 }),
        // Generate image ID
        fc.integer({ min: 1, max: 10000 }),
        (screenCols, screenRows, col, row, width, height, imageId) => {
          const parser = new KittyGraphicsParser();
          
          // Create params with dimensions
          const params: GraphicsParams = {
            action: 'd',
            imageId,
            x: col,
            y: row,
            cols: width,
            rows: height,
          };
          
          // Handle display with screen boundaries
          const placement = parser.handleDisplay(
            params,
            0,
            0,
            undefined,
            undefined,
            undefined,
            screenCols,
            screenRows
          );
          
          // Verify position is preserved
          expect(placement.row).toBe(row);
          expect(placement.col).toBe(col);
          
          // Verify dimensions are clipped to screen boundaries
          const maxWidth = Math.max(0, screenCols - col);
          const maxHeight = Math.max(0, screenRows - row);
          
          expect(placement.width).toBeLessThanOrEqual(maxWidth);
          expect(placement.height).toBeLessThanOrEqual(maxHeight);
          
          // Verify dimensions are non-negative
          expect(placement.width).toBeGreaterThanOrEqual(0);
          expect(placement.height).toBeGreaterThanOrEqual(0);
          
          // If placement is within bounds, dimensions should match requested
          if (col < screenCols && col + width <= screenCols) {
            expect(placement.width).toBe(width);
          } else {
            expect(placement.width).toBe(maxWidth);
          }
          
          if (row < screenRows && row + height <= screenRows) {
            expect(placement.height).toBe(height);
          } else {
            expect(placement.height).toBe(maxHeight);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 83: Screen boundary clipping with position beyond screen
   * For any placement starting beyond screen boundaries, dimensions should be zero
   * Validates: Requirements 27.5
   */
  it('Property 83: Screen boundary clipping handles positions beyond screen', () => {
    fc.assert(
      fc.property(
        // Generate screen dimensions
        fc.integer({ min: 10, max: 100 }),
        fc.integer({ min: 10, max: 100 }),
        // Generate position beyond screen
        fc.integer({ min: 101, max: 200 }),
        fc.integer({ min: 101, max: 200 }),
        // Generate dimensions
        fc.integer({ min: 1, max: 50 }),
        fc.integer({ min: 1, max: 50 }),
        // Generate image ID
        fc.integer({ min: 1, max: 10000 }),
        (screenCols, screenRows, col, row, width, height, imageId) => {
          const parser = new KittyGraphicsParser();
          
          // Create params with position beyond screen
          const params: GraphicsParams = {
            action: 'd',
            imageId,
            x: col,
            y: row,
            cols: width,
            rows: height,
          };
          
          // Handle display with screen boundaries
          const placement = parser.handleDisplay(
            params,
            0,
            0,
            undefined,
            undefined,
            undefined,
            screenCols,
            screenRows
          );
          
          // Verify dimensions are clipped to zero when position is beyond screen
          if (col >= screenCols) {
            expect(placement.width).toBe(0);
          }
          if (row >= screenRows) {
            expect(placement.height).toBe(0);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 79-83: Complete positioning workflow
   * For any placement with various parameter combinations, all positioning logic should work together correctly
   * Validates: Requirements 27.1, 27.2, 27.3, 27.4, 27.5
   */
  it('Property 79-83: Complete positioning workflow', () => {
    fc.assert(
      fc.property(
        // Generate all possible parameters
        fc.record({
          // Screen dimensions
          screenCols: fc.integer({ min: 20, max: 200 }),
          screenRows: fc.integer({ min: 20, max: 200 }),
          // Cell dimensions
          cellWidth: fc.integer({ min: 5, max: 20 }),
          cellHeight: fc.integer({ min: 10, max: 30 }),
          // Cursor position
          cursorRow: fc.integer({ min: 0, max: 50 }),
          cursorCol: fc.integer({ min: 0, max: 50 }),
          // Image data
          imageWidth: fc.integer({ min: 10, max: 1000 }),
          imageHeight: fc.integer({ min: 10, max: 1000 }),
          imageId: fc.integer({ min: 1, max: 10000 }),
          // Placement parameters (all optional)
          explicitRow: fc.option(fc.integer({ min: 0, max: 100 }), { nil: undefined }),
          explicitCol: fc.option(fc.integer({ min: 0, max: 100 }), { nil: undefined }),
          pixelWidth: fc.option(fc.integer({ min: 10, max: 1000 }), { nil: undefined }),
          pixelHeight: fc.option(fc.integer({ min: 10, max: 1000 }), { nil: undefined }),
          cellCols: fc.option(fc.integer({ min: 1, max: 50 }), { nil: undefined }),
          cellRows: fc.option(fc.integer({ min: 1, max: 50 }), { nil: undefined }),
          sourceX: fc.option(fc.integer({ min: 0, max: 500 }), { nil: undefined }),
          sourceY: fc.option(fc.integer({ min: 0, max: 500 }), { nil: undefined }),
          sourceWidth: fc.option(fc.integer({ min: 1, max: 500 }), { nil: undefined }),
          sourceHeight: fc.option(fc.integer({ min: 1, max: 500 }), { nil: undefined }),
        }),
        (config) => {
          const parser = new KittyGraphicsParser();
          
          // Create mock image data
          const mockBitmap = new (globalThis as any).ImageBitmap(config.imageWidth, config.imageHeight);
          const imageData: ImageData = {
            id: config.imageId,
            data: mockBitmap,
            width: config.imageWidth,
            height: config.imageHeight,
            format: 'png',
            hasAlpha: true,
          };
          
          // Create params
          const params: GraphicsParams = {
            action: 'd',
            imageId: config.imageId,
            x: config.explicitCol,
            y: config.explicitRow,
            width: config.pixelWidth,
            height: config.pixelHeight,
            cols: config.cellCols,
            rows: config.cellRows,
            sourceX: config.sourceX,
            sourceY: config.sourceY,
            sourceWidth: config.sourceWidth,
            sourceHeight: config.sourceHeight,
          };
          
          // Handle display
          const placement = parser.handleDisplay(
            params,
            config.cursorRow,
            config.cursorCol,
            imageData,
            config.cellWidth,
            config.cellHeight,
            config.screenCols,
            config.screenRows
          );
          
          // Verify position (Property 79)
          const expectedRow = config.explicitRow !== undefined ? config.explicitRow : config.cursorRow;
          const expectedCol = config.explicitCol !== undefined ? config.explicitCol : config.cursorCol;
          expect(placement.row).toBe(expectedRow);
          expect(placement.col).toBe(expectedCol);
          
          // Verify dimensions are calculated correctly (Properties 80, 82)
          expect(placement.width).toBeGreaterThanOrEqual(0);
          expect(placement.height).toBeGreaterThanOrEqual(0);
          
          // Verify source rectangle is preserved (Property 81)
          if (config.sourceX !== undefined) expect(placement.sourceX).toBe(config.sourceX);
          if (config.sourceY !== undefined) expect(placement.sourceY).toBe(config.sourceY);
          if (config.sourceWidth !== undefined) expect(placement.sourceWidth).toBe(config.sourceWidth);
          if (config.sourceHeight !== undefined) expect(placement.sourceHeight).toBe(config.sourceHeight);
          
          // Verify screen boundary clipping (Property 83)
          const maxWidth = config.screenCols - expectedCol;
          const maxHeight = config.screenRows - expectedRow;
          expect(placement.width).toBeLessThanOrEqual(Math.max(0, maxWidth));
          expect(placement.height).toBeLessThanOrEqual(Math.max(0, maxHeight));
        }
      ),
      { numRuns: 100 }
    );
  });
});
