/**
 * Image Manager for Kitty Graphics Protocol.
 * Manages storage and lifecycle of images and placements.
 * 
 * The ImageManager is responsible for:
 * - Storing decoded images by ID
 * - Managing image placements (visible and scrollback)
 * - Tracking chunked transmissions
 * - Handling image and placement deletion
 * - Managing placement positions during scrolling and resizing
 */

import type { ImageData, ImagePlacement, TransmissionState } from '../types.js';

/**
 * Manages images and placements for the Kitty Graphics Protocol.
 * Provides storage, retrieval, and lifecycle management for terminal images.
 */
export class ImageManager {
  /** Map of image ID to decoded image data */
  private images: Map<number, ImageData> = new Map();
  
  /** Map of placement ID to placement configuration */
  private placements: Map<number, ImagePlacement> = new Map();
  
  /** List of placements currently visible on screen */
  private activePlacements: ImagePlacement[] = [];
  
  /** List of placements in scrollback buffer */
  private scrollbackPlacements: ImagePlacement[] = [];
  
  /** Map of image ID to transmission state for chunked transfers */
  private transmissions: Map<number, TransmissionState> = new Map();

  /**
   * Store a decoded image by ID.
   * If an image with this ID already exists, it will be replaced.
   * 
   * @param id - Unique image identifier
   * @param data - Decoded image bitmap or element
   * @param format - Image format (png, jpeg, gif)
   * @param width - Image width in pixels
   * @param height - Image height in pixels
   */
  storeImage(
    id: number,
    data: ImageBitmap | HTMLImageElement,
    format: 'png' | 'jpeg' | 'gif',
    width: number,
    height: number
  ): void {
    this.images.set(id, {
      id,
      data,
      width,
      height,
      format
    });
  }

  /**
   * Retrieve an image by ID.
   * 
   * @param id - Image identifier
   * @returns Image data if found, undefined otherwise
   */
  getImage(id: number): ImageData | undefined {
    return this.images.get(id);
  }

  /**
   * Delete an image and all its placements.
   * Frees the image data from memory and removes all placements referencing it.
   * 
   * @param id - Image identifier to delete
   */
  deleteImage(id: number): void {
    // Remove the image data
    const imageData = this.images.get(id);
    if (imageData) {
      // Close ImageBitmap to free memory if applicable
      // Check if ImageBitmap is defined (not available in Node.js test environment)
      if (typeof ImageBitmap !== 'undefined' && imageData.data instanceof ImageBitmap) {
        imageData.data.close();
      }
      this.images.delete(id);
    }

    // Remove all placements of this image
    const placementsToDelete: number[] = [];
    
    for (const [placementId, placement] of this.placements.entries()) {
      if (placement.imageId === id) {
        placementsToDelete.push(placementId);
      }
    }

    for (const placementId of placementsToDelete) {
      this.deletePlacement(placementId);
    }
  }

  /**
   * Create a new image placement.
   * If a placement with this ID already exists, it will be replaced.
   * 
   * @param placement - Placement configuration
   */
  createPlacement(placement: ImagePlacement): void {
    // Store in placements map
    this.placements.set(placement.placementId, placement);
    
    // Add to active placements (visible on screen)
    // Remove existing placement with same ID if present
    this.activePlacements = this.activePlacements.filter(
      p => p.placementId !== placement.placementId
    );
    this.activePlacements.push(placement);
  }

  /**
   * Retrieve a placement by ID.
   * 
   * @param id - Placement identifier
   * @returns Placement configuration if found, undefined otherwise
   */
  getPlacement(id: number): ImagePlacement | undefined {
    return this.placements.get(id);
  }

