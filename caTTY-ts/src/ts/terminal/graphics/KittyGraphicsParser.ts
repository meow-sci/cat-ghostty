/**
 * Kitty Graphics Protocol parser.
 * Parses ESC_G sequences for inline image display.
 * 
 * The Kitty Graphics Protocol allows terminals to display images inline
 * with support for multiple formats, transparency, scrolling, and more.
 * 
 * Escape sequence format: ESC_G<control_data>;<payload>ESC\
 * Where control_data contains key=value pairs separated by commas.
 */

import type { GraphicsParams, ImageData, ImagePlacement } from '../types.js';

/**
 * Parser for Kitty Graphics Protocol escape sequences.
 * Handles image transmission, display, and deletion commands.
 */
export class KittyGraphicsParser {
  private nextImageId = 1;
  private nextPlacementId = 1;

  /**
   * Parse a Kitty graphics command escape sequence.
   * 
   * @param sequence - The complete escape sequence (without ESC_G prefix and ESC\ suffix)
   * @returns Parsed graphics parameters and payload
   */
  parseGraphicsCommand(sequence: string): { params: GraphicsParams; payload: string } | null {
    // Split into control data and payload
    const parts = sequence.split(';');
    if (parts.length === 0) {
      return null;
    }

    const controlData = parts[0];
    const payload = parts.slice(1).join(';');

    // Parse control data into key=value pairs
    const params = this.parseControlData(controlData);
    if (!params) {
      return null;
    }

    return { params, payload };
  }

  /**
   * Parse control data string into GraphicsParams object.
   * Control data format: key=value,key=value,...
   * 
   * @param controlData - The control data string
   * @returns Parsed parameters or null if invalid
   */
  private parseControlData(controlData: string): GraphicsParams | null {
    const params: Partial<GraphicsParams> = {};
    
    // Split by comma to get key=value pairs
    const pairs = controlData.split(',');
    
    for (const pair of pairs) {
      const [key, value] = pair.split('=');
      if (!key || value === undefined) {
        continue;
      }

      // Parse based on key
      switch (key) {
        case 'a': // action
          if (value === 't' || value === 'd' || value === 'D') {
            params.action = value;
          }
          break;
        case 'i': // image ID
          params.imageId = parseInt(value, 10);
          break;
        case 'p': // placement ID
          params.placementId = parseInt(value, 10);
          break;
        case 'f': // format
          params.format = value;
          break;
        case 's': // width (source)
          params.width = parseInt(value, 10);
          break;
        case 'v': // height (source)
          params.height = parseInt(value, 10);
          break;
        case 'x': // x offset
          params.x = parseInt(value, 10);
          break;
        case 'y': // y offset
          params.y = parseInt(value, 10);
          break;
        case 'r': // rows
          params.rows = parseInt(value, 10);
          break;
        case 'c': // columns
          params.cols = parseInt(value, 10);
          break;
        case 'X': // source X
          params.sourceX = parseInt(value, 10);
          break;
        case 'Y': // source Y
          params.sourceY = parseInt(value, 10);
          break;
        case 'w': // source width
          params.sourceWidth = parseInt(value, 10);
          break;
        case 'h': // source height
          params.sourceHeight = parseInt(value, 10);
          break;
        case 'm': // more chunks
          params.more = value === '1';
          break;
        case 'U': // unicode placeholder
          params.unicodePlaceholder = parseInt(value, 10);
          break;
        case 'z': // z-index
          params.zIndex = parseInt(value, 10);
          break;
        case 'o': // compression
          params.compression = parseInt(value, 10);
          break;
        case 't': // transmission medium
          params.medium = value;
          break;
      }
    }

    // Validate that we have at least an action
    if (!params.action) {
      return null;
    }

    return params as GraphicsParams;
  }

  /**
   * Handle image transmission command.
   * Processes the 't' action to receive and store image data.
   * 
   * @param params - Parsed graphics parameters
   * @param payload - Base64-encoded image data
   * @returns Image ID (generated if not provided)
   */
  handleTransmission(params: GraphicsParams, payload: string): number {
    // Generate image ID if not provided
    const imageId = params.imageId ?? this.nextImageId++;
    
    // Return the image ID for further processing
    // The actual decoding and storage will be handled by ImageManager
    return imageId;
  }

