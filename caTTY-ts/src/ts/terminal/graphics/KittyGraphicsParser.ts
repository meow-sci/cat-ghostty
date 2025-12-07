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
   * @returns Placement configuration
   */
  handleDisplay(
    params: GraphicsParams,
    cursorRow: number,
    cursorCol: number
  ): Partial<ImagePlacement> {
    // Generate placement ID if not provided
    const placementId = params.placementId ?? this.nextPlacementId++;
    
    // Use cursor position if not specified
    const row = params.y !== undefined ? params.y : cursorRow;
    const col = params.x !== undefined ? params.x : cursorCol;
    
    // Build placement configuration
    const placement: Partial<ImagePlacement> = {
      placementId,
      imageId: params.imageId,
      row,
      col,
      width: params.cols ?? 0, // Will be calculated from image if 0
      height: params.rows ?? 0, // Will be calculated from image if 0
    };

    // Add optional parameters
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
   * Reset ID generators (useful for testing).
   */
  reset(): void {
    this.nextImageId = 1;
    this.nextPlacementId = 1;
  }
}
