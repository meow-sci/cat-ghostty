/**
 * Unit tests for ImageManager ID generation.
 * Tests automatic ID generation for images and placements.
 * 
 * @vitest-environment jsdom
 */

import { describe, it, expect, beforeAll } from 'vitest';
import { ImageManager } from '../graphics/ImageManager.js';
import type { ImagePlacement } from '../types.js';

describe('ImageManager ID Generation', () => {
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

  it('should generate sequential image IDs starting from 1', () => {
    const manager = new ImageManager();
    
    const id1 = manager.generateImageId();
    const id2 = manager.generateImageId();
    const id3 = manager.generateImageId();
    
    expect(id1).toBe(1);
    expect(id2).toBe(2);
    expect(id3).toBe(3);
  });

  it('should generate sequential placement IDs starting from 1', () => {
    const manager = new ImageManager();
    
    const id1 = manager.generatePlacementId();
    const id2 = manager.generatePlacementId();
    const id3 = manager.generatePlacementId();
    
    expect(id1).toBe(1);
    expect(id2).toBe(2);
    expect(id3).toBe(3);
  });

  it('should update image ID counter when storing image with explicit ID', () => {
    const manager = new ImageManager();
    
    // Generate first few IDs
    expect(manager.generateImageId()).toBe(1);
    expect(manager.generateImageId()).toBe(2);
    
    // Store an image with explicit ID 100
    const mockBitmap = new (globalThis as any).ImageBitmap(100, 100);
    manager.storeImage(100, mockBitmap, 'png', 100, 100);
    
    // Next generated ID should be 101
    expect(manager.generateImageId()).toBe(101);
  });

  it('should update placement ID counter when creating placement with explicit ID', () => {
    const manager = new ImageManager();
    
    // Generate first few IDs
    expect(manager.generatePlacementId()).toBe(1);
    expect(manager.generatePlacementId()).toBe(2);
    
    // Create a placement with explicit ID 200
    const placement: ImagePlacement = {
      placementId: 200,
      imageId: 1,
      row: 0,
      col: 0,
      width: 10,
      height: 10
    };
    manager.createPlacement(placement);
    
    // Next generated ID should be 201
    expect(manager.generatePlacementId()).toBe(201);
  });

  it('should not update counter when storing image with ID less than current counter', () => {
    const manager = new ImageManager();
    
    // Generate IDs up to 10
    for (let i = 0; i < 10; i++) {
      manager.generateImageId();
    }
    
    // Store an image with ID 5 (less than current counter of 11)
    const mockBitmap = new (globalThis as any).ImageBitmap(100, 100);
    manager.storeImage(5, mockBitmap, 'png', 100, 100);
    
    // Next generated ID should still be 11
    expect(manager.generateImageId()).toBe(11);
  });

  it('should not update counter when creating placement with ID less than current counter', () => {
    const manager = new ImageManager();
    
    // Generate IDs up to 10
    for (let i = 0; i < 10; i++) {
      manager.generatePlacementId();
    }
    
    // Create a placement with ID 5 (less than current counter of 11)
    const placement: ImagePlacement = {
      placementId: 5,
      imageId: 1,
      row: 0,
      col: 0,
      width: 10,
      height: 10
    };
    manager.createPlacement(placement);
    
    // Next generated ID should still be 11
    expect(manager.generatePlacementId()).toBe(11);
  });

  it('should handle image ID reuse correctly', () => {
    const manager = new ImageManager();
    
    const mockBitmap1 = new (globalThis as any).ImageBitmap(100, 100);
    const mockBitmap2 = new (globalThis as any).ImageBitmap(200, 200);
    
    // Store image with ID 50
    manager.storeImage(50, mockBitmap1, 'png', 100, 100);
    expect(manager.generateImageId()).toBe(51);
    
    // Reuse ID 50 with different image data
    manager.storeImage(50, mockBitmap2, 'png', 200, 200);
    
    // Counter should still be 51 (not affected by reuse)
    expect(manager.generateImageId()).toBe(52);
    
    // Verify the image was replaced
    const retrieved = manager.getImage(50);
    expect(retrieved?.data).toBe(mockBitmap2);
    expect(retrieved?.width).toBe(200);
  });

  it('should handle placement ID reuse correctly', () => {
    const manager = new ImageManager();
    
    // Create placement with ID 50
    const placement1: ImagePlacement = {
      placementId: 50,
      imageId: 1,
      row: 0,
      col: 0,
      width: 10,
      height: 10
    };
    manager.createPlacement(placement1);
    expect(manager.generatePlacementId()).toBe(51);
    
    // Reuse ID 50 with different placement data
    const placement2: ImagePlacement = {
      placementId: 50,
      imageId: 2,
      row: 5,
      col: 5,
      width: 20,
      height: 20
    };
    manager.createPlacement(placement2);
    
    // Counter should still be 51 (not affected by reuse)
    expect(manager.generatePlacementId()).toBe(52);
    
    // Verify the placement was replaced
    const retrieved = manager.getPlacement(50);
    expect(retrieved?.imageId).toBe(2);
    expect(retrieved?.row).toBe(5);
    expect(retrieved?.width).toBe(20);
  });

  it('should maintain independent counters for images and placements', () => {
    const manager = new ImageManager();
    
    // Generate some image IDs
    expect(manager.generateImageId()).toBe(1);
    expect(manager.generateImageId()).toBe(2);
    
    // Generate some placement IDs
    expect(manager.generatePlacementId()).toBe(1);
    expect(manager.generatePlacementId()).toBe(2);
    
    // Store image with high ID
    const mockBitmap = new (globalThis as any).ImageBitmap(100, 100);
    manager.storeImage(100, mockBitmap, 'png', 100, 100);
    
    // Image counter should be updated, but placement counter should not
    expect(manager.generateImageId()).toBe(101);
    expect(manager.generatePlacementId()).toBe(3);
    
    // Create placement with high ID
    const placement: ImagePlacement = {
      placementId: 200,
      imageId: 1,
      row: 0,
      col: 0,
      width: 10,
      height: 10
    };
    manager.createPlacement(placement);
    
    // Placement counter should be updated, but image counter should not
    expect(manager.generatePlacementId()).toBe(201);
    expect(manager.generateImageId()).toBe(102);
  });
});