  /**
   * Handle image display command.
   * Processes the 'd' action to create an image placement.
   * 
   * @param params - Parsed graphics parameters
   * @param cursorRow - Current cursor row position
   * @param cursorCol - Current cursor column position
   * @param imageData - Optional image data for dimension calculations
   * @param cellWidth - Width of a terminal cell in pixels (for pixel-to-cell conversion)
   * @param cellHeight - Height of a terminal cell in pixels (for pixel-to-cell conversion)
   * @param screenCols - Number of columns on screen (for boundary clipping)
   * @param screenRows - Number of rows on screen (for boundary clipping)
   * @returns Placement configuration
   */
  handleDisplay(
    params: GraphicsParams,
    cursorRow: number,
    cursorCol: number,
    imageData?: ImageData,
    cellWidth?: number,
    cellHeight?: number,
    screenCols?: number,
    screenRows?: number
  ): Partial<ImagePlacement> {
    // Generate placement ID if not provided
    const placementId = params.placementId ?? this.nextPlacementId++;
    
    // Use cursor position if not specified (grid coordinate positioning)
    const row = params.y !== undefined ? params.y : cursorRow;
    const col = params.x !== undefined ? params.x : cursorCol;
    
    // Calculate dimensions
    let width = 0;
    let height = 0;
    
    if (params.cols !== undefined && params.cols > 0) {
      // Explicit cell dimensions provided
      width = params.cols;
    } else if (params.width !== undefined && params.width > 0 && cellWidth && cellWidth > 0) {
      // Pixel width provided - convert to cells
      width = this.pixelsToCells(params.width, cellWidth);
    } else if (imageData) {
      // Native dimension fallback - use image's native dimensions
      width = cellWidth && cellWidth > 0 
        ? this.pixelsToCells(imageData.width, cellWidth)
        : Math.ceil(imageData.width / 10); // Fallback: assume 10px per cell
    }
    
    if (params.rows !== undefined && params.rows > 0) {
      // Explicit cell dimensions provided
      height = params.rows;
    } else if (params.height !== undefined && params.height > 0 && cellHeight && cellHeight > 0) {
      // Pixel height provided - convert to cells
      height = this.pixelsToCells(params.height, cellHeight);
    } else if (imageData) {
      // Native dimension fallback - use image's native dimensions
      height = cellHeight && cellHeight > 0
        ? this.pixelsToCells(imageData.height, cellHeight)
        : Math.ceil(imageData.height / 20); // Fallback: assume 20px per cell
    }
    
    // Apply screen boundary clipping
    if (screenCols !== undefined && screenRows !== undefined) {
      // Ensure placement doesn't extend beyond screen boundaries
      const maxWidth = screenCols - col;
      const maxHeight = screenRows - row;
      
      if (width > maxWidth) {
        width = Math.max(0, maxWidth);
      }
      if (height > maxHeight) {
        height = Math.max(0, maxHeight);
      }
    }
    
    // Build placement configuration
    const placement: Partial<ImagePlacement> = {
      placementId,
      imageId: params.imageId,
      row,
      col,
      width,
      height,
    };

    // Add optional parameters (source rectangle cropping)
    if (params.sourceX !== undefined) placement.sourceX = params.sourceX;
    if (params.sourceY !== undefined) placement.sourceY = params.sourceY;
    if (params.sourceWidth !== undefined) placement.sourceWidth = params.sourceWidth;
    if (params.sourceHeight !== undefined) placement.sourceHeight = params.sourceHeight;
    if (params.zIndex !== undefined) placement.zIndex = params.zIndex;
    if (params.unicodePlaceholder !== undefined) {
      placement.unicodePlaceholder = String.fromCodePoint(params.unicodePlaceholder);
    }

    return placement;
  }

  /**
   * Convert pixel dimensions to cell dimensions.
   * Rounds up to ensure the entire image is visible.
   * 
   * @param pixels - Dimension in pixels
   * @param cellSize - Size of one cell in pixels
   * @returns Dimension in cells
   */
  private pixelsToCells(pixels: number, cellSize: number): number {
    if (cellSize <= 0) return 0;
    return Math.ceil(pixels / cellSize);
  }

