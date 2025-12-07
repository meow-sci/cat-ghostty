/**
 * Property-based tests for ImageManager.
 * These tests verify universal properties of image and placement storage.
 * 
 * @vitest-environment jsdom
 */

import { describe, it, expect, beforeAll } from 'vitest';
import * as fc from 'fast-check';
import { ImageManager } from '../graphics/ImageManager.js';
import type { ImagePlacement } from '../types.js';

describe('ImageManager Property Tests', () => {
  // Mock ImageBitmap for testing (not available in jsdom by default)
  beforeAll(() => {
    // Define ImageBitmap constructor for instanceof checks
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
   * Feature: headless-terminal-emulator, Property 77: Image storage with ID
   * For any image transmitted with an ID, the terminal should store it such that it can be retrieved by that ID
   * Validates: Requirements 26.3, 34.1
   */
  it('Property 77: Image storage with ID', () => {
    fc.assert(
      fc.property(
        // Generate random image IDs
        fc.integer({ min: 1, max: 10000 }),
        // Generate random image dimensions
        fc.integer({ min: 1, max: 2000 }),
        fc.integer({ min: 1, max: 2000 }),
        // Generate random format
        fc.constantFrom('png', 'jpeg', 'gif'),
        (imageId, width, height, format) => {
          const manager = new ImageManager();
          
          // Create a mock ImageBitmap with the specified dimensions
          const mockBitmap = new (globalThis as any).ImageBitmap(width, height);
          
          // Store the image
          manager.storeImage(imageId, mockBitmap, format, width, height, true);
          
          // Retrieve the image
          const retrieved = manager.getImage(imageId);
          
          // Verify the image was stored and can be retrieved
          expect(retrieved).toBeDefined();
          expect(retrieved?.id).toBe(imageId);
          expect(retrieved?.data).toBe(mockBitmap);
          expect(retrieved?.format).toBe(format);
          expect(retrieved?.width).toBe(width);
          expect(retrieved?.height).toBe(height);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 77: Image ID reuse replaces data
   * For any image ID reused, the previous image data should be replaced with new data
   * Validates: Requirements 34.1
   */
  it('Property 77: Image ID reuse replaces data', () => {
    fc.assert(
      fc.property(
        // Generate a single image ID
        fc.integer({ min: 1, max: 10000 }),
        // Generate two different sets of image properties
        fc.tuple(
          fc.integer({ min: 1, max: 2000 }),
          fc.integer({ min: 1, max: 2000 }),
          fc.constantFrom('png', 'jpeg', 'gif')
        ),
        fc.tuple(
          fc.integer({ min: 1, max: 2000 }),
          fc.integer({ min: 1, max: 2000 }),
          fc.constantFrom('png', 'jpeg', 'gif')
        ),
        (imageId, [width1, height1, format1], [width2, height2, format2]) => {
          const manager = new ImageManager();
          
          // Store first image
          const mockBitmap1 = new (globalThis as any).ImageBitmap(width1, height1);
          manager.storeImage(imageId, mockBitmap1, format1, width1, height1, true);
          
          // Store second image with same ID
          const mockBitmap2 = new (globalThis as any).ImageBitmap(width2, height2);
          manager.storeImage(imageId, mockBitmap2, format2, width2, height2, true);
          
          // Retrieve the image
          const retrieved = manager.getImage(imageId);
          
          // Verify the second image replaced the first
          expect(retrieved).toBeDefined();
          expect(retrieved?.data).toBe(mockBitmap2);
          expect(retrieved?.format).toBe(format2);
          expect(retrieved?.width).toBe(width2);
          expect(retrieved?.height).toBe(height2);
          
          // Verify it's not the first image
          expect(retrieved?.data).not.toBe(mockBitmap1);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 77: Multiple images with different IDs
   * For any set of images with different IDs, all should be stored and retrievable independently
   * Validates: Requirements 26.3, 34.1
   */
  it('Property 77: Multiple images with different IDs', () => {
    fc.assert(
      fc.property(
        // Generate an array of unique image IDs with their properties
        fc.uniqueArray(
          fc.record({
            id: fc.integer({ min: 1, max: 10000 }),
            width: fc.integer({ min: 1, max: 2000 }),
            height: fc.integer({ min: 1, max: 2000 }),
            format: fc.constantFrom('png', 'jpeg', 'gif')
          }),
          { 
            minLength: 1, 
            maxLength: 20,
            selector: (item) => item.id
          }
        ),
        (images) => {
          const manager = new ImageManager();
          const bitmaps = new Map<number, any>();
          
          // Store all images
          for (const img of images) {
            const mockBitmap = new (globalThis as any).ImageBitmap(img.width, img.height);
            bitmaps.set(img.id, mockBitmap);
            manager.storeImage(img.id, mockBitmap, img.format, img.width, img.height, true);
          }
          
          // Verify all images can be retrieved independently
          for (const img of images) {
            const retrieved = manager.getImage(img.id);
            expect(retrieved).toBeDefined();
            expect(retrieved?.id).toBe(img.id);
            expect(retrieved?.data).toBe(bitmaps.get(img.id));
            expect(retrieved?.format).toBe(img.format);
            expect(retrieved?.width).toBe(img.width);
            expect(retrieved?.height).toBe(img.height);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 78: Placement creation at cursor
   * For any display command, the terminal should create an image placement at the current cursor position
   * Validates: Requirements 26.4, 26.5
   */
  it('Property 78: Placement creation at cursor', () => {
    fc.assert(
      fc.property(
        // Generate random placement parameters
        fc.integer({ min: 1, max: 10000 }), // placementId
        fc.integer({ min: 1, max: 10000 }), // imageId
        fc.integer({ min: 0, max: 200 }),   // row (cursor position)
        fc.integer({ min: 0, max: 200 }),   // col (cursor position)
        fc.integer({ min: 1, max: 100 }),   // width in cells
        fc.integer({ min: 1, max: 100 }),   // height in cells
        (placementId, imageId, row, col, width, height) => {
          const manager = new ImageManager();
          
          // Create a placement at the cursor position
          const placement: ImagePlacement = {
            placementId,
            imageId,
            row,
            col,
            width,
            height
          };
          
          manager.createPlacement(placement);
          
          // Verify the placement was created at the correct position
          const retrieved = manager.getPlacement(placementId);
          expect(retrieved).toBeDefined();
          expect(retrieved?.placementId).toBe(placementId);
          expect(retrieved?.imageId).toBe(imageId);
          expect(retrieved?.row).toBe(row);
          expect(retrieved?.col).toBe(col);
          expect(retrieved?.width).toBe(width);
          expect(retrieved?.height).toBe(height);
          
          // Verify it's in the visible placements list
          const visible = manager.getVisiblePlacements();
          expect(visible).toContainEqual(placement);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 78: Placement with optional parameters
   * For any placement with optional parameters (source rectangle, zIndex, unicode placeholder),
   * all parameters should be preserved
   * Validates: Requirements 26.4, 26.5
   */
  it('Property 78: Placement with optional parameters', () => {
    fc.assert(
      fc.property(
        // Generate random placement parameters including optional ones
        fc.integer({ min: 1, max: 10000 }), // placementId
        fc.integer({ min: 1, max: 10000 }), // imageId
        fc.integer({ min: 0, max: 200 }),   // row
        fc.integer({ min: 0, max: 200 }),   // col
        fc.integer({ min: 1, max: 100 }),   // width
        fc.integer({ min: 1, max: 100 }),   // height
        fc.option(fc.integer({ min: 0, max: 2000 }), { nil: undefined }), // sourceX
        fc.option(fc.integer({ min: 0, max: 2000 }), { nil: undefined }), // sourceY
        fc.option(fc.integer({ min: 1, max: 2000 }), { nil: undefined }), // sourceWidth
        fc.option(fc.integer({ min: 1, max: 2000 }), { nil: undefined }), // sourceHeight
        fc.option(fc.integer({ min: -100, max: 100 }), { nil: undefined }), // zIndex
        fc.option(fc.string({ minLength: 1, maxLength: 10 }), { nil: undefined }), // unicodePlaceholder
        (placementId, imageId, row, col, width, height, sourceX, sourceY, sourceWidth, sourceHeight, zIndex, unicodePlaceholder) => {
          const manager = new ImageManager();
          
          // Create a placement with optional parameters
          const placement: ImagePlacement = {
            placementId,
            imageId,
            row,
            col,
            width,
            height,
            ...(sourceX !== undefined && { sourceX }),
            ...(sourceY !== undefined && { sourceY }),
            ...(sourceWidth !== undefined && { sourceWidth }),
            ...(sourceHeight !== undefined && { sourceHeight }),
            ...(zIndex !== undefined && { zIndex }),
            ...(unicodePlaceholder !== undefined && { unicodePlaceholder })
          };
          
          manager.createPlacement(placement);
          
          // Verify all parameters were preserved
          const retrieved = manager.getPlacement(placementId);
          expect(retrieved).toBeDefined();
          expect(retrieved).toEqual(placement);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 78: Placement ID reuse replaces placement
   * For any placement ID reused, the previous placement should be replaced with the new placement
   * Validates: Requirements 26.4, 26.5
   */
  it('Property 78: Placement ID reuse replaces placement', () => {
    fc.assert(
      fc.property(
        // Generate a single placement ID
        fc.integer({ min: 1, max: 10000 }),
        // Generate two different sets of placement properties
        fc.tuple(
          fc.integer({ min: 1, max: 10000 }), // imageId1
          fc.integer({ min: 0, max: 200 }),   // row1
          fc.integer({ min: 0, max: 200 }),   // col1
          fc.integer({ min: 1, max: 100 }),   // width1
          fc.integer({ min: 1, max: 100 })    // height1
        ),
        fc.tuple(
          fc.integer({ min: 1, max: 10000 }), // imageId2
          fc.integer({ min: 0, max: 200 }),   // row2
          fc.integer({ min: 0, max: 200 }),   // col2
          fc.integer({ min: 1, max: 100 }),   // width2
          fc.integer({ min: 1, max: 100 })    // height2
        ),
        (placementId, [imageId1, row1, col1, width1, height1], [imageId2, row2, col2, width2, height2]) => {
          const manager = new ImageManager();
          
          // Create first placement
          const placement1: ImagePlacement = {
            placementId,
            imageId: imageId1,
            row: row1,
            col: col1,
            width: width1,
            height: height1
          };
          manager.createPlacement(placement1);
          
          // Create second placement with same ID
          const placement2: ImagePlacement = {
            placementId,
            imageId: imageId2,
            row: row2,
            col: col2,
            width: width2,
            height: height2
          };
          manager.createPlacement(placement2);
          
          // Verify the second placement replaced the first
          const retrieved = manager.getPlacement(placementId);
          expect(retrieved).toBeDefined();
          expect(retrieved).toEqual(placement2);
          
          // Verify only one placement exists in visible list
          const visible = manager.getVisiblePlacements();
          expect(visible).toHaveLength(1);
          expect(visible[0]).toEqual(placement2);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 78: Multiple placements for same image
   * For any image, multiple placements with different IDs should be independently managed
   * Validates: Requirements 26.4, 26.5
   */
  it('Property 78: Multiple placements for same image', () => {
    fc.assert(
      fc.property(
        // Generate a single image ID
        fc.integer({ min: 1, max: 10000 }),
        // Generate multiple unique placements for that image
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 200 }),
            col: fc.integer({ min: 0, max: 200 }),
            width: fc.integer({ min: 1, max: 100 }),
            height: fc.integer({ min: 1, max: 100 })
          }),
          {
            minLength: 1,
            maxLength: 10,
            selector: (item) => item.placementId
          }
        ),
        (imageId, placements) => {
          const manager = new ImageManager();
          
          // Create all placements for the same image
          const createdPlacements: ImagePlacement[] = [];
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
            createdPlacements.push(placement);
          }
          
          // Verify all placements exist independently
          for (const placement of createdPlacements) {
            const retrieved = manager.getPlacement(placement.placementId);
            expect(retrieved).toBeDefined();
            expect(retrieved).toEqual(placement);
          }
          
          // Verify all are in visible placements
          const visible = manager.getVisiblePlacements();
          expect(visible).toHaveLength(createdPlacements.length);
          
          for (const placement of createdPlacements) {
            expect(visible).toContainEqual(placement);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 77 & 78: Image deletion removes all placements
   * For any image with multiple placements, deleting the image should remove all its placements
   * Validates: Requirements 26.3, 29.1
   */
  it('Property 77 & 78: Image deletion removes all placements', () => {
    fc.assert(
      fc.property(
        // Generate an image ID
        fc.integer({ min: 1, max: 10000 }),
        // Generate image properties
        fc.integer({ min: 1, max: 2000 }),
        fc.integer({ min: 1, max: 2000 }),
        fc.constantFrom('png', 'jpeg', 'gif'),
        // Generate multiple placements for that image
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 200 }),
            col: fc.integer({ min: 0, max: 200 }),
            width: fc.integer({ min: 1, max: 100 }),
            height: fc.integer({ min: 1, max: 100 })
          }),
          {
            minLength: 1,
            maxLength: 10,
            selector: (item) => item.placementId
          }
        ),
        (imageId, width, height, format, placements) => {
          const manager = new ImageManager();
          
          // Store the image
          const mockBitmap = new (globalThis as any).ImageBitmap(width, height);
          manager.storeImage(imageId, mockBitmap, format, width, height, true);
          
          // Create all placements
          const placementIds: number[] = [];
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
            placementIds.push(p.placementId);
          }
          
          // Delete the image
          manager.deleteImage(imageId);
          
          // Verify the image is deleted
          expect(manager.getImage(imageId)).toBeUndefined();
          
          // Verify all placements are deleted
          for (const placementId of placementIds) {
            expect(manager.getPlacement(placementId)).toBeUndefined();
          }
          
          // Verify visible placements list is empty
          expect(manager.getVisiblePlacements()).toHaveLength(0);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 96: Chunked transmission accumulation
   * For any image transmitted in chunks, the terminal should accumulate all chunks until transmission is complete
   * Validates: Requirements 31.1
   */
  it('Property 96: Chunked transmission accumulation', () => {
    fc.assert(
      fc.property(
        // Generate an image ID
        fc.integer({ min: 1, max: 10000 }),
        // Generate a format
        fc.constantFrom('png', 'jpeg', 'gif'),
        // Generate multiple chunks of data
        fc.array(
          fc.uint8Array({ minLength: 1, maxLength: 1000 }),
          { minLength: 1, maxLength: 20 }
        ),
        (imageId, format, chunks) => {
          const manager = new ImageManager();
          
          // Start transmission
          manager.startTransmission(imageId, format);
          
          // Add all chunks
          for (const chunk of chunks) {
            manager.addChunk(imageId, chunk);
          }
          
          // Verify transmission is tracked
          const transmission = manager.getTransmission(imageId);
          expect(transmission).toBeDefined();
          expect(transmission?.imageId).toBe(imageId);
          expect(transmission?.format).toBe(format);
          expect(transmission?.chunks).toHaveLength(chunks.length);
          expect(transmission?.complete).toBe(false);
          
          // Complete transmission
          const combined = manager.completeTransmission(imageId);
          
          // Verify all chunks were accumulated
          expect(combined).toBeDefined();
          
          // Calculate expected total length
          const expectedLength = chunks.reduce((sum, chunk) => sum + chunk.length, 0);
          expect(combined?.length).toBe(expectedLength);
          
          // Verify data is correctly combined
          let offset = 0;
          for (const chunk of chunks) {
            for (let i = 0; i < chunk.length; i++) {
              expect(combined![offset + i]).toBe(chunk[i]);
            }
            offset += chunk.length;
          }
          
          // Verify transmission is cleaned up after completion
          expect(manager.getTransmission(imageId)).toBeUndefined();
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 97: Non-blocking chunked transmission
   * For any chunked transmission in progress, the terminal should continue processing other output
   * Validates: Requirements 31.2
   */
  it('Property 97: Non-blocking chunked transmission', () => {
    fc.assert(
      fc.property(
        // Generate multiple image IDs for concurrent operations
        fc.uniqueArray(
          fc.integer({ min: 1, max: 10000 }),
          { minLength: 2, maxLength: 5 }
        ),
        // Generate chunks for first transmission
        fc.array(
          fc.uint8Array({ minLength: 1, maxLength: 100 }),
          { minLength: 1, maxLength: 5 }
        ),
        // Generate format
        fc.constantFrom('png', 'jpeg', 'gif'),
        (imageIds, chunks, format) => {
          const manager = new ImageManager();
          
          // Start transmission for first image
          const transmissionId = imageIds[0];
          manager.startTransmission(transmissionId, format);
          
          // Add some chunks but don't complete
          for (let i = 0; i < Math.floor(chunks.length / 2); i++) {
            manager.addChunk(transmissionId, chunks[i]);
          }
          
          // Verify transmission is in progress
          const transmission = manager.getTransmission(transmissionId);
          expect(transmission).toBeDefined();
          expect(transmission?.complete).toBe(false);
          
          // Perform other operations while transmission is in progress
          // Store other images
          for (let i = 1; i < imageIds.length; i++) {
            const mockBitmap = new (globalThis as any).ImageBitmap(100, 100);
            manager.storeImage(imageIds[i], mockBitmap, format, 100, 100, true);
          }
          
          // Verify other images were stored successfully
          for (let i = 1; i < imageIds.length; i++) {
            const retrieved = manager.getImage(imageIds[i]);
            expect(retrieved).toBeDefined();
            expect(retrieved?.id).toBe(imageIds[i]);
          }
          
          // Verify transmission is still in progress
          const stillInProgress = manager.getTransmission(transmissionId);
          expect(stillInProgress).toBeDefined();
          expect(stillInProgress?.complete).toBe(false);
          
          // Complete the transmission
          for (let i = Math.floor(chunks.length / 2); i < chunks.length; i++) {
            manager.addChunk(transmissionId, chunks[i]);
          }
          const combined = manager.completeTransmission(transmissionId);
          
          // Verify transmission completed successfully
          expect(combined).toBeDefined();
          
          // Verify other images are still accessible
          for (let i = 1; i < imageIds.length; i++) {
            const retrieved = manager.getImage(imageIds[i]);
            expect(retrieved).toBeDefined();
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 98: Transmission completion finalization
   * For any transmission marked complete, the terminal should finalize the image and make it available for placement
   * Validates: Requirements 31.3
   */
  it('Property 98: Transmission completion finalization', () => {
    fc.assert(
      fc.property(
        // Generate an image ID
        fc.integer({ min: 1, max: 10000 }),
        // Generate a format
        fc.constantFrom('png', 'jpeg', 'gif'),
        // Generate chunks
        fc.array(
          fc.uint8Array({ minLength: 1, maxLength: 500 }),
          { minLength: 1, maxLength: 10 }
        ),
        (imageId, format, chunks) => {
          const manager = new ImageManager();
          
          // Start transmission
          manager.startTransmission(imageId, format);
          
          // Verify transmission exists but is not complete
          let transmission = manager.getTransmission(imageId);
          expect(transmission).toBeDefined();
          expect(transmission?.complete).toBe(false);
          
          // Add all chunks
          for (const chunk of chunks) {
            manager.addChunk(imageId, chunk);
          }
          
          // Complete transmission
          const combined = manager.completeTransmission(imageId);
          
          // Verify combined data is returned
          expect(combined).toBeDefined();
          expect(combined).toBeInstanceOf(Uint8Array);
          
          // Verify transmission is cleaned up (no longer tracked)
          transmission = manager.getTransmission(imageId);
          expect(transmission).toBeUndefined();
          
          // Verify the combined data has correct length
          const expectedLength = chunks.reduce((sum, chunk) => sum + chunk.length, 0);
          expect(combined?.length).toBe(expectedLength);
          
          // The combined data should now be available for decoding and storage
          // (In real usage, this would be passed to decodeImageData and then storeImage)
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 99: Transmission failure cleanup
   * For any failed transmission, the terminal should discard partial data and emit an error event
   * Validates: Requirements 31.4
   */
  it('Property 99: Transmission failure cleanup', () => {
    fc.assert(
      fc.property(
        // Generate an image ID
        fc.integer({ min: 1, max: 10000 }),
        // Generate a format
        fc.constantFrom('png', 'jpeg', 'gif'),
        // Generate some chunks
        fc.array(
          fc.uint8Array({ minLength: 1, maxLength: 500 }),
          { minLength: 1, maxLength: 10 }
        ),
        (imageId, format, chunks) => {
          const manager = new ImageManager();
          
          // Start transmission
          manager.startTransmission(imageId, format);
          
          // Add some chunks
          for (const chunk of chunks) {
            manager.addChunk(imageId, chunk);
          }
          
          // Verify transmission exists with accumulated chunks
          let transmission = manager.getTransmission(imageId);
          expect(transmission).toBeDefined();
          expect(transmission?.chunks.length).toBe(chunks.length);
          
          // Cancel the transmission (simulating a failure)
          manager.cancelTransmission(imageId);
          
          // Verify transmission is completely removed
          transmission = manager.getTransmission(imageId);
          expect(transmission).toBeUndefined();
          
          // Verify attempting to complete a cancelled transmission returns undefined
          const result = manager.completeTransmission(imageId);
          expect(result).toBeUndefined();
          
          // Verify we can start a new transmission with the same ID after cancellation
          manager.startTransmission(imageId, format);
          transmission = manager.getTransmission(imageId);
          expect(transmission).toBeDefined();
          expect(transmission?.chunks).toHaveLength(0);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 100: Concurrent transmission independence
   * For any multiple concurrent image transmissions, each should be handled independently
   * Validates: Requirements 31.5
   */
  it('Property 100: Concurrent transmission independence', () => {
    fc.assert(
      fc.property(
        // Generate multiple unique image IDs
        fc.uniqueArray(
          fc.integer({ min: 1, max: 10000 }),
          { minLength: 2, maxLength: 5 }
        ),
        // Generate chunks for each transmission
        fc.array(
          fc.record({
            format: fc.constantFrom('png', 'jpeg', 'gif'),
            chunks: fc.array(
              fc.uint8Array({ minLength: 1, maxLength: 200 }),
              { minLength: 1, maxLength: 5 }
            )
          }),
          { minLength: 2, maxLength: 5 }
        ),
        (imageIds, transmissionData) => {
          // Ensure we have matching data for each image ID
          const numTransmissions = Math.min(imageIds.length, transmissionData.length);
          const manager = new ImageManager();
          
          // Start all transmissions
          for (let i = 0; i < numTransmissions; i++) {
            manager.startTransmission(imageIds[i], transmissionData[i].format);
          }
          
          // Verify all transmissions are tracked independently
          for (let i = 0; i < numTransmissions; i++) {
            const transmission = manager.getTransmission(imageIds[i]);
            expect(transmission).toBeDefined();
            expect(transmission?.imageId).toBe(imageIds[i]);
            expect(transmission?.format).toBe(transmissionData[i].format);
            expect(transmission?.chunks).toHaveLength(0);
          }
          
          // Add chunks to each transmission in interleaved fashion
          const maxChunks = Math.max(...transmissionData.slice(0, numTransmissions).map(t => t.chunks.length));
          for (let chunkIndex = 0; chunkIndex < maxChunks; chunkIndex++) {
            for (let i = 0; i < numTransmissions; i++) {
              if (chunkIndex < transmissionData[i].chunks.length) {
                manager.addChunk(imageIds[i], transmissionData[i].chunks[chunkIndex]);
              }
            }
          }
          
          // Verify each transmission has accumulated its own chunks independently
          for (let i = 0; i < numTransmissions; i++) {
            const transmission = manager.getTransmission(imageIds[i]);
            expect(transmission).toBeDefined();
            expect(transmission?.chunks.length).toBe(transmissionData[i].chunks.length);
          }
          
          // Complete transmissions in different order
          const completionOrder = [...Array(numTransmissions).keys()].reverse();
          const results: (Uint8Array | undefined)[] = [];
          
          for (const i of completionOrder) {
            const combined = manager.completeTransmission(imageIds[i]);
            results[i] = combined;
            
            // Verify this transmission is cleaned up
            expect(manager.getTransmission(imageIds[i])).toBeUndefined();
            
            // Verify other transmissions are still tracked (if not yet completed)
            for (let j = 0; j < numTransmissions; j++) {
              if (!completionOrder.slice(0, completionOrder.indexOf(i) + 1).includes(j)) {
                expect(manager.getTransmission(imageIds[j])).toBeDefined();
              }
            }
          }
          
          // Verify each result has correct data
          for (let i = 0; i < numTransmissions; i++) {
            expect(results[i]).toBeDefined();
            const expectedLength = transmissionData[i].chunks.reduce(
              (sum, chunk) => sum + chunk.length,
              0
            );
            expect(results[i]?.length).toBe(expectedLength);
          }
          
          // Verify all transmissions are cleaned up
          for (let i = 0; i < numTransmissions; i++) {
            expect(manager.getTransmission(imageIds[i])).toBeUndefined();
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 84: Image scrolling with content
   * For any image placement in a scrolled region, the placement should move with the scrolled content
   * Validates: Requirements 28.1
   */
  it('Property 84: Image scrolling with content', () => {
    fc.assert(
      fc.property(
        // Generate screen dimensions
        fc.integer({ min: 10, max: 100 }), // screenRows
        // Generate initial placement position
        fc.integer({ min: 5, max: 50 }), // initial row (somewhere in middle of screen)
        fc.integer({ min: 0, max: 50 }), // col
        // Generate scroll amount
        fc.integer({ min: 1, max: 10 }), // lines to scroll
        // Generate placement dimensions
        fc.integer({ min: 1, max: 10 }), // width
        fc.integer({ min: 1, max: 10 }), // height
        (screenRows, initialRow, col, scrollLines, width, height) => {
          // Ensure initial row is within screen bounds
          const row = Math.min(initialRow, screenRows - 1);
          
          const manager = new ImageManager();
          
          // Create a placement
          const placement: ImagePlacement = {
            placementId: 1,
            imageId: 1,
            row,
            col,
            width,
            height
          };
          manager.createPlacement(placement);
          
          // Scroll up (content moves up, placements move up)
          manager.handleScroll('up', scrollLines, screenRows, false);
          
          // Verify placement moved with the content
          const retrieved = manager.getPlacement(1);
          
          if (row - scrollLines >= 0) {
            // Placement should still be visible, just moved up
            expect(retrieved).toBeDefined();
            expect(retrieved?.row).toBe(row - scrollLines);
            expect(retrieved?.col).toBe(col);
            
            // Should still be in active placements
            const visible = manager.getVisiblePlacements();
            expect(visible.some(p => p.placementId === 1)).toBe(true);
          } else {
            // Placement scrolled off the top, should be in scrollback
            const scrollback = manager.getScrollbackPlacements();
            expect(scrollback.some(p => p.placementId === 1)).toBe(true);
            
            // Should not be in active placements
            const visible = manager.getVisiblePlacements();
            expect(visible.some(p => p.placementId === 1)).toBe(false);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 85: Scrollback buffer image preservation
   * For any image placement that scrolls off the top, it should be moved to the scrollback buffer
   * Validates: Requirements 28.2
   */
  it('Property 85: Scrollback buffer image preservation', () => {
    fc.assert(
      fc.property(
        // Generate screen dimensions
        fc.integer({ min: 10, max: 100 }), // screenRows
        // Generate initial placement position (near top)
        fc.integer({ min: 0, max: 5 }), // initial row
        fc.integer({ min: 0, max: 50 }), // col
        // Generate scroll amount that will push it off screen
        fc.integer({ min: 6, max: 20 }), // lines to scroll (more than initial row)
        // Generate placement dimensions
        fc.integer({ min: 1, max: 10 }), // width
        fc.integer({ min: 1, max: 10 }), // height
        (screenRows, row, col, scrollLines, width, height) => {
          const manager = new ImageManager();
          
          // Create a placement near the top
          const placement: ImagePlacement = {
            placementId: 1,
            imageId: 1,
            row,
            col,
            width,
            height
          };
          manager.createPlacement(placement);
          
          // Verify it's initially in active placements
          expect(manager.getVisiblePlacements()).toHaveLength(1);
          expect(manager.getScrollbackPlacements()).toHaveLength(0);
          
          // Scroll up enough to push it off the top
          manager.handleScroll('up', scrollLines, screenRows, false);
          
          // Verify placement moved to scrollback
          const scrollback = manager.getScrollbackPlacements();
          expect(scrollback.length).toBeGreaterThan(0);
          expect(scrollback.some(p => p.placementId === 1)).toBe(true);
          
          // Verify it's no longer in active placements
          const visible = manager.getVisiblePlacements();
          expect(visible.some(p => p.placementId === 1)).toBe(false);
          
          // Verify the placement still exists in the placements map
          const retrieved = manager.getPlacement(1);
          expect(retrieved).toBeDefined();
          
          // Verify the row is negative (indicating scrollback position)
          const scrollbackPlacement = scrollback.find(p => p.placementId === 1);
          expect(scrollbackPlacement?.row).toBeLessThan(0);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 86: Reverse scroll image removal
   * For any image placement that scrolls off the bottom during reverse scroll, it should be removed
   * Validates: Requirements 28.3
   */
  it('Property 86: Reverse scroll image removal', () => {
    fc.assert(
      fc.property(
        // Generate screen dimensions
        fc.integer({ min: 10, max: 100 }), // screenRows
        // Generate initial placement position (near bottom)
        fc.integer({ min: 0, max: 50 }), // col
        // Generate scroll amount
        fc.integer({ min: 1, max: 20 }), // lines to scroll down
        // Generate placement dimensions
        fc.integer({ min: 1, max: 10 }), // width
        fc.integer({ min: 1, max: 10 }), // height
        (screenRows, col, scrollLines, width, height) => {
          const manager = new ImageManager();
          
          // Create a placement near the bottom
          const row = screenRows - 3; // 3 rows from bottom
          const placement: ImagePlacement = {
            placementId: 1,
            imageId: 1,
            row,
            col,
            width,
            height
          };
          manager.createPlacement(placement);
          
          // Verify it's initially in active placements
          expect(manager.getVisiblePlacements()).toHaveLength(1);
          
          // Scroll down (reverse scroll)
          manager.handleScroll('down', scrollLines, screenRows, false);
          
          const newRow = row + scrollLines;
          
          if (newRow >= screenRows) {
            // Placement scrolled off the bottom, should be removed entirely
            const retrieved = manager.getPlacement(1);
            expect(retrieved).toBeUndefined();
            
            // Should not be in active placements
            const visible = manager.getVisiblePlacements();
            expect(visible.some(p => p.placementId === 1)).toBe(false);
            
            // Should not be in scrollback either
            const scrollback = manager.getScrollbackPlacements();
            expect(scrollback.some(p => p.placementId === 1)).toBe(false);
          } else {
            // Placement should still be visible, just moved down
            const retrieved = manager.getPlacement(1);
            expect(retrieved).toBeDefined();
            expect(retrieved?.row).toBe(newRow);
            
            // Should still be in active placements
            const visible = manager.getVisiblePlacements();
            expect(visible.some(p => p.placementId === 1)).toBe(true);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 87: Scrollback image display
   * For any scrollback view, image placements in the scrollback buffer should be displayed
   * Validates: Requirements 28.4
   */
  it('Property 87: Scrollback image display', () => {
    fc.assert(
      fc.property(
        // Generate screen dimensions
        fc.integer({ min: 10, max: 100 }), // screenRows
        // Generate multiple placements
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 10 }), // near top
            col: fc.integer({ min: 0, max: 50 }),
            width: fc.integer({ min: 1, max: 10 }),
            height: fc.integer({ min: 1, max: 10 })
          }),
          {
            minLength: 1,
            maxLength: 5,
            selector: (item) => item.placementId
          }
        ),
        // Generate scroll amount
        fc.integer({ min: 15, max: 30 }), // enough to push all placements to scrollback
        (screenRows, placements, scrollLines) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.placementId, // use same ID for simplicity
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
          }
          
          // Verify all are initially in active placements
          expect(manager.getVisiblePlacements().length).toBe(placements.length);
          expect(manager.getScrollbackPlacements().length).toBe(0);
          
          // Scroll up to push all placements to scrollback
          manager.handleScroll('up', scrollLines, screenRows, false);
          
          // Verify placements are now in scrollback
          const scrollback = manager.getScrollbackPlacements();
          expect(scrollback.length).toBeGreaterThan(0);
          
          // Verify all placements that scrolled off are in scrollback
          for (const p of placements) {
            const newRow = p.row - scrollLines;
            if (newRow < 0) {
              // Should be in scrollback
              expect(scrollback.some(sp => sp.placementId === p.placementId)).toBe(true);
              
              // Should not be in active placements
              const visible = manager.getVisiblePlacements();
              expect(visible.some(vp => vp.placementId === p.placementId)).toBe(false);
            }
          }
          
          // Verify scrollback placements can be retrieved
          const scrollbackPlacements = manager.getScrollbackPlacements();
          expect(scrollbackPlacements).toBeInstanceOf(Array);
          expect(scrollbackPlacements.length).toBeGreaterThan(0);
          
          // Verify each scrollback placement has correct structure
          for (const sp of scrollbackPlacements) {
            expect(sp).toHaveProperty('placementId');
            expect(sp).toHaveProperty('imageId');
            expect(sp).toHaveProperty('row');
            expect(sp).toHaveProperty('col');
            expect(sp).toHaveProperty('width');
            expect(sp).toHaveProperty('height');
            expect(sp.row).toBeLessThan(0); // scrollback rows are negative
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 88: Alternate screen no image scrollback
   * For any image in alternate screen mode, it should not be preserved in scrollback when scrolled
   * Validates: Requirements 28.5
   */
  it('Property 88: Alternate screen no image scrollback', () => {
    fc.assert(
      fc.property(
        // Generate screen dimensions
        fc.integer({ min: 10, max: 100 }), // screenRows
        // Generate initial placement position
        fc.integer({ min: 0, max: 10 }), // initial row (near top)
        fc.integer({ min: 0, max: 50 }), // col
        // Generate scroll amount that will push it off screen
        fc.integer({ min: 15, max: 30 }), // lines to scroll
        // Generate placement dimensions
        fc.integer({ min: 1, max: 10 }), // width
        fc.integer({ min: 1, max: 10 }), // height
        (screenRows, row, col, scrollLines, width, height) => {
          const manager = new ImageManager();
          
          // Create a placement
          const placement: ImagePlacement = {
            placementId: 1,
            imageId: 1,
            row,
            col,
            width,
            height
          };
          manager.createPlacement(placement);
          
          // Verify it's initially in active placements
          expect(manager.getVisiblePlacements()).toHaveLength(1);
          expect(manager.getScrollbackPlacements()).toHaveLength(0);
          
          // Scroll up in alternate screen mode (isAlternateScreen = true)
          manager.handleScroll('up', scrollLines, screenRows, true);
          
          // Verify placement is NOT in scrollback (alternate screen doesn't preserve scrollback)
          const scrollback = manager.getScrollbackPlacements();
          expect(scrollback.some(p => p.placementId === 1)).toBe(false);
          
          // Verify placement is removed entirely if it scrolled off
          const newRow = row - scrollLines;
          if (newRow < 0) {
            // Should be completely removed, not in scrollback
            const retrieved = manager.getPlacement(1);
            expect(retrieved).toBeUndefined();
            
            // Should not be in active placements
            const visible = manager.getVisiblePlacements();
            expect(visible.some(p => p.placementId === 1)).toBe(false);
            
            // Verify scrollback is still empty
            expect(manager.getScrollbackPlacements()).toHaveLength(0);
          } else {
            // If still visible, should be in active placements
            const retrieved = manager.getPlacement(1);
            expect(retrieved).toBeDefined();
            expect(retrieved?.row).toBe(newRow);
            
            const visible = manager.getVisiblePlacements();
            expect(visible.some(p => p.placementId === 1)).toBe(true);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 88: Multiple placements in alternate screen
   * For any multiple placements in alternate screen mode, none should be preserved in scrollback
   * Validates: Requirements 28.5
   */
  it('Property 88: Multiple placements in alternate screen no scrollback', () => {
    fc.assert(
      fc.property(
        // Generate screen dimensions
        fc.integer({ min: 20, max: 100 }), // screenRows
        // Generate multiple placements
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 15 }), // near top
            col: fc.integer({ min: 0, max: 50 }),
            width: fc.integer({ min: 1, max: 10 }),
            height: fc.integer({ min: 1, max: 10 })
          }),
          {
            minLength: 2,
            maxLength: 10,
            selector: (item) => item.placementId
          }
        ),
        // Generate scroll amount
        fc.integer({ min: 20, max: 40 }), // enough to push all off screen
        (screenRows, placements, scrollLines) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.placementId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
          }
          
          const initialCount = placements.length;
          expect(manager.getVisiblePlacements().length).toBe(initialCount);
          
          // Scroll up in alternate screen mode
          manager.handleScroll('up', scrollLines, screenRows, true);
          
          // Verify NO placements are in scrollback
          const scrollback = manager.getScrollbackPlacements();
          expect(scrollback).toHaveLength(0);
          
          // Verify placements that scrolled off are completely removed
          for (const p of placements) {
            const newRow = p.row - scrollLines;
            if (newRow < 0) {
              // Should be completely removed
              const retrieved = manager.getPlacement(p.placementId);
              expect(retrieved).toBeUndefined();
            }
          }
          
          // Count how many should still be visible
          const expectedVisible = placements.filter(p => p.row - scrollLines >= 0).length;
          const visible = manager.getVisiblePlacements();
          expect(visible.length).toBe(expectedVisible);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 89: Image deletion by image ID
   * For any image with one or more placements, deleting the image by ID should remove all its placements
   * Validates: Requirements 29.1
   */
  it('Property 89: Image deletion by image ID', () => {
    fc.assert(
      fc.property(
        // Generate an image ID
        fc.integer({ min: 1, max: 10000 }),
        // Generate image properties
        fc.integer({ min: 1, max: 2000 }),
        fc.integer({ min: 1, max: 2000 }),
        fc.constantFrom('png', 'jpeg', 'gif'),
        // Generate multiple placements for that image
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 200 }),
            col: fc.integer({ min: 0, max: 200 }),
            width: fc.integer({ min: 1, max: 100 }),
            height: fc.integer({ min: 1, max: 100 })
          }),
          {
            minLength: 1,
            maxLength: 10,
            selector: (item) => item.placementId
          }
        ),
        (imageId, width, height, format, placements) => {
          const manager = new ImageManager();
          
          // Store the image
          const mockBitmap = new (globalThis as any).ImageBitmap(width, height);
          manager.storeImage(imageId, mockBitmap, format, width, height, true);
          
          // Verify image is stored
          expect(manager.getImage(imageId)).toBeDefined();
          
          // Create all placements for this image
          const placementIds: number[] = [];
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
            placementIds.push(p.placementId);
          }
          
          // Verify all placements exist
          expect(manager.getVisiblePlacements()).toHaveLength(placements.length);
          for (const placementId of placementIds) {
            expect(manager.getPlacement(placementId)).toBeDefined();
          }
          
          // Delete the image by ID
          manager.deleteImage(imageId);
          
          // Verify the image is deleted
          expect(manager.getImage(imageId)).toBeUndefined();
          
          // Verify ALL placements of this image are deleted
          for (const placementId of placementIds) {
            expect(manager.getPlacement(placementId)).toBeUndefined();
          }
          
          // Verify visible placements list is empty
          expect(manager.getVisiblePlacements()).toHaveLength(0);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 90: Placement deletion by placement ID
   * For any placement, deleting it by placement ID should remove only that specific placement
   * Validates: Requirements 29.2
   */
  it('Property 90: Placement deletion by placement ID', () => {
    fc.assert(
      fc.property(
        // Generate an image ID
        fc.integer({ min: 1, max: 10000 }),
        // Generate multiple placements for that image
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 200 }),
            col: fc.integer({ min: 0, max: 200 }),
            width: fc.integer({ min: 1, max: 100 }),
            height: fc.integer({ min: 1, max: 100 })
          }),
          {
            minLength: 2,
            maxLength: 10,
            selector: (item) => item.placementId
          }
        ),
        // Select one placement to delete
        fc.integer({ min: 0, max: 9 }),
        (imageId, placements, deleteIndex) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
          }
          
          // Verify all placements exist
          const initialCount = placements.length;
          expect(manager.getVisiblePlacements()).toHaveLength(initialCount);
          
          // Select a placement to delete (ensure index is valid)
          const indexToDelete = deleteIndex % placements.length;
          const placementToDelete = placements[indexToDelete];
          
          // Delete the specific placement by ID
          manager.deletePlacement(placementToDelete.placementId);
          
          // Verify the deleted placement is gone
          expect(manager.getPlacement(placementToDelete.placementId)).toBeUndefined();
          
          // Verify all OTHER placements still exist
          for (let i = 0; i < placements.length; i++) {
            if (i !== indexToDelete) {
              const p = placements[i];
              expect(manager.getPlacement(p.placementId)).toBeDefined();
            }
          }
          
          // Verify visible placements count decreased by 1
          expect(manager.getVisiblePlacements()).toHaveLength(initialCount - 1);
          
          // Verify the deleted placement is not in visible list
          const visible = manager.getVisiblePlacements();
          expect(visible.some(p => p.placementId === placementToDelete.placementId)).toBe(false);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 91: Delete all visible placements
   * For any set of visible placements, deleteAllPlacements should remove all active placements but keep scrollback
   * Validates: Requirements 29.3
   */
  it('Property 91: Delete all visible placements', () => {
    fc.assert(
      fc.property(
        // Generate multiple visible placements
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            imageId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 200 }),
            col: fc.integer({ min: 0, max: 200 }),
            width: fc.integer({ min: 1, max: 100 }),
            height: fc.integer({ min: 1, max: 100 })
          }),
          {
            minLength: 1,
            maxLength: 20,
            selector: (item) => item.placementId
          }
        ),
        (placements) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
          }
          
          // Verify all placements are visible
          const initialCount = placements.length;
          expect(manager.getVisiblePlacements()).toHaveLength(initialCount);
          
          // Delete all visible placements
          manager.deleteAllPlacements();
          
          // Verify all visible placements are removed
          expect(manager.getVisiblePlacements()).toHaveLength(0);
          
          // Verify all placements are deleted from the map
          for (const p of placements) {
            expect(manager.getPlacement(p.placementId)).toBeUndefined();
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 92: Image data memory cleanup
   * For any image deleted, the image data should be freed from memory
   * Validates: Requirements 29.4
   */
  it('Property 92: Image data memory cleanup', () => {
    fc.assert(
      fc.property(
        // Generate multiple images
        fc.uniqueArray(
          fc.record({
            id: fc.integer({ min: 1, max: 10000 }),
            width: fc.integer({ min: 1, max: 2000 }),
            height: fc.integer({ min: 1, max: 2000 }),
            format: fc.constantFrom('png', 'jpeg', 'gif')
          }),
          {
            minLength: 2,
            maxLength: 10,
            selector: (item) => item.id
          }
        ),
        // Select one image to delete
        fc.integer({ min: 0, max: 9 }),
        (images, deleteIndex) => {
          const manager = new ImageManager();
          const bitmaps = new Map<number, any>();
          
          // Store all images
          for (const img of images) {
            const mockBitmap = new (globalThis as any).ImageBitmap(img.width, img.height);
            bitmaps.set(img.id, mockBitmap);
            manager.storeImage(img.id, mockBitmap, img.format, img.width, img.height, true);
          }
          
          // Verify all images are stored
          for (const img of images) {
            expect(manager.getImage(img.id)).toBeDefined();
          }
          
          // Select an image to delete (ensure index is valid)
          const indexToDelete = deleteIndex % images.length;
          const imageToDelete = images[indexToDelete];
          
          // Delete the image
          manager.deleteImage(imageToDelete.id);
          
          // Verify the deleted image is gone
          expect(manager.getImage(imageToDelete.id)).toBeUndefined();
          
          // Verify all OTHER images still exist
          for (let i = 0; i < images.length; i++) {
            if (i !== indexToDelete) {
              const img = images[i];
              expect(manager.getImage(img.id)).toBeDefined();
              expect(manager.getImage(img.id)?.data).toBe(bitmaps.get(img.id));
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 93: Display update on placement deletion
   * For any placement deletion, the visible placements list should be updated immediately
   * Validates: Requirements 29.5
   */
  it('Property 93: Display update on placement deletion', () => {
    fc.assert(
      fc.property(
        // Generate multiple placements
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            imageId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 200 }),
            col: fc.integer({ min: 0, max: 200 }),
            width: fc.integer({ min: 1, max: 100 }),
            height: fc.integer({ min: 1, max: 100 })
          }),
          {
            minLength: 3,
            maxLength: 15,
            selector: (item) => item.placementId
          }
        ),
        // Select placements to delete
        fc.array(fc.integer({ min: 0, max: 14 }), { minLength: 1, maxLength: 5 }),
        (placements, deleteIndices) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
          }
          
          // Get initial visible placements
          const initialVisible = manager.getVisiblePlacements();
          expect(initialVisible).toHaveLength(placements.length);
          
          // Delete some placements
          const uniqueIndices = [...new Set(deleteIndices.map(i => i % placements.length))];
          const deletedIds = new Set<number>();
          
          for (const index of uniqueIndices) {
            const placementToDelete = placements[index];
            manager.deletePlacement(placementToDelete.placementId);
            deletedIds.add(placementToDelete.placementId);
            
            // Verify visible placements list is updated immediately after each deletion
            const currentVisible = manager.getVisiblePlacements();
            expect(currentVisible.some(p => p.placementId === placementToDelete.placementId)).toBe(false);
          }
          
          // Verify final visible placements list
          const finalVisible = manager.getVisiblePlacements();
          expect(finalVisible).toHaveLength(placements.length - uniqueIndices.length);
          
          // Verify deleted placements are not in visible list
          for (const id of deletedIds) {
            expect(finalVisible.some(p => p.placementId === id)).toBe(false);
          }
          
          // Verify remaining placements are still in visible list
          for (const p of placements) {
            if (!deletedIds.has(p.placementId)) {
              expect(finalVisible.some(vp => vp.placementId === p.placementId)).toBe(true);
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 106: Clear screen removes images
   * For any screen clear operation, all image placements in the cleared region should be removed
   * Validates: Requirements 33.1
   */
  it('Property 106: Clear screen removes images', () => {
    fc.assert(
      fc.property(
        // Generate multiple placements at various positions
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            imageId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 50 }),
            col: fc.integer({ min: 0, max: 80 }),
            width: fc.integer({ min: 1, max: 20 }),
            height: fc.integer({ min: 1, max: 10 })
          }),
          {
            minLength: 1,
            maxLength: 10,
            selector: (item) => item.placementId
          }
        ),
        (placements) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
          }
          
          // Verify all placements are visible
          expect(manager.getVisiblePlacements()).toHaveLength(placements.length);
          
          // Clear the screen
          manager.handleClear('screen');
          
          // Verify all placements are removed
          expect(manager.getVisiblePlacements()).toHaveLength(0);
          
          // Verify all placements are deleted from the map
          for (const p of placements) {
            expect(manager.getPlacement(p.placementId)).toBeUndefined();
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 107: Line erase removes images
   * For any line erase operation, image placements on that line should be removed
   * Validates: Requirements 33.2
   */
  it('Property 107: Line erase removes images', () => {
    fc.assert(
      fc.property(
        // Generate a target row to erase
        fc.integer({ min: 0, max: 50 }),
        // Generate multiple placements, some on the target row, some not
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            imageId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 50 }),
            col: fc.integer({ min: 0, max: 80 }),
            width: fc.integer({ min: 1, max: 20 }),
            height: fc.integer({ min: 1, max: 10 })
          }),
          {
            minLength: 3,
            maxLength: 15,
            selector: (item) => item.placementId
          }
        ),
        (targetRow, placements) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
          }
          
          const initialCount = placements.length;
          expect(manager.getVisiblePlacements()).toHaveLength(initialCount);
          
          // Erase the target line
          manager.handleClear('line', targetRow);
          
          // Count how many placements should be removed
          // A placement overlaps with the line if targetRow is within [placement.row, placement.row + placement.height - 1]
          const shouldBeRemoved = placements.filter(p => {
            const placementStartRow = p.row;
            const placementEndRow = p.row + p.height - 1;
            return targetRow >= placementStartRow && targetRow <= placementEndRow;
          });
          
          const shouldRemain = placements.filter(p => {
            const placementStartRow = p.row;
            const placementEndRow = p.row + p.height - 1;
            return targetRow < placementStartRow || targetRow > placementEndRow;
          });
          
          // Verify removed placements are gone
          for (const p of shouldBeRemoved) {
            expect(manager.getPlacement(p.placementId)).toBeUndefined();
          }
          
          // Verify remaining placements still exist
          for (const p of shouldRemain) {
            expect(manager.getPlacement(p.placementId)).toBeDefined();
          }
          
          // Verify visible placements count
          expect(manager.getVisiblePlacements()).toHaveLength(shouldRemain.length);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 107: Line erase with column range removes images
   * For any line erase with column range, only placements overlapping that range should be removed
   * Validates: Requirements 33.2
   */
  it('Property 107: Line erase with column range removes images', () => {
    fc.assert(
      fc.property(
        // Generate a target row to erase
        fc.integer({ min: 0, max: 50 }),
        // Generate column range
        fc.integer({ min: 0, max: 40 }), // startCol
        fc.integer({ min: 41, max: 80 }), // endCol
        // Generate multiple placements
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            imageId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 50 }),
            col: fc.integer({ min: 0, max: 80 }),
            width: fc.integer({ min: 1, max: 20 }),
            height: fc.integer({ min: 1, max: 10 })
          }),
          {
            minLength: 3,
            maxLength: 15,
            selector: (item) => item.placementId
          }
        ),
        (targetRow, startCol, endCol, placements) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
          }
          
          const initialCount = placements.length;
          expect(manager.getVisiblePlacements()).toHaveLength(initialCount);
          
          // Erase the target line with column range
          manager.handleClear('line', targetRow, startCol, endCol);
          
          // Count how many placements should be removed
          // A placement should be removed if:
          // 1. It overlaps with the target row
          // 2. It overlaps with the column range
          const shouldBeRemoved = placements.filter(p => {
            const placementStartRow = p.row;
            const placementEndRow = p.row + p.height - 1;
            const placementStartCol = p.col;
            const placementEndCol = p.col + p.width - 1;
            
            const rowOverlap = targetRow >= placementStartRow && targetRow <= placementEndRow;
            const colOverlap = !(endCol < placementStartCol || startCol > placementEndCol);
            
            return rowOverlap && colOverlap;
          });
          
          const shouldRemain = placements.filter(p => {
            const placementStartRow = p.row;
            const placementEndRow = p.row + p.height - 1;
            const placementStartCol = p.col;
            const placementEndCol = p.col + p.width - 1;
            
            const rowOverlap = targetRow >= placementStartRow && targetRow <= placementEndRow;
            const colOverlap = !(endCol < placementStartCol || startCol > placementEndCol);
            
            return !(rowOverlap && colOverlap);
          });
          
          // Verify removed placements are gone
          for (const p of shouldBeRemoved) {
            expect(manager.getPlacement(p.placementId)).toBeUndefined();
          }
          
          // Verify remaining placements still exist
          for (const p of shouldRemain) {
            expect(manager.getPlacement(p.placementId)).toBeDefined();
          }
          
          // Verify visible placements count
          expect(manager.getVisiblePlacements()).toHaveLength(shouldRemain.length);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 108: Line insertion shifts images
   * For any line insertion, image placements should shift down accordingly
   * Validates: Requirements 33.3
   */
  it('Property 108: Line insertion shifts images', () => {
    fc.assert(
      fc.property(
        // Generate screen dimensions
        fc.integer({ min: 20, max: 100 }), // screenRows
        // Generate insertion parameters
        fc.integer({ min: 0, max: 50 }), // insertRow
        fc.integer({ min: 1, max: 10 }), // count (number of lines to insert)
        // Generate multiple placements
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            imageId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 50 }),
            col: fc.integer({ min: 0, max: 80 }),
            width: fc.integer({ min: 1, max: 20 }),
            height: fc.integer({ min: 1, max: 10 })
          }),
          {
            minLength: 1,
            maxLength: 10,
            selector: (item) => item.placementId
          }
        ),
        (screenRows, insertRow, count, placements) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
          }
          
          const initialCount = placements.length;
          expect(manager.getVisiblePlacements()).toHaveLength(initialCount);
          
          // Insert lines
          manager.handleLineInsertion(insertRow, count, screenRows);
          
          // Verify placements are shifted correctly
          for (const p of placements) {
            const retrieved = manager.getPlacement(p.placementId);
            
            if (p.row >= insertRow) {
              // Placement should be shifted down
              const newRow = p.row + count;
              
              if (newRow >= screenRows) {
                // Placement went off the bottom, should be removed
                expect(retrieved).toBeUndefined();
              } else {
                // Placement should still exist at new position
                expect(retrieved).toBeDefined();
                expect(retrieved?.row).toBe(newRow);
                expect(retrieved?.col).toBe(p.col);
                expect(retrieved?.width).toBe(p.width);
                expect(retrieved?.height).toBe(p.height);
              }
            } else {
              // Placement above insertion point should not move
              expect(retrieved).toBeDefined();
              expect(retrieved?.row).toBe(p.row);
              expect(retrieved?.col).toBe(p.col);
            }
          }
          
          // Count how many should remain
          const shouldRemain = placements.filter(p => {
            if (p.row >= insertRow) {
              return p.row + count < screenRows;
            }
            return true;
          });
          
          expect(manager.getVisiblePlacements()).toHaveLength(shouldRemain.length);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 109: Line deletion shifts images
   * For any line deletion, image placements should shift up and those in deleted lines should be removed
   * Validates: Requirements 33.4
   */
  it('Property 109: Line deletion shifts images', () => {
    fc.assert(
      fc.property(
        // Generate deletion parameters
        fc.integer({ min: 0, max: 50 }), // deleteRow
        fc.integer({ min: 1, max: 10 }), // count (number of lines to delete)
        // Generate multiple placements
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            imageId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 60 }),
            col: fc.integer({ min: 0, max: 80 }),
            width: fc.integer({ min: 1, max: 20 }),
            height: fc.integer({ min: 1, max: 10 })
          }),
          {
            minLength: 1,
            maxLength: 15,
            selector: (item) => item.placementId
          }
        ),
        (deleteRow, count, placements) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
          }
          
          const initialCount = placements.length;
          expect(manager.getVisiblePlacements()).toHaveLength(initialCount);
          
          // Delete lines
          manager.handleLineDeletion(deleteRow, count);
          
          const deleteEndRow = deleteRow + count - 1;
          
          // Verify placements are handled correctly
          for (const p of placements) {
            const retrieved = manager.getPlacement(p.placementId);
            
            const placementStartRow = p.row;
            const placementEndRow = p.row + p.height - 1;
            
            // Check if placement overlaps with deleted lines
            if (placementStartRow <= deleteEndRow && placementEndRow >= deleteRow) {
              // Placement is in the deleted region - should be removed
              expect(retrieved).toBeUndefined();
            } else if (placementStartRow > deleteEndRow) {
              // Placement is below the deleted region - should be shifted up
              expect(retrieved).toBeDefined();
              expect(retrieved?.row).toBe(p.row - count);
              expect(retrieved?.col).toBe(p.col);
              expect(retrieved?.width).toBe(p.width);
              expect(retrieved?.height).toBe(p.height);
            } else {
              // Placement is above the deleted region - should not move
              expect(retrieved).toBeDefined();
              expect(retrieved?.row).toBe(p.row);
              expect(retrieved?.col).toBe(p.col);
            }
          }
          
          // Count how many should remain
          const shouldRemain = placements.filter(p => {
            const placementStartRow = p.row;
            const placementEndRow = p.row + p.height - 1;
            
            // Not in deleted region
            return !(placementStartRow <= deleteEndRow && placementEndRow >= deleteRow);
          });
          
          expect(manager.getVisiblePlacements()).toHaveLength(shouldRemain.length);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 110: Resize repositions images
   * For any terminal resize, image placements should be repositioned based on new cell dimensions
   * Validates: Requirements 33.5
   */
  it('Property 110: Resize repositions images', () => {
    fc.assert(
      fc.property(
        // Generate old dimensions
        fc.integer({ min: 20, max: 100 }), // oldCols
        fc.integer({ min: 20, max: 100 }), // oldRows
        // Generate new dimensions
        fc.integer({ min: 20, max: 100 }), // newCols
        fc.integer({ min: 20, max: 100 }), // newRows
        // Generate multiple placements (constrained to old dimensions)
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            imageId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 50 }),
            col: fc.integer({ min: 0, max: 50 }),
            width: fc.integer({ min: 1, max: 20 }),
            height: fc.integer({ min: 1, max: 10 })
          }),
          {
            minLength: 1,
            maxLength: 15,
            selector: (item) => item.placementId
          }
        ),
        (oldCols, oldRows, newCols, newRows, rawPlacements) => {
          const manager = new ImageManager();
          
          // Filter placements to only include those within old bounds
          const placements = rawPlacements.filter(p => p.row < oldRows && p.col < oldCols);
          
          // Skip test if no valid placements
          if (placements.length === 0) {
            return true;
          }
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height
            };
            manager.createPlacement(placement);
          }
          
          const initialCount = placements.length;
          expect(manager.getVisiblePlacements()).toHaveLength(initialCount);
          
          // Resize terminal
          manager.handleResize(oldCols, oldRows, newCols, newRows);
          
          // Verify placements are handled correctly
          for (const p of placements) {
            const retrieved = manager.getPlacement(p.placementId);
            
            if (p.row >= newRows || p.col >= newCols) {
              // Placement is now out of bounds - should be removed
              expect(retrieved).toBeUndefined();
            } else {
              // Placement is still within bounds - should still exist
              expect(retrieved).toBeDefined();
              expect(retrieved?.row).toBe(p.row);
              expect(retrieved?.col).toBe(p.col);
              expect(retrieved?.width).toBe(p.width);
              expect(retrieved?.height).toBe(p.height);
            }
          }
          
          // Count how many should remain
          const shouldRemain = placements.filter(p => {
            return p.row < newRows && p.col < newCols;
          });
          
          expect(manager.getVisiblePlacements()).toHaveLength(shouldRemain.length);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 111: Image ID reuse replaces data
   * For any image ID reused, the previous image data should be replaced with new data
   * Validates: Requirements 34.3
   */
  it('Property 111: Image ID reuse replaces data', () => {
    fc.assert(
      fc.property(
        // Generate a single image ID
        fc.integer({ min: 1, max: 10000 }),
        // Generate two different sets of image properties
        fc.tuple(
          fc.integer({ min: 1, max: 2000 }),
          fc.integer({ min: 1, max: 2000 }),
          fc.constantFrom('png', 'jpeg', 'gif')
        ),
        fc.tuple(
          fc.integer({ min: 1, max: 2000 }),
          fc.integer({ min: 1, max: 2000 }),
          fc.constantFrom('png', 'jpeg', 'gif')
        ),
        (imageId, [width1, height1, format1], [width2, height2, format2]) => {
          const manager = new ImageManager();
          
          // Store first image
          const mockBitmap1 = new (globalThis as any).ImageBitmap(width1, height1);
          manager.storeImage(imageId, mockBitmap1, format1, width1, height1, true);
          
          // Verify first image is stored
          const retrieved1 = manager.getImage(imageId);
          expect(retrieved1).toBeDefined();
          expect(retrieved1?.data).toBe(mockBitmap1);
          expect(retrieved1?.format).toBe(format1);
          expect(retrieved1?.width).toBe(width1);
          expect(retrieved1?.height).toBe(height1);
          
          // Store second image with same ID (reuse)
          const mockBitmap2 = new (globalThis as any).ImageBitmap(width2, height2);
          manager.storeImage(imageId, mockBitmap2, format2, width2, height2, true);
          
          // Verify the second image replaced the first
          const retrieved2 = manager.getImage(imageId);
          expect(retrieved2).toBeDefined();
          expect(retrieved2?.data).toBe(mockBitmap2);
          expect(retrieved2?.format).toBe(format2);
          expect(retrieved2?.width).toBe(width2);
          expect(retrieved2?.height).toBe(height2);
          
          // Verify it's not the first image
          expect(retrieved2?.data).not.toBe(mockBitmap1);
          
          // Verify only one image exists with this ID
          expect(manager.getImage(imageId)).toBeDefined();
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 112: Placement ID reuse replaces placement
   * For any placement ID reused, the previous placement should be replaced with the new placement
   * Validates: Requirements 34.4
   */
  it('Property 112: Placement ID reuse replaces placement', () => {
    fc.assert(
      fc.property(
        // Generate a single placement ID
        fc.integer({ min: 1, max: 10000 }),
        // Generate two different sets of placement properties
        fc.tuple(
          fc.integer({ min: 1, max: 10000 }), // imageId1
          fc.integer({ min: 0, max: 200 }),   // row1
          fc.integer({ min: 0, max: 200 }),   // col1
          fc.integer({ min: 1, max: 100 }),   // width1
          fc.integer({ min: 1, max: 100 })    // height1
        ),
        fc.tuple(
          fc.integer({ min: 1, max: 10000 }), // imageId2
          fc.integer({ min: 0, max: 200 }),   // row2
          fc.integer({ min: 0, max: 200 }),   // col2
          fc.integer({ min: 1, max: 100 }),   // width2
          fc.integer({ min: 1, max: 100 })    // height2
        ),
        (placementId, [imageId1, row1, col1, width1, height1], [imageId2, row2, col2, width2, height2]) => {
          const manager = new ImageManager();
          
          // Create first placement
          const placement1: ImagePlacement = {
            placementId,
            imageId: imageId1,
            row: row1,
            col: col1,
            width: width1,
            height: height1
          };
          manager.createPlacement(placement1);
          
          // Verify first placement is created
          const retrieved1 = manager.getPlacement(placementId);
          expect(retrieved1).toBeDefined();
          expect(retrieved1).toEqual(placement1);
          
          // Verify it's in visible placements
          const visible1 = manager.getVisiblePlacements();
          expect(visible1).toHaveLength(1);
          expect(visible1[0]).toEqual(placement1);
          
          // Create second placement with same ID (reuse)
          const placement2: ImagePlacement = {
            placementId,
            imageId: imageId2,
            row: row2,
            col: col2,
            width: width2,
            height: height2
          };
          manager.createPlacement(placement2);
          
          // Verify the second placement replaced the first
          const retrieved2 = manager.getPlacement(placementId);
          expect(retrieved2).toBeDefined();
          expect(retrieved2).toEqual(placement2);
          
          // Verify only one placement exists in visible list
          const visible2 = manager.getVisiblePlacements();
          expect(visible2).toHaveLength(1);
          expect(visible2[0]).toEqual(placement2);
          
          // Verify it's not the first placement
          expect(retrieved2).not.toEqual(placement1);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 113: Automatic ID generation
   * For any image or placement without specified ID, the terminal should generate a unique ID automatically
   * Validates: Requirements 34.5
   */
  it('Property 113: Automatic ID generation', () => {
    fc.assert(
      fc.property(
        // Generate number of images and placements to create
        fc.integer({ min: 1, max: 20 }), // numImages
        fc.integer({ min: 1, max: 20 }), // numPlacements
        // Generate some explicit IDs to test counter updates
        fc.option(fc.integer({ min: 50, max: 200 }), { nil: undefined }), // explicitImageId
        fc.option(fc.integer({ min: 50, max: 200 }), { nil: undefined }), // explicitPlacementId
        (numImages, numPlacements, explicitImageId, explicitPlacementId) => {
          const manager = new ImageManager();
          const generatedImageIds: number[] = [];
          const generatedPlacementIds: number[] = [];
          
          // Generate image IDs
          for (let i = 0; i < numImages; i++) {
            const id = manager.generateImageId();
            generatedImageIds.push(id);
            
            // Insert explicit ID in the middle if provided
            if (i === Math.floor(numImages / 2) && explicitImageId !== undefined) {
              const mockBitmap = new (globalThis as any).ImageBitmap(100, 100);
              manager.storeImage(explicitImageId, mockBitmap, 'png', 100, 100, true);
            }
          }
          
          // Generate placement IDs
          for (let i = 0; i < numPlacements; i++) {
            const id = manager.generatePlacementId();
            generatedPlacementIds.push(id);
            
            // Insert explicit ID in the middle if provided
            if (i === Math.floor(numPlacements / 2) && explicitPlacementId !== undefined) {
              const placement: ImagePlacement = {
                placementId: explicitPlacementId,
                imageId: 1,
                row: 0,
                col: 0,
                width: 10,
                height: 10
              };
              manager.createPlacement(placement);
            }
          }
          
          // Verify all generated image IDs are unique
          const uniqueImageIds = new Set(generatedImageIds);
          expect(uniqueImageIds.size).toBe(generatedImageIds.length);
          
          // Verify all generated placement IDs are unique
          const uniquePlacementIds = new Set(generatedPlacementIds);
          expect(uniquePlacementIds.size).toBe(generatedPlacementIds.length);
          
          // Verify image IDs are sequential (with possible jump after explicit ID)
          for (let i = 1; i < generatedImageIds.length; i++) {
            const diff = generatedImageIds[i] - generatedImageIds[i - 1];
            // Difference should be 1, or larger if explicit ID was inserted
            expect(diff).toBeGreaterThanOrEqual(1);
          }
          
          // Verify placement IDs are sequential (with possible jump after explicit ID)
          for (let i = 1; i < generatedPlacementIds.length; i++) {
            const diff = generatedPlacementIds[i] - generatedPlacementIds[i - 1];
            // Difference should be 1, or larger if explicit ID was inserted
            expect(diff).toBeGreaterThanOrEqual(1);
          }
          
          // Verify first generated IDs start from 1 (if no explicit ID was inserted before)
          if (explicitImageId === undefined || Math.floor(numImages / 2) > 0) {
            expect(generatedImageIds[0]).toBe(1);
          }
          
          if (explicitPlacementId === undefined || Math.floor(numPlacements / 2) > 0) {
            expect(generatedPlacementIds[0]).toBe(1);
          }
          
          // If explicit IDs were provided, verify counter jumped past them
          if (explicitImageId !== undefined) {
            const idsAfterExplicit = generatedImageIds.slice(Math.floor(numImages / 2) + 1);
            for (const id of idsAfterExplicit) {
              if (explicitImageId >= generatedImageIds[Math.floor(numImages / 2)]) {
                expect(id).toBeGreaterThan(explicitImageId);
              }
            }
          }
          
          if (explicitPlacementId !== undefined) {
            const idsAfterExplicit = generatedPlacementIds.slice(Math.floor(numPlacements / 2) + 1);
            for (const id of idsAfterExplicit) {
              if (explicitPlacementId >= generatedPlacementIds[Math.floor(numPlacements / 2)]) {
                expect(id).toBeGreaterThan(explicitPlacementId);
              }
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });
});

describe('ImageManager Transparency Property Tests', () => {
  /**
   * Feature: headless-terminal-emulator, Property 114: Alpha channel preservation
   * For any image with alpha channel, the transparency should be preserved
   * Validates: Requirements 35.1
   */
  it('Property 114: Alpha channel preservation', () => {
    fc.assert(
      fc.property(
        // Generate random image IDs
        fc.integer({ min: 1, max: 10000 }),
        // Generate random image dimensions
        fc.integer({ min: 1, max: 2000 }),
        fc.integer({ min: 1, max: 2000 }),
        // Generate random format
        fc.constantFrom('png', 'gif'), // PNG and GIF support alpha, JPEG does not
        (imageId, width, height, format) => {
          const manager = new ImageManager();
          
          // Create a mock ImageBitmap with the specified dimensions
          const mockBitmap = new (globalThis as any).ImageBitmap(width, height);
          
          // Store the image with alpha channel enabled
          manager.storeImage(imageId, mockBitmap, format, width, height, true);
          
          // Retrieve the image
          const retrieved = manager.getImage(imageId);
          
          // Verify the image was stored with alpha channel preserved
          expect(retrieved).toBeDefined();
          expect(retrieved?.hasAlpha).toBe(true);
          expect(retrieved?.id).toBe(imageId);
          expect(retrieved?.data).toBe(mockBitmap);
          expect(retrieved?.format).toBe(format);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 115: Transparent pixel rendering
   * For any transparent image, the terminal background should show through transparent pixels
   * Validates: Requirements 35.2
   * 
   * Note: This property verifies that images with alpha channel are stored correctly.
   * The actual rendering behavior (background showing through) is tested in Renderer tests.
   */
  it('Property 115: Transparent pixel rendering', () => {
    fc.assert(
      fc.property(
        // Generate random image and placement parameters
        fc.integer({ min: 1, max: 10000 }), // imageId
        fc.integer({ min: 1, max: 10000 }), // placementId
        fc.integer({ min: 1, max: 2000 }),  // width
        fc.integer({ min: 1, max: 2000 }),  // height
        fc.constantFrom('png', 'gif'),      // format with alpha support
        fc.integer({ min: 0, max: 200 }),   // row
        fc.integer({ min: 0, max: 200 }),   // col
        fc.integer({ min: 1, max: 100 }),   // placement width in cells
        fc.integer({ min: 1, max: 100 }),   // placement height in cells
        (imageId, placementId, width, height, format, row, col, cellWidth, cellHeight) => {
          const manager = new ImageManager();
          
          // Create a mock ImageBitmap
          const mockBitmap = new (globalThis as any).ImageBitmap(width, height);
          
          // Store image with alpha channel
          manager.storeImage(imageId, mockBitmap, format, width, height, true);
          
          // Create a placement for this image
          const placement: ImagePlacement = {
            placementId,
            imageId,
            row,
            col,
            width: cellWidth,
            height: cellHeight
          };
          manager.createPlacement(placement);
          
          // Verify the image has alpha channel
          const image = manager.getImage(imageId);
          expect(image?.hasAlpha).toBe(true);
          
          // Verify the placement exists and references the transparent image
          const retrievedPlacement = manager.getPlacement(placementId);
          expect(retrievedPlacement).toBeDefined();
          expect(retrievedPlacement?.imageId).toBe(imageId);
          
          // The renderer should use the hasAlpha flag to determine rendering behavior
          // This is verified by checking that the flag is accessible through the image
          expect(image?.hasAlpha).toBe(true);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 116: Opaque image handling
   * For any image without alpha channel, it should be treated as fully opaque
   * Validates: Requirements 35.3
   */
  it('Property 116: Opaque image handling', () => {
    fc.assert(
      fc.property(
        // Generate random image IDs
        fc.integer({ min: 1, max: 10000 }),
        // Generate random image dimensions
        fc.integer({ min: 1, max: 2000 }),
        fc.integer({ min: 1, max: 2000 }),
        // Generate random format (JPEG never has alpha, PNG/GIF can be opaque)
        fc.constantFrom('png', 'jpeg', 'gif'),
        (imageId, width, height, format) => {
          const manager = new ImageManager();
          
          // Create a mock ImageBitmap
          const mockBitmap = new (globalThis as any).ImageBitmap(width, height);
          
          // Store the image without alpha channel (opaque)
          manager.storeImage(imageId, mockBitmap, format, width, height, false);
          
          // Retrieve the image
          const retrieved = manager.getImage(imageId);
          
          // Verify the image is marked as opaque (no alpha channel)
          expect(retrieved).toBeDefined();
          expect(retrieved?.hasAlpha).toBe(false);
          expect(retrieved?.id).toBe(imageId);
          expect(retrieved?.data).toBe(mockBitmap);
          expect(retrieved?.format).toBe(format);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 117: Image text layering
   * For any transparent image overlapping text, the image should be layered appropriately
   * Validates: Requirements 35.4
   * 
   * Note: This property verifies that placements with transparent images can be created
   * and positioned over text cells. The actual layering behavior is handled by the Renderer.
   */
  it('Property 117: Image text layering', () => {
    fc.assert(
      fc.property(
        // Generate random image and placement parameters
        fc.integer({ min: 1, max: 10000 }), // imageId
        fc.integer({ min: 1, max: 10000 }), // placementId
        fc.integer({ min: 1, max: 2000 }),  // width
        fc.integer({ min: 1, max: 2000 }),  // height
        fc.constantFrom('png', 'gif'),      // format with alpha support
        fc.integer({ min: 0, max: 200 }),   // row (where text might be)
        fc.integer({ min: 0, max: 200 }),   // col (where text might be)
        fc.integer({ min: 1, max: 100 }),   // placement width in cells
        fc.integer({ min: 1, max: 100 }),   // placement height in cells
        fc.option(fc.integer({ min: -100, max: 100 }), { nil: undefined }), // zIndex for layering
        (imageId, placementId, width, height, format, row, col, cellWidth, cellHeight, zIndex) => {
          const manager = new ImageManager();
          
          // Create a mock ImageBitmap
          const mockBitmap = new (globalThis as any).ImageBitmap(width, height);
          
          // Store image with alpha channel (transparent)
          manager.storeImage(imageId, mockBitmap, format, width, height, true);
          
          // Create a placement that might overlap with text
          const placement: ImagePlacement = {
            placementId,
            imageId,
            row,
            col,
            width: cellWidth,
            height: cellHeight,
            ...(zIndex !== undefined && { zIndex })
          };
          manager.createPlacement(placement);
          
          // Verify the placement exists
          const retrievedPlacement = manager.getPlacement(placementId);
          expect(retrievedPlacement).toBeDefined();
          expect(retrievedPlacement?.imageId).toBe(imageId);
          
          // Verify the image has alpha channel for proper layering
          const image = manager.getImage(imageId);
          expect(image?.hasAlpha).toBe(true);
          
          // Verify zIndex is preserved if provided (for layering control)
          if (zIndex !== undefined) {
            expect(retrievedPlacement?.zIndex).toBe(zIndex);
          }
          
          // The placement can be positioned at any cell location,
          // potentially overlapping with text. The renderer will handle
          // the actual layering based on hasAlpha and zIndex.
          expect(retrievedPlacement?.row).toBe(row);
          expect(retrievedPlacement?.col).toBe(col);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 118: Background color change updates transparency
   * For any terminal background color change, transparent images should update their appearance
   * Validates: Requirements 35.5
   * 
   * Note: This property verifies that transparent images maintain their hasAlpha flag
   * consistently, which allows the renderer to respond to background color changes.
   * The actual visual update is handled by the Renderer component.
   */
  it('Property 118: Background color change updates transparency', () => {
    fc.assert(
      fc.property(
        // Generate multiple images with different transparency settings
        // Generate arrays with unique imageIds, then assign unique placementIds
        fc.uniqueArray(
          fc.record({
            imageId: fc.integer({ min: 1, max: 10000 }),
            width: fc.integer({ min: 1, max: 2000 }),
            height: fc.integer({ min: 1, max: 2000 }),
            format: fc.constantFrom('png', 'gif'),
            hasAlpha: fc.boolean(),
            row: fc.integer({ min: 0, max: 200 }),
            col: fc.integer({ min: 0, max: 200 }),
            cellWidth: fc.integer({ min: 1, max: 100 }),
            cellHeight: fc.integer({ min: 1, max: 100 })
          }),
          {
            minLength: 1,
            maxLength: 10,
            selector: (item) => item.imageId
          }
        ).map(images => {
          // Assign unique placementIds to each image
          return images.map((img, index) => ({
            ...img,
            placementId: index + 1
          }));
        }),
        (images) => {
          const manager = new ImageManager();
          
          // Build a map to track the final state of each image
          // (in case same imageId appears multiple times, last one wins)
          const imageStates = new Map<number, typeof images[0]>();
          for (const img of images) {
            imageStates.set(img.imageId, img);
          }
          
          // Store all images and create placements
          for (const img of images) {
            const mockBitmap = new (globalThis as any).ImageBitmap(img.width, img.height);
            
            // Store image with specified alpha channel setting
            manager.storeImage(img.imageId, mockBitmap, img.format, img.width, img.height, img.hasAlpha);
            
            // Create placement
            const placement: ImagePlacement = {
              placementId: img.placementId,
              imageId: img.imageId,
              row: img.row,
              col: img.col,
              width: img.cellWidth,
              height: img.cellHeight
            };
            manager.createPlacement(placement);
          }
          
          // Simulate background color change by verifying all images maintain their alpha state
          // In a real scenario, the renderer would query these images and re-render them
          // with the new background color showing through transparent pixels
          
          // Verify each stored image has the correct alpha state (from the final store operation)
          for (const [imageId, finalState] of imageStates) {
            const storedImage = manager.getImage(imageId);
            
            // Verify image alpha state matches the final state
            expect(storedImage).toBeDefined();
            expect(storedImage?.hasAlpha).toBe(finalState.hasAlpha);
            
            // The renderer can now use the hasAlpha flag to determine
            // whether to blend with the new background color
            if (finalState.hasAlpha) {
              // Transparent images should be re-rendered with new background
              expect(storedImage?.hasAlpha).toBe(true);
            } else {
              // Opaque images don't need background blending
              expect(storedImage?.hasAlpha).toBe(false);
            }
          }
          
          // Verify all placements are still visible and accessible
          const visiblePlacements = manager.getVisiblePlacements();
          expect(visiblePlacements).toHaveLength(images.length);
          
          // Each placement should still reference an image with correct alpha state
          for (const placement of visiblePlacements) {
            const image = manager.getImage(placement.imageId);
            expect(image).toBeDefined();
            
            // Find the final state for this image
            const finalState = imageStates.get(placement.imageId);
            expect(finalState).toBeDefined();
            expect(image?.hasAlpha).toBe(finalState?.hasAlpha);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 114-116: Mixed transparency images
   * For any collection of images with mixed transparency settings, each should maintain
   * its transparency state independently
   * Validates: Requirements 35.1, 35.2, 35.3
   */
  it('Property 114-116: Mixed transparency images', () => {
    fc.assert(
      fc.property(
        // Generate multiple images with different transparency settings
        fc.uniqueArray(
          fc.record({
            imageId: fc.integer({ min: 1, max: 10000 }),
            width: fc.integer({ min: 1, max: 2000 }),
            height: fc.integer({ min: 1, max: 2000 }),
            format: fc.constantFrom('png', 'jpeg', 'gif'),
            hasAlpha: fc.boolean()
          }),
          {
            minLength: 2,
            maxLength: 20,
            selector: (item) => item.imageId
          }
        ),
        (images) => {
          const manager = new ImageManager();
          
          // Store all images with their respective alpha settings
          for (const img of images) {
            const mockBitmap = new (globalThis as any).ImageBitmap(img.width, img.height);
            manager.storeImage(img.imageId, mockBitmap, img.format, img.width, img.height, img.hasAlpha);
          }
          
          // Verify each image maintains its transparency state independently
          for (const img of images) {
            const retrieved = manager.getImage(img.imageId);
            expect(retrieved).toBeDefined();
            expect(retrieved?.hasAlpha).toBe(img.hasAlpha);
            expect(retrieved?.id).toBe(img.imageId);
            expect(retrieved?.format).toBe(img.format);
          }
          
          // Verify that changing one image doesn't affect others
          // Replace the first image with opposite alpha setting
          if (images.length > 0) {
            const firstImage = images[0];
            const newAlpha = !firstImage.hasAlpha;
            const mockBitmap = new (globalThis as any).ImageBitmap(firstImage.width, firstImage.height);
            manager.storeImage(firstImage.imageId, mockBitmap, firstImage.format, firstImage.width, firstImage.height, newAlpha);
            
            // Verify the first image was updated
            const updated = manager.getImage(firstImage.imageId);
            expect(updated?.hasAlpha).toBe(newAlpha);
            
            // Verify all other images remain unchanged
            for (let i = 1; i < images.length; i++) {
              const img = images[i];
              const retrieved = manager.getImage(img.imageId);
              expect(retrieved?.hasAlpha).toBe(img.hasAlpha);
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });
});

  /**
   * Feature: headless-terminal-emulator, Property 119: Unicode placeholder association
   * For any image placement with Unicode placeholder, the placeholder character should be written to the grid and associated with the placement
   * Validates: Requirements 36.1, 36.2
   */
  it('Property 119: Unicode placeholder association', () => {
    fc.assert(
      fc.property(
        // Generate random placement parameters
        fc.integer({ min: 1, max: 10000 }), // placementId
        fc.integer({ min: 1, max: 10000 }), // imageId
        fc.integer({ min: 0, max: 200 }),   // row
        fc.integer({ min: 0, max: 200 }),   // col
        fc.integer({ min: 1, max: 100 }),   // width
        fc.integer({ min: 1, max: 100 }),   // height
        fc.string({ minLength: 1, maxLength: 1 }), // unicodePlaceholder (single character)
        (placementId, imageId, row, col, width, height, unicodePlaceholder) => {
          const manager = new ImageManager();
          
          // Create a placement with Unicode placeholder
          const placement: ImagePlacement = {
            placementId,
            imageId,
            row,
            col,
            width,
            height,
            unicodePlaceholder
          };
          
          manager.createPlacement(placement);
          
          // Verify the placement was created
          const retrieved = manager.getPlacement(placementId);
          expect(retrieved).toBeDefined();
          expect(retrieved?.unicodePlaceholder).toBe(unicodePlaceholder);
          
          // Verify the placeholder is associated with the cell position
          const associatedPlacementId = manager.getPlacementAtCell(row, col);
          expect(associatedPlacementId).toBe(placementId);
          
          // Verify the association is bidirectional - we can find the placement from the cell
          expect(associatedPlacementId).toBeDefined();
          const placementFromCell = manager.getPlacement(associatedPlacementId!);
          expect(placementFromCell).toEqual(placement);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 119: Multiple placements with placeholders
   * For any set of placements with Unicode placeholders at different positions, each should be independently associated
   * Validates: Requirements 36.1, 36.2
   */
  it('Property 119: Multiple placements with placeholders', () => {
    fc.assert(
      fc.property(
        // Generate multiple unique placements with placeholders
        fc.uniqueArray(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            imageId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 200 }),
            col: fc.integer({ min: 0, max: 200 }),
            width: fc.integer({ min: 1, max: 100 }),
            height: fc.integer({ min: 1, max: 100 }),
            unicodePlaceholder: fc.string({ minLength: 1, maxLength: 1 })
          }),
          {
            minLength: 1,
            maxLength: 20,
            selector: (item) => item.placementId // Ensure unique placement IDs
          }
        ),
        (placements) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height,
              unicodePlaceholder: p.unicodePlaceholder
            };
            manager.createPlacement(placement);
          }
          
          // Verify each placeholder is independently associated
          for (const p of placements) {
            const associatedPlacementId = manager.getPlacementAtCell(p.row, p.col);
            expect(associatedPlacementId).toBe(p.placementId);
            
            const retrieved = manager.getPlacement(associatedPlacementId!);
            expect(retrieved?.unicodePlaceholder).toBe(p.unicodePlaceholder);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 120: Placeholder erase removes image
   * For any Unicode placeholder that is erased, the associated image placement should be removed
   * Validates: Requirements 36.3
   */
  it('Property 120: Placeholder erase removes image', () => {
    fc.assert(
      fc.property(
        // Generate random placement parameters
        fc.integer({ min: 1, max: 10000 }), // placementId
        fc.integer({ min: 1, max: 10000 }), // imageId
        fc.integer({ min: 0, max: 200 }),   // row
        fc.integer({ min: 0, max: 200 }),   // col
        fc.integer({ min: 1, max: 100 }),   // width
        fc.integer({ min: 1, max: 100 }),   // height
        fc.string({ minLength: 1, maxLength: 1 }), // unicodePlaceholder
        (placementId, imageId, row, col, width, height, unicodePlaceholder) => {
          const manager = new ImageManager();
          
          // Create a placement with Unicode placeholder
          const placement: ImagePlacement = {
            placementId,
            imageId,
            row,
            col,
            width,
            height,
            unicodePlaceholder
          };
          
          manager.createPlacement(placement);
          
          // Verify the placement exists
          expect(manager.getPlacement(placementId)).toBeDefined();
          expect(manager.getPlacementAtCell(row, col)).toBe(placementId);
          
          // Erase the cell containing the placeholder
          manager.handleCellOverwrite(row, col);
          
          // Verify the placement was removed
          expect(manager.getPlacement(placementId)).toBeUndefined();
          
          // Verify the placeholder association was removed
          expect(manager.getPlacementAtCell(row, col)).toBeUndefined();
          
          // Verify the placement is no longer in visible placements
          const visible = manager.getVisiblePlacements();
          expect(visible.find(p => p.placementId === placementId)).toBeUndefined();
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 120: Region erase removes multiple placeholders
   * For any region containing multiple Unicode placeholders, erasing the region should remove all associated placements
   * Validates: Requirements 36.3
   */
  it('Property 120: Region erase removes multiple placeholders', () => {
    fc.assert(
      fc.property(
        // Generate a region to erase
        fc.integer({ min: 0, max: 100 }), // startRow
        fc.integer({ min: 0, max: 100 }), // startCol
        fc.integer({ min: 1, max: 50 }),  // regionHeight
        fc.integer({ min: 1, max: 50 }),  // regionWidth
        // Generate placements within and outside the region
        fc.array(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            imageId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 200 }),
            col: fc.integer({ min: 0, max: 200 }),
            width: fc.integer({ min: 1, max: 10 }),
            height: fc.integer({ min: 1, max: 10 }),
            unicodePlaceholder: fc.string({ minLength: 1, maxLength: 1 })
          }),
          { minLength: 1, maxLength: 20 }
        ),
        (startRow, startCol, regionHeight, regionWidth, placements) => {
          const manager = new ImageManager();
          const endRow = startRow + regionHeight - 1;
          const endCol = startCol + regionWidth - 1;
          
          // Create all placements
          const placementsInRegion: number[] = [];
          const placementsOutsideRegion: number[] = [];
          
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height,
              unicodePlaceholder: p.unicodePlaceholder
            };
            manager.createPlacement(placement);
            
            // Determine if this placement is in the erase region
            if (p.row >= startRow && p.row <= endRow && p.col >= startCol && p.col <= endCol) {
              placementsInRegion.push(p.placementId);
            } else {
              placementsOutsideRegion.push(p.placementId);
            }
          }
          
          // Erase the region
          manager.handleRegionOverwrite(startRow, endRow, startCol, endCol);
          
          // Verify placements in the region were removed
          for (const placementId of placementsInRegion) {
            expect(manager.getPlacement(placementId)).toBeUndefined();
          }
          
          // Verify placements outside the region still exist
          for (const placementId of placementsOutsideRegion) {
            expect(manager.getPlacement(placementId)).toBeDefined();
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 121: Placeholder scroll moves image
   * For any Unicode placeholder that scrolls, the image placement should move with it
   * Validates: Requirements 36.4
   */
  it('Property 121: Placeholder scroll moves image (scroll up)', () => {
    fc.assert(
      fc.property(
        // Generate random placement parameters
        fc.integer({ min: 1, max: 10000 }), // placementId
        fc.integer({ min: 1, max: 10000 }), // imageId
        fc.integer({ min: 5, max: 50 }),    // row (start above bottom to allow scrolling)
        fc.integer({ min: 0, max: 200 }),   // col
        fc.integer({ min: 1, max: 10 }),    // width
        fc.integer({ min: 1, max: 10 }),    // height
        fc.string({ minLength: 1, maxLength: 1 }), // unicodePlaceholder
        fc.integer({ min: 1, max: 5 }),     // lines to scroll
        fc.integer({ min: 60, max: 100 }),  // screenRows
        (placementId, imageId, row, col, width, height, unicodePlaceholder, scrollLines, screenRows) => {
          const manager = new ImageManager();
          
          // Create a placement with Unicode placeholder
          const placement: ImagePlacement = {
            placementId,
            imageId,
            row,
            col,
            width,
            height,
            unicodePlaceholder
          };
          
          manager.createPlacement(placement);
          
          // Verify initial state
          expect(manager.getPlacementAtCell(row, col)).toBe(placementId);
          
          // Scroll up
          manager.handleScroll('up', scrollLines, screenRows, false);
          
          const newRow = row - scrollLines;
          
          if (newRow >= 0) {
            // Placement should still be on screen at new position
            const retrieved = manager.getPlacement(placementId);
            expect(retrieved).toBeDefined();
            expect(retrieved?.row).toBe(newRow);
            
            // Placeholder association should have moved
            expect(manager.getPlacementAtCell(newRow, col)).toBe(placementId);
            expect(manager.getPlacementAtCell(row, col)).toBeUndefined();
          } else {
            // Placement scrolled off top - should be in scrollback (not alternate screen)
            const scrollbackPlacements = manager.getScrollbackPlacements();
            const inScrollback = scrollbackPlacements.find(p => p.placementId === placementId);
            expect(inScrollback).toBeDefined();
            
            // Should not be in visible placements
            const visible = manager.getVisiblePlacements();
            expect(visible.find(p => p.placementId === placementId)).toBeUndefined();
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 121: Placeholder scroll down moves image
   * For any Unicode placeholder that scrolls down, the image placement should move with it
   * Validates: Requirements 36.4
   */
  it('Property 121: Placeholder scroll down moves image', () => {
    fc.assert(
      fc.property(
        // Generate random placement parameters
        fc.integer({ min: 1, max: 10000 }), // placementId
        fc.integer({ min: 1, max: 10000 }), // imageId
        fc.integer({ min: 0, max: 40 }),    // row (start near top)
        fc.integer({ min: 0, max: 200 }),   // col
        fc.integer({ min: 1, max: 10 }),    // width
        fc.integer({ min: 1, max: 10 }),    // height
        fc.string({ minLength: 1, maxLength: 1 }), // unicodePlaceholder
        fc.integer({ min: 1, max: 5 }),     // lines to scroll
        fc.integer({ min: 50, max: 100 }),  // screenRows
        (placementId, imageId, row, col, width, height, unicodePlaceholder, scrollLines, screenRows) => {
          const manager = new ImageManager();
          
          // Create a placement with Unicode placeholder
          const placement: ImagePlacement = {
            placementId,
            imageId,
            row,
            col,
            width,
            height,
            unicodePlaceholder
          };
          
          manager.createPlacement(placement);
          
          // Verify initial state
          expect(manager.getPlacementAtCell(row, col)).toBe(placementId);
          
          // Scroll down (reverse scroll)
          manager.handleScroll('down', scrollLines, screenRows, false);
          
          const newRow = row + scrollLines;
          
          if (newRow < screenRows) {
            // Placement should still be on screen at new position
            const retrieved = manager.getPlacement(placementId);
            expect(retrieved).toBeDefined();
            expect(retrieved?.row).toBe(newRow);
            
            // Placeholder association should have moved
            expect(manager.getPlacementAtCell(newRow, col)).toBe(placementId);
            expect(manager.getPlacementAtCell(row, col)).toBeUndefined();
          } else {
            // Placement scrolled off bottom - should be removed
            expect(manager.getPlacement(placementId)).toBeUndefined();
            
            // Should not be in visible placements
            const visible = manager.getVisiblePlacements();
            expect(visible.find(p => p.placementId === placementId)).toBeUndefined();
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 121: Multiple placeholders scroll together
   * For any set of Unicode placeholders that scroll, all should move together maintaining relative positions
   * Validates: Requirements 36.4
   */
  it('Property 121: Multiple placeholders scroll together', () => {
    fc.assert(
      fc.property(
        // Generate multiple placements with unique IDs and positions
        fc.array(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            imageId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 5, max: 40 }),
            col: fc.integer({ min: 0, max: 200 }),
            width: fc.integer({ min: 1, max: 10 }),
            height: fc.integer({ min: 1, max: 10 }),
            unicodePlaceholder: fc.string({ minLength: 1, maxLength: 1 })
          }),
          { minLength: 2, maxLength: 10 }
        ).map(arr => {
          // Ensure unique placement IDs and positions
          const seen = new Set<string>();
          const unique: typeof arr = [];
          let nextId = 1;
          
          for (const item of arr) {
            const posKey = `${item.row},${item.col}`;
            if (!seen.has(posKey)) {
              seen.add(posKey);
              unique.push({ ...item, placementId: nextId++ });
            }
          }
          
          return unique.length >= 2 ? unique : [
            { placementId: 1, imageId: 1, row: 10, col: 0, width: 1, height: 1, unicodePlaceholder: 'A' },
            { placementId: 2, imageId: 1, row: 11, col: 0, width: 1, height: 1, unicodePlaceholder: 'B' }
          ];
        }),
        fc.integer({ min: 1, max: 3 }),     // lines to scroll
        fc.integer({ min: 50, max: 100 }),  // screenRows
        (placements, scrollLines, screenRows) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height,
              unicodePlaceholder: p.unicodePlaceholder
            };
            manager.createPlacement(placement);
          }
          
          // Scroll up
          manager.handleScroll('up', scrollLines, screenRows, false);
          
          // Verify all placements moved together
          for (const p of placements) {
            const newRow = p.row - scrollLines;
            
            if (newRow >= 0) {
              // Should be at new position
              const retrieved = manager.getPlacement(p.placementId);
              expect(retrieved?.row).toBe(newRow);
              expect(manager.getPlacementAtCell(newRow, p.col)).toBe(p.placementId);
            } else {
              // Should be in scrollback
              const scrollbackPlacements = manager.getScrollbackPlacements();
              const inScrollback = scrollbackPlacements.find(pl => pl.placementId === p.placementId);
              expect(inScrollback).toBeDefined();
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 122: Placeholder overwrite removes image
   * For any Unicode placeholder overwritten by text, the associated image placement should be removed
   * Validates: Requirements 36.5
   */
  it('Property 122: Placeholder overwrite removes image', () => {
    fc.assert(
      fc.property(
        // Generate random placement parameters
        fc.integer({ min: 1, max: 10000 }), // placementId
        fc.integer({ min: 1, max: 10000 }), // imageId
        fc.integer({ min: 0, max: 200 }),   // row
        fc.integer({ min: 0, max: 200 }),   // col
        fc.integer({ min: 1, max: 100 }),   // width
        fc.integer({ min: 1, max: 100 }),   // height
        fc.string({ minLength: 1, maxLength: 1 }), // unicodePlaceholder
        (placementId, imageId, row, col, width, height, unicodePlaceholder) => {
          const manager = new ImageManager();
          
          // Create a placement with Unicode placeholder
          const placement: ImagePlacement = {
            placementId,
            imageId,
            row,
            col,
            width,
            height,
            unicodePlaceholder
          };
          
          manager.createPlacement(placement);
          
          // Verify the placement exists
          expect(manager.getPlacement(placementId)).toBeDefined();
          expect(manager.getPlacementAtCell(row, col)).toBe(placementId);
          
          // Overwrite the cell (simulating text being written over the placeholder)
          manager.handleCellOverwrite(row, col);
          
          // Verify the placement was removed
          expect(manager.getPlacement(placementId)).toBeUndefined();
          
          // Verify the placeholder association was removed
          expect(manager.getPlacementAtCell(row, col)).toBeUndefined();
          
          // Verify the placement is no longer in visible placements
          const visible = manager.getVisiblePlacements();
          expect(visible.find(p => p.placementId === placementId)).toBeUndefined();
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 122: Multiple placeholder overwrites
   * For any set of Unicode placeholders that are overwritten, all associated placements should be removed
   * Validates: Requirements 36.5
   */
  it('Property 122: Multiple placeholder overwrites', () => {
    fc.assert(
      fc.property(
        // Generate multiple placements with unique IDs and positions
        fc.array(
          fc.record({
            placementId: fc.integer({ min: 1, max: 10000 }),
            imageId: fc.integer({ min: 1, max: 10000 }),
            row: fc.integer({ min: 0, max: 200 }),
            col: fc.integer({ min: 0, max: 200 }),
            width: fc.integer({ min: 1, max: 10 }),
            height: fc.integer({ min: 1, max: 10 }),
            unicodePlaceholder: fc.string({ minLength: 1, maxLength: 1 })
          }),
          { minLength: 2, maxLength: 20 }
        ).map(arr => {
          // Ensure unique placement IDs and positions
          const seen = new Set<string>();
          const unique: typeof arr = [];
          let nextId = 1;
          
          for (const item of arr) {
            const posKey = `${item.row},${item.col}`;
            if (!seen.has(posKey)) {
              seen.add(posKey);
              unique.push({ ...item, placementId: nextId++ });
            }
          }
          
          return unique.length >= 2 ? unique : [
            { placementId: 1, imageId: 1, row: 0, col: 0, width: 1, height: 1, unicodePlaceholder: 'A' },
            { placementId: 2, imageId: 1, row: 0, col: 1, width: 1, height: 1, unicodePlaceholder: 'B' }
          ];
        }),
        // Select which placements to overwrite
        fc.integer({ min: 0, max: 100 }), // percentage to overwrite
        (placements, overwritePercentage) => {
          const manager = new ImageManager();
          
          // Create all placements
          for (const p of placements) {
            const placement: ImagePlacement = {
              placementId: p.placementId,
              imageId: p.imageId,
              row: p.row,
              col: p.col,
              width: p.width,
              height: p.height,
              unicodePlaceholder: p.unicodePlaceholder
            };
            manager.createPlacement(placement);
          }
          
          // Determine which placements to overwrite
          const numToOverwrite = Math.floor(placements.length * overwritePercentage / 100);
          const toOverwrite = placements.slice(0, numToOverwrite);
          const toKeep = placements.slice(numToOverwrite);
          
          // Overwrite selected placements
          for (const p of toOverwrite) {
            manager.handleCellOverwrite(p.row, p.col);
          }
          
          // Verify overwritten placements were removed
          for (const p of toOverwrite) {
            expect(manager.getPlacement(p.placementId)).toBeUndefined();
            expect(manager.getPlacementAtCell(p.row, p.col)).toBeUndefined();
          }
          
          // Verify kept placements still exist
          for (const p of toKeep) {
            expect(manager.getPlacement(p.placementId)).toBeDefined();
            expect(manager.getPlacementAtCell(p.row, p.col)).toBe(p.placementId);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 122: Placeholder overwrite is independent
   * For any placement without a Unicode placeholder, cell overwrites should not affect it
   * Validates: Requirements 36.5
   */
  it('Property 122: Placeholder overwrite is independent', () => {
    fc.assert(
      fc.property(
        // Generate two placements: one with placeholder, one without
        fc.record({
          placementId: fc.integer({ min: 1, max: 10000 }),
          imageId: fc.integer({ min: 1, max: 10000 }),
          row: fc.integer({ min: 0, max: 200 }),
          col: fc.integer({ min: 0, max: 200 }),
          width: fc.integer({ min: 1, max: 10 }),
          height: fc.integer({ min: 1, max: 10 }),
          unicodePlaceholder: fc.string({ minLength: 1, maxLength: 1 })
        }),
        fc.record({
          placementId: fc.integer({ min: 1, max: 10000 }),
          imageId: fc.integer({ min: 1, max: 10000 }),
          row: fc.integer({ min: 0, max: 200 }),
          col: fc.integer({ min: 0, max: 200 }),
          width: fc.integer({ min: 1, max: 10 }),
          height: fc.integer({ min: 1, max: 10 })
        }),
        (withPlaceholder, withoutPlaceholder) => {
          // Ensure different placement IDs and positions
          if (withPlaceholder.placementId === withoutPlaceholder.placementId) {
            return; // Skip this test case
          }
          if (withPlaceholder.row === withoutPlaceholder.row && withPlaceholder.col === withoutPlaceholder.col) {
            return; // Skip this test case
          }
          
          const manager = new ImageManager();
          
          // Create placement with placeholder
          const placement1: ImagePlacement = {
            placementId: withPlaceholder.placementId,
            imageId: withPlaceholder.imageId,
            row: withPlaceholder.row,
            col: withPlaceholder.col,
            width: withPlaceholder.width,
            height: withPlaceholder.height,
            unicodePlaceholder: withPlaceholder.unicodePlaceholder
          };
          manager.createPlacement(placement1);
          
          // Create placement without placeholder
          const placement2: ImagePlacement = {
            placementId: withoutPlaceholder.placementId,
            imageId: withoutPlaceholder.imageId,
            row: withoutPlaceholder.row,
            col: withoutPlaceholder.col,
            width: withoutPlaceholder.width,
            height: withoutPlaceholder.height
          };
          manager.createPlacement(placement2);
          
          // Overwrite the cell with the placeholder
          manager.handleCellOverwrite(withPlaceholder.row, withPlaceholder.col);
          
          // Verify placement with placeholder was removed
          expect(manager.getPlacement(withPlaceholder.placementId)).toBeUndefined();
          
          // Verify placement without placeholder still exists
          expect(manager.getPlacement(withoutPlaceholder.placementId)).toBeDefined();
          
          // Overwrite the cell without placeholder - should have no effect
          manager.handleCellOverwrite(withoutPlaceholder.row, withoutPlaceholder.col);
          
          // Verify placement without placeholder still exists (not affected by cell overwrite)
          expect(manager.getPlacement(withoutPlaceholder.placementId)).toBeDefined();
        }
      ),
      { numRuns: 100 }
    );
  });
