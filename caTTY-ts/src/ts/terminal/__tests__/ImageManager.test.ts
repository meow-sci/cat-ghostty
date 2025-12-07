/**
 * Unit tests for ImageManager.
 * Tests image and placement storage, retrieval, and lifecycle management.
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { ImageManager } from '../graphics/ImageManager.js';
import type { ImagePlacement } from '../types.js';

describe('ImageManager', () => {
  let manager: ImageManager;

  beforeEach(() => {
    manager = new ImageManager();
  });

  describe('Image Storage', () => {
    it('should store and retrieve an image', () => {
      // Create a mock ImageBitmap
      const mockBitmap = {} as ImageBitmap;
      
      manager.storeImage(1, mockBitmap, 'png', 100, 100);
      
      const retrieved = manager.getImage(1);
      expect(retrieved).toBeDefined();
      expect(retrieved?.id).toBe(1);
      expect(retrieved?.data).toBe(mockBitmap);
      expect(retrieved?.format).toBe('png');
      expect(retrieved?.width).toBe(100);
      expect(retrieved?.height).toBe(100);
    });

    it('should replace an existing image with the same ID', () => {
      const mockBitmap1 = {} as ImageBitmap;
      const mockBitmap2 = {} as ImageBitmap;
      
      manager.storeImage(1, mockBitmap1, 'png', 100, 100);
      manager.storeImage(1, mockBitmap2, 'jpeg', 200, 200);
      
      const retrieved = manager.getImage(1);
      expect(retrieved?.data).toBe(mockBitmap2);
      expect(retrieved?.format).toBe('jpeg');
      expect(retrieved?.width).toBe(200);
    });

    it('should return undefined for non-existent image', () => {
      const retrieved = manager.getImage(999);
      expect(retrieved).toBeUndefined();
    });

    it('should delete an image', () => {
      const mockBitmap = {} as ImageBitmap;
      
      manager.storeImage(1, mockBitmap, 'png', 100, 100);
      manager.deleteImage(1);
      
      const retrieved = manager.getImage(1);
      expect(retrieved).toBeUndefined();
    });
  });

  describe('Placement Management', () => {
    it('should create and retrieve a placement', () => {
      const placement: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 10,
        width: 20,
        height: 10
      };
      
      manager.createPlacement(placement);
      
      const retrieved = manager.getPlacement(1);
      expect(retrieved).toEqual(placement);
    });

    it('should add placement to visible placements', () => {
      const placement: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 10,
        width: 20,
        height: 10
      };
      
      manager.createPlacement(placement);
      
      const visible = manager.getVisiblePlacements();
      expect(visible).toHaveLength(1);
      expect(visible[0]).toEqual(placement);
    });

    it('should replace existing placement with same ID', () => {
      const placement1: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 10,
        width: 20,
        height: 10
      };
      
      const placement2: ImagePlacement = {
        placementId: 1,
        imageId: 2,
        row: 15,
        col: 20,
        width: 30,
        height: 15
      };
      
      manager.createPlacement(placement1);
      manager.createPlacement(placement2);
      
      const visible = manager.getVisiblePlacements();
      expect(visible).toHaveLength(1);
      expect(visible[0]).toEqual(placement2);
    });

    it('should delete a placement', () => {
      const placement: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 10,
        width: 20,
        height: 10
      };
      
      manager.createPlacement(placement);
      manager.deletePlacement(1);
      
      const retrieved = manager.getPlacement(1);
      expect(retrieved).toBeUndefined();
      
      const visible = manager.getVisiblePlacements();
      expect(visible).toHaveLength(0);
    });

    it('should delete all visible placements', () => {
      const placement1: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 10,
        width: 20,
        height: 10
      };
      
      const placement2: ImagePlacement = {
        placementId: 2,
        imageId: 1,
        row: 15,
        col: 20,
        width: 30,
        height: 15
      };
      
      manager.createPlacement(placement1);
      manager.createPlacement(placement2);
      manager.deleteAllPlacements();
      
      const visible = manager.getVisiblePlacements();
      expect(visible).toHaveLength(0);
    });
  });

  describe('Image Deletion with Placements', () => {
    it('should delete all placements when deleting an image', () => {
      const mockBitmap = {} as ImageBitmap;
      manager.storeImage(1, mockBitmap, 'png', 100, 100);
      
      const placement1: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 10,
        width: 20,
        height: 10
      };
      
      const placement2: ImagePlacement = {
        placementId: 2,
        imageId: 1,
        row: 15,
        col: 20,
        width: 30,
        height: 15
      };
      
      manager.createPlacement(placement1);
      manager.createPlacement(placement2);
      
      manager.deleteImage(1);
      
      expect(manager.getImage(1)).toBeUndefined();
      expect(manager.getPlacement(1)).toBeUndefined();
      expect(manager.getPlacement(2)).toBeUndefined();
      expect(manager.getVisiblePlacements()).toHaveLength(0);
    });

    it('should not delete placements of other images', () => {
      const mockBitmap1 = {} as ImageBitmap;
      const mockBitmap2 = {} as ImageBitmap;
      
      manager.storeImage(1, mockBitmap1, 'png', 100, 100);
      manager.storeImage(2, mockBitmap2, 'png', 100, 100);
      
      const placement1: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 10,
        width: 20,
        height: 10
      };
      
      const placement2: ImagePlacement = {
        placementId: 2,
        imageId: 2,
        row: 15,
        col: 20,
        width: 30,
        height: 15
      };
      
      manager.createPlacement(placement1);
      manager.createPlacement(placement2);
      
      manager.deleteImage(1);
      
      expect(manager.getPlacement(1)).toBeUndefined();
      expect(manager.getPlacement(2)).toBeDefined();
      expect(manager.getVisiblePlacements()).toHaveLength(1);
    });
  });

  describe('Scrollback Management', () => {
    it('should return empty scrollback initially', () => {
      const scrollback = manager.getScrollbackPlacements();
      expect(scrollback).toHaveLength(0);
    });

    it('should move placements to scrollback when scrolling up', () => {
      const placement: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 2,
        col: 10,
        width: 20,
        height: 10
      };
      
      manager.createPlacement(placement);
      manager.handleScroll('up', 5, 24);
      
      const visible = manager.getVisiblePlacements();
      const scrollback = manager.getScrollbackPlacements();
      
      expect(visible).toHaveLength(0);
      expect(scrollback).toHaveLength(1);
      expect(scrollback[0].row).toBe(-3); // 2 - 5 = -3
    });

    it('should update placement positions when scrolling up', () => {
      const placement: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 10,
        col: 10,
        width: 20,
        height: 10
      };
      
      manager.createPlacement(placement);
      manager.handleScroll('up', 3, 24);
      
      const visible = manager.getVisiblePlacements();
      expect(visible).toHaveLength(1);
      expect(visible[0].row).toBe(7); // 10 - 3 = 7
    });

    it('should remove placements when scrolling down past bottom', () => {
      const placement: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 20,
        col: 10,
        width: 20,
        height: 10
      };
      
      manager.createPlacement(placement);
      manager.handleScroll('down', 5, 24);
      
      const visible = manager.getVisiblePlacements();
      expect(visible).toHaveLength(0);
      expect(manager.getPlacement(1)).toBeUndefined();
    });
  });

  describe('Clear Operations', () => {
    it('should clear all placements when clearing screen', () => {
      const placement1: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 10,
        width: 20,
        height: 10
      };
      
      const placement2: ImagePlacement = {
        placementId: 2,
        imageId: 1,
        row: 15,
        col: 20,
        width: 30,
        height: 15
      };
      
      manager.createPlacement(placement1);
      manager.createPlacement(placement2);
      
      manager.handleClear('screen');
      
      expect(manager.getVisiblePlacements()).toHaveLength(0);
    });

    it('should clear placements on a specific line', () => {
      const placement1: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 10,
        width: 20,
        height: 2
      };
      
      const placement2: ImagePlacement = {
        placementId: 2,
        imageId: 1,
        row: 10,
        col: 20,
        width: 30,
        height: 2
      };
      
      manager.createPlacement(placement1);
      manager.createPlacement(placement2);
      
      manager.handleClear('line', 5);
      
      const visible = manager.getVisiblePlacements();
      expect(visible).toHaveLength(1);
      expect(visible[0].placementId).toBe(2);
    });
  });

  describe('Resize Operations', () => {
    it('should remove placements outside new bounds', () => {
      const placement1: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 10,
        width: 20,
        height: 10
      };
      
      const placement2: ImagePlacement = {
        placementId: 2,
        imageId: 1,
        row: 50,
        col: 20,
        width: 30,
        height: 15
      };
      
      manager.createPlacement(placement1);
      manager.createPlacement(placement2);
      
      manager.handleResize(80, 60, 80, 40);
      
      const visible = manager.getVisiblePlacements();
      expect(visible).toHaveLength(1);
      expect(visible[0].placementId).toBe(1);
    });
  });

  describe('Chunked Transmission', () => {
    it('should track chunked transmission', () => {
      manager.startTransmission(1, 'png');
      
      const transmission = manager.getTransmission(1);
      expect(transmission).toBeDefined();
      expect(transmission?.imageId).toBe(1);
      expect(transmission?.format).toBe('png');
      expect(transmission?.complete).toBe(false);
    });

    it('should accumulate chunks', () => {
      manager.startTransmission(1, 'png');
      
      const chunk1 = new Uint8Array([1, 2, 3]);
      const chunk2 = new Uint8Array([4, 5, 6]);
      
      manager.addChunk(1, chunk1);
      manager.addChunk(1, chunk2);
      
      const transmission = manager.getTransmission(1);
      expect(transmission?.chunks).toHaveLength(2);
    });

    it('should combine chunks on completion', () => {
      manager.startTransmission(1, 'png');
      
      const chunk1 = new Uint8Array([1, 2, 3]);
      const chunk2 = new Uint8Array([4, 5, 6]);
      
      manager.addChunk(1, chunk1);
      manager.addChunk(1, chunk2);
      
      const combined = manager.completeTransmission(1);
      
      expect(combined).toBeDefined();
      expect(combined?.length).toBe(6);
      expect(Array.from(combined!)).toEqual([1, 2, 3, 4, 5, 6]);
      expect(manager.getTransmission(1)).toBeUndefined();
    });

    it('should cancel transmission', () => {
      manager.startTransmission(1, 'png');
      manager.addChunk(1, new Uint8Array([1, 2, 3]));
      
      manager.cancelTransmission(1);
      
      expect(manager.getTransmission(1)).toBeUndefined();
    });
  });

  describe('Clear All', () => {
    it('should clear all data', () => {
      const mockBitmap = {} as ImageBitmap;
      manager.storeImage(1, mockBitmap, 'png', 100, 100);
      
      const placement: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 10,
        width: 20,
        height: 10
      };
      
      manager.createPlacement(placement);
      manager.startTransmission(2, 'jpeg');
      
      manager.clear();
      
      expect(manager.getImage(1)).toBeUndefined();
      expect(manager.getPlacement(1)).toBeUndefined();
      expect(manager.getVisiblePlacements()).toHaveLength(0);
      expect(manager.getScrollbackPlacements()).toHaveLength(0);
      expect(manager.getTransmission(2)).toBeUndefined();
    });
  });
});