  /**
   * Handle image deletion command.
   * Processes the 'D' action to remove images or placements.
   * 
   * @param params - Parsed graphics parameters
   * @returns Deletion specification
   */
  handleDelete(params: GraphicsParams): {
    type: 'image' | 'placement' | 'all';
    id?: number;
  } {
    // Delete specific image (all its placements)
    if (params.imageId !== undefined) {
      return { type: 'image', id: params.imageId };
    }
    
    // Delete specific placement
    if (params.placementId !== undefined) {
      return { type: 'placement', id: params.placementId };
    }
    
    // Delete all visible placements
    return { type: 'all' };
  }

  /**
   * Decode base64-encoded image data to ImageBitmap.
   * Supports PNG, JPEG, and GIF formats.
   * 
   * @param payload - Base64-encoded image data
   * @param format - Image format identifier (100=PNG, 24=RGB, 32=RGBA, etc.)
   * @returns Promise resolving to decoded ImageBitmap and format
   * @throws Error if decoding fails or format is unsupported
   */
  async decodeImageData(
    payload: string,
    format?: string
  ): Promise<{ bitmap: ImageBitmap; format: 'png' | 'jpeg' | 'gif'; width: number; height: number }> {
    try {
      // Validate payload is not empty
      if (!payload || payload.length === 0) {
        throw new Error('Empty image payload');
      }

      // Decode base64 to binary data
      const binaryString = atob(payload);
      const bytes = new Uint8Array(binaryString.length);
      for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
      }

      // Validate we have some data
      if (bytes.length === 0) {
        throw new Error('Empty image data after decoding');
      }

      // Detect format from magic bytes
      let detectedFormat: 'png' | 'jpeg' | 'gif' | null = null;
      
      if (this.isPNG(bytes)) {
        detectedFormat = 'png';
      } else if (this.isJPEG(bytes)) {
        detectedFormat = 'jpeg';
      } else if (this.isGIF(bytes)) {
        detectedFormat = 'gif';
      }

      // If format was specified but doesn't match detected format, throw error
      if (format && format !== '100' && format !== '24' && format !== '32') {
        // Unknown format code
        throw new Error(`Unsupported image format: ${format}`);
      }

      // If we couldn't detect format and no valid format was specified, error
      if (!detectedFormat) {
        throw new Error('Unable to detect image format from data');
      }

      // Create a Blob from the binary data
      const mimeType = this.getMimeType(detectedFormat);
      const blob = new Blob([bytes], { type: mimeType });

      // Decode to ImageBitmap for efficient rendering
      const bitmap = await createImageBitmap(blob);

      return {
        bitmap,
        format: detectedFormat,
        width: bitmap.width,
        height: bitmap.height
      };
    } catch (error) {
      // Provide more context in error message
      const errorMessage = error instanceof Error ? error.message : String(error);
      throw new Error(`Failed to decode image data: ${errorMessage}`);
    }
  }

  /**
   * Check if binary data is PNG format.
   * PNG files start with: 89 50 4E 47 0D 0A 1A 0A
   */
  private isPNG(bytes: Uint8Array): boolean {
    if (bytes.length < 8) return false;
    return (
      bytes[0] === 0x89 &&
      bytes[1] === 0x50 &&
      bytes[2] === 0x4e &&
      bytes[3] === 0x47 &&
      bytes[4] === 0x0d &&
      bytes[5] === 0x0a &&
      bytes[6] === 0x1a &&
      bytes[7] === 0x0a
    );
  }

  /**
   * Check if binary data is JPEG format.
   * JPEG files start with: FF D8 FF
   */
  private isJPEG(bytes: Uint8Array): boolean {
    if (bytes.length < 3) return false;
    return bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff;
  }

  /**
   * Check if binary data is GIF format.
   * GIF files start with: "GIF87a" or "GIF89a"
   */
  private isGIF(bytes: Uint8Array): boolean {
    if (bytes.length < 6) return false;
    const header = String.fromCharCode(...bytes.slice(0, 6));
    return header === 'GIF87a' || header === 'GIF89a';
  }

  /**
   * Get MIME type for image format.
   */
  private getMimeType(format: 'png' | 'jpeg' | 'gif'): string {
    switch (format) {
      case 'png':
        return 'image/png';
      case 'jpeg':
        return 'image/jpeg';
      case 'gif':
        return 'image/gif';
    }
  }

  /**
   * Reset ID generators (useful for testing).
   */
  reset(): void {
    this.nextImageId = 1;
    this.nextPlacementId = 1;
  }
}