  /**
   * Delete a specific placement by ID.
   * Removes the placement from both active and scrollback lists.
   * 
   * @param id - Placement identifier to delete
   */
  deletePlacement(id: number): void {
    // Remove from placements map
    this.placements.delete(id);
    
    // Remove from active placements
    this.activePlacements = this.activePlacements.filter(
      p => p.placementId !== id
    );
    
    // Remove from scrollback placements
    this.scrollbackPlacements = this.scrollbackPlacements.filter(
      p => p.placementId !== id
    );
  }

  /**
   * Delete all visible image placements.
   * Removes all placements from the active list but keeps scrollback placements.
   */
  deleteAllPlacements(): void {
    // Get all active placement IDs
    const activeIds = this.activePlacements.map(p => p.placementId);
    
    // Delete each active placement
    for (const id of activeIds) {
      this.placements.delete(id);
    }
    
    // Clear active placements list
    this.activePlacements = [];
  }

  /**
   * Get all visible image placements.
   * Returns placements currently displayed on the screen.
   * 
   * @returns Array of active placements
   */
  getVisiblePlacements(): ImagePlacement[] {
    return [...this.activePlacements];
  }

  /**
   * Get all scrollback image placements.
   * Returns placements that have scrolled off the top of the screen.
   * 
   * @returns Array of scrollback placements
   */
  getScrollbackPlacements(): ImagePlacement[] {
    return [...this.scrollbackPlacements];
  }

  /**
   * Handle scrolling operations.
   * Moves placements between active and scrollback lists based on scroll direction.
   * 
   * @param direction - Scroll direction ('up' or 'down')
   * @param lines - Number of lines scrolled
   * @param screenRows - Total number of rows on screen
   * @param isAlternateScreen - Whether terminal is in alternate screen mode (default: false)
   */
  handleScroll(direction: 'up' | 'down', lines: number, screenRows: number, isAlternateScreen: boolean = false): void {
    if (direction === 'up') {
      // Content scrolls up, placements move up
      // Placements that scroll off the top go to scrollback (unless in alternate screen)
      const movedToScrollback: ImagePlacement[] = [];
      
      this.activePlacements = this.activePlacements.filter(placement => {
        const newRow = placement.row - lines;
        
        if (newRow < 0) {
          // Placement scrolled off the top
          if (!isAlternateScreen) {
            // In primary screen mode, preserve in scrollback
            movedToScrollback.push({
              ...placement,
              row: newRow // Keep negative row to track position in scrollback
            });
          } else {
            // In alternate screen mode, remove the placement entirely
            this.placements.delete(placement.placementId);
          }
          return false;
        }
        
        // Update row position
        placement.row = newRow;
        return true;
      });
      
      // Add to scrollback (only if not in alternate screen mode)
      if (!isAlternateScreen) {
        this.scrollbackPlacements.push(...movedToScrollback);
      }
      
    } else {
      // Content scrolls down (reverse scroll)
      // Placements that scroll off the bottom are removed
      this.activePlacements = this.activePlacements.filter(placement => {
        const newRow = placement.row + lines;
        
        if (newRow >= screenRows) {
          // Placement scrolled off the bottom - remove it
          this.placements.delete(placement.placementId);
          return false;
        }
        
        // Update row position
        placement.row = newRow;
        return true;
      });
    }
  }

  /**
   * Handle clear operations.
   * Removes placements in the cleared region.
   * 
   * @param region - Region to clear ('screen' or 'line')
   * @param row - Row number (for 'line' region)
   * @param startCol - Start column (for partial line clear)
   * @param endCol - End column (for partial line clear)
   */
  handleClear(
    region: 'screen' | 'line',
    row?: number,
    startCol?: number,
    endCol?: number
  ): void {
    if (region === 'screen') {
      // Clear all active placements
      this.deleteAllPlacements();
    } else if (region === 'line' && row !== undefined) {
      // Remove placements on the specified line
      const placementsToDelete: number[] = [];
      
      for (const placement of this.activePlacements) {
        // Check if placement overlaps with the line
        const placementStartRow = placement.row;
        const placementEndRow = placement.row + placement.height - 1;
        
        if (row >= placementStartRow && row <= placementEndRow) {
          // Check column overlap if specified
          if (startCol !== undefined && endCol !== undefined) {
            const placementStartCol = placement.col;
            const placementEndCol = placement.col + placement.width - 1;
            
            // Check if there's any overlap
            if (!(endCol < placementStartCol || startCol > placementEndCol)) {
              placementsToDelete.push(placement.placementId);
            }
          } else {
            // No column specified, delete entire line
            placementsToDelete.push(placement.placementId);
          }
        }
      }
      
      for (const id of placementsToDelete) {
        this.deletePlacement(id);
      }
    }
  }

