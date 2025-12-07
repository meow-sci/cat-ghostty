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
          manager.storeImage(imageId, mockBitmap, format, width, height);
          
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
          manager.storeImage(imageId, mockBitmap1, format1, width1, height1);
          
          // Store second image with same ID
          const mockBitmap2 = new (globalThis as any).ImageBitmap(width2, height2);
          manager.storeImage(imageId, mockBitmap2, format2, width2, height2);
          
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
            manager.storeImage(img.id, mockBitmap, img.format, img.width, img.height);
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
          manager.storeImage(imageId, mockBitmap, format, width, height);
          
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
            manager.storeImage(imageIds[i], mockBitmap, format, 100, 100);
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
});
