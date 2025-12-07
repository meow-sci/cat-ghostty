/**
 * Tests for ImageManager line insertion and deletion operations.
 * Validates Requirements 33.3 and 33.4.
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { ImageManager } from '../graphics/ImageManager.js';
import type { ImagePlacement } from '../types.js';

describe('ImageManager - Line Operations', () => {
  let manager: ImageManager;

  beforeEach(() => {
    manager = new ImageManager();
  });

  describe('handleLineInsertion', () => {
    it('should shift placements down when lines are inserted', () => {
      // Create placements at rows 5, 10, 15
      const placement1: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 0,
        width: 10,
        height: 5
      };
      const placement2: ImagePlacement = {
        placementId: 2,
        imageId: 1,
        row: 10,
        col: 0,
        width: 10,
        height: 5
      };
      const placement3: ImagePlacement = {
        placementId: 3,
        imageId: 1,
        row: 15,
        col: 0,
        width: 10,
        height: 5
      };

      manager.createPlacement(placement1);
      manager.createPlacement(placement2);
      manager.createPlacement(placement3);

      // Insert 3 lines at row 8
      manager.handleLineInsertion(8, 3, 24);

      // Placement at row 5 should stay at row 5 (above insertion point)
      const p1 = manager.getPlacement(1);
      expect(p1?.row).toBe(5);

      // Placement at row 10 should move to row 13 (10 + 3)
      const p2 = manager.getPlacement(2);
      expect(p2?.row).toBe(13);

      // Placement at row 15 should move to row 18 (15 + 3)
      const p3 = manager.getPlacement(3);
      expect(p3?.row).toBe(18);
    });

    it('should remove placements that go off the bottom of the screen', () => {
      // Create placement at row 22
      const placement: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 22,
        col: 0,
        width: 10,
        height: 5
      };

      manager.createPlacement(placement);

      // Insert 3 lines at row 20 (screen has 24 rows)
      // Placement at row 22 would move to row 25, which is off-screen
      manager.handleLineInsertion(20, 3, 24);

      // Placement should be removed
      const p = manager.getPlacement(1);
      expect(p).toBeUndefined();
    });
  });

  describe('handleLineDeletion', () => {
    it('should shift placements up when lines are deleted', () => {
      // Create placements at rows 5, 15, 20
      const placement1: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 0,
        width: 10,
        height: 2
      };
      const placement2: ImagePlacement = {
        placementId: 2,
        imageId: 1,
        row: 15,
        col: 0,
        width: 10,
        height: 2
      };
      const placement3: ImagePlacement = {
        placementId: 3,
        imageId: 1,
        row: 20,
        col: 0,
        width: 10,
        height: 2
      };

      manager.createPlacement(placement1);
      manager.createPlacement(placement2);
      manager.createPlacement(placement3);

      // Delete 3 lines starting at row 8
      manager.handleLineDeletion(8, 3);

      // Placement at row 5 should stay at row 5 (above deletion point)
      const p1 = manager.getPlacement(1);
      expect(p1?.row).toBe(5);

      // Placement at row 15 should move to row 12 (15 - 3)
      const p2 = manager.getPlacement(2);
      expect(p2?.row).toBe(12);

      // Placement at row 20 should move to row 17 (20 - 3)
      const p3 = manager.getPlacement(3);
      expect(p3?.row).toBe(17);
    });

    it('should remove placements in deleted lines', () => {
      // Create placements at rows 5, 10, 15
      const placement1: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 5,
        col: 0,
        width: 10,
        height: 2
      };
      const placement2: ImagePlacement = {
        placementId: 2,
        imageId: 1,
        row: 10,
        col: 0,
        width: 10,
        height: 2
      };
      const placement3: ImagePlacement = {
        placementId: 3,
        imageId: 1,
        row: 15,
        col: 0,
        width: 10,
        height: 2
      };

      manager.createPlacement(placement1);
      manager.createPlacement(placement2);
      manager.createPlacement(placement3);

      // Delete 3 lines starting at row 9 (rows 9, 10, 11)
      manager.handleLineDeletion(9, 3);

      // Placement at row 5 should still exist
      const p1 = manager.getPlacement(1);
      expect(p1).toBeDefined();
      expect(p1?.row).toBe(5);

      // Placement at row 10 should be removed (in deleted region)
      const p2 = manager.getPlacement(2);
      expect(p2).toBeUndefined();

      // Placement at row 15 should move to row 12 (15 - 3)
      const p3 = manager.getPlacement(3);
      expect(p3).toBeDefined();
      expect(p3?.row).toBe(12);
    });

    it('should remove placements that partially overlap deleted lines', () => {
      // Create placement at row 8 with height 5 (spans rows 8-12)
      const placement: ImagePlacement = {
        placementId: 1,
        imageId: 1,
        row: 8,
        col: 0,
        width: 10,
        height: 5
      };

      manager.createPlacement(placement);

      // Delete 2 lines starting at row 10 (rows 10, 11)
      // Placement spans rows 8-12, so it overlaps with deleted rows
      manager.handleLineDeletion(10, 2);

      // Placement should be removed because it overlaps
      const p = manager.getPlacement(1);
      expect(p).toBeUndefined();
    });
  });
});