  /**
   * Handle terminal resize.
   * Repositions placements based on new cell dimensions.
   * Note: This is a simplified implementation. Full implementation would
   * recalculate pixel-to-cell conversions based on new cell size.
   * 
   * @param oldCols - Previous number of columns
   * @param oldRows - Previous number of rows
   * @param newCols - New number of columns
   * @param newRows - New number of rows
   */
  handleResize(oldCols: number, oldRows: number, newCols: number, newRows: number): void {
    // Remove placements that are now out of bounds
    this.activePlacements = this.activePlacements.filter(placement => {
      return placement.row < newRows && placement.col < newCols;
    });
  }

  /**
   * Start a chunked image transmission.
   * Initializes tracking for an image being transmitted in multiple parts.
   * 
   * @param imageId - Image identifier
   * @param format - Image format
   */
  startTransmission(imageId: number, format: string): void {
    this.transmissions.set(imageId, {
      imageId,
      chunks: [],
      format,
      complete: false
    });
  }

  /**
   * Add a chunk to an ongoing transmission.
   * 
   * @param imageId - Image identifier
   * @param chunk - Binary chunk data
   */
  addChunk(imageId: number, chunk: Uint8Array): void {
    const transmission = this.transmissions.get(imageId);
    if (transmission) {
      transmission.chunks.push(chunk);
    }
  }

  /**
   * Complete a chunked transmission.
   * Combines all chunks and returns the complete data.
   * 
   * @param imageId - Image identifier
   * @returns Complete image data as Uint8Array, or undefined if transmission not found
   */
  completeTransmission(imageId: number): Uint8Array | undefined {
    const transmission = this.transmissions.get(imageId);
    if (!transmission) {
      return undefined;
    }

    // Combine all chunks
    const totalLength = transmission.chunks.reduce((sum, chunk) => sum + chunk.length, 0);
    const combined = new Uint8Array(totalLength);
    
    let offset = 0;
    for (const chunk of transmission.chunks) {
      combined.set(chunk, offset);
      offset += chunk.length;
    }

    // Mark as complete and clean up
    transmission.complete = true;
    this.transmissions.delete(imageId);

    return combined;
  }

  /**
   * Cancel an ongoing transmission.
   * Discards all accumulated chunks.
   * 
   * @param imageId - Image identifier
   */
  cancelTransmission(imageId: number): void {
    this.transmissions.delete(imageId);
  }

  /**
   * Get the current transmission state for an image.
   * 
   * @param imageId - Image identifier
   * @returns Transmission state if found, undefined otherwise
   */
  getTransmission(imageId: number): TransmissionState | undefined {
    return this.transmissions.get(imageId);
  }

  /**
   * Clear all images, placements, and transmissions.
   * Useful for terminal reset or testing.
   */
  clear(): void {
    // Close all ImageBitmaps to free memory
    // Check if ImageBitmap is defined (not available in Node.js test environment)
    if (typeof ImageBitmap !== 'undefined') {
      for (const imageData of this.images.values()) {
        if (imageData.data instanceof ImageBitmap) {
          imageData.data.close();
        }
      }
    }

    this.images.clear();
    this.placements.clear();
    this.activePlacements = [];
    this.scrollbackPlacements = [];
    this.transmissions.clear();
  }
}
