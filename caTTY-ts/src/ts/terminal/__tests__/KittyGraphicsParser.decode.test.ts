/**
 * Tests for image decoding functionality in KittyGraphicsParser.
 * Verifies that base64-encoded images are correctly decoded to ImageBitmap.
 * 
 * @vitest-environment jsdom
 */

import { describe, it, expect, beforeAll } from 'vitest';
import { KittyGraphicsParser } from '../graphics/KittyGraphicsParser.js';

describe('KittyGraphicsParser Image Decoding', () => {
  // Mock createImageBitmap for testing (not available in jsdom)
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
        // Create a mock ImageBitmap instance
        return new (globalThis as any).ImageBitmap();
      };
    }
  });
  // 1x1 red PNG image (base64 encoded)
  const RED_PNG_BASE64 = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFBQIAX8jx0gAAAABJRU5ErkJggg==';
  
  // 1x1 red JPEG image (base64 encoded)
  const RED_JPEG_BASE64 = '/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCwAA8A/9k=';
  
  // 1x1 red GIF image (base64 encoded)
  const RED_GIF_BASE64 = 'R0lGODlhAQABAPAAAP8AAP///yH5BAAAAAAALAAAAAABAAEAAAICRAEAOw==';

  it('should decode PNG images correctly', async () => {
    const parser = new KittyGraphicsParser();
    
    const result = await parser.decodeImageData(RED_PNG_BASE64, '100');
    
    expect(result).toBeDefined();
    expect(result.format).toBe('png');
    expect(result.bitmap).toBeInstanceOf(ImageBitmap);
    expect(result.width).toBe(1);
    expect(result.height).toBe(1);
  });

  it('should decode JPEG images correctly', async () => {
    const parser = new KittyGraphicsParser();
    
    const result = await parser.decodeImageData(RED_JPEG_BASE64);
    
    expect(result).toBeDefined();
    expect(result.format).toBe('jpeg');
    expect(result.bitmap).toBeInstanceOf(ImageBitmap);
    expect(result.width).toBe(1);
    expect(result.height).toBe(1);
  });

  it('should decode GIF images correctly', async () => {
    const parser = new KittyGraphicsParser();
    
    const result = await parser.decodeImageData(RED_GIF_BASE64);
    
    expect(result).toBeDefined();
    expect(result.format).toBe('gif');
    expect(result.bitmap).toBeInstanceOf(ImageBitmap);
    expect(result.width).toBe(1);
    expect(result.height).toBe(1);
  });

  it('should detect PNG format from magic bytes', async () => {
    const parser = new KittyGraphicsParser();
    
    // Don't specify format, let it auto-detect
    const result = await parser.decodeImageData(RED_PNG_BASE64);
    
    expect(result.format).toBe('png');
  });

  it('should detect JPEG format from magic bytes', async () => {
    const parser = new KittyGraphicsParser();
    
    // Don't specify format, let it auto-detect
    const result = await parser.decodeImageData(RED_JPEG_BASE64);
    
    expect(result.format).toBe('jpeg');
  });

  it('should detect GIF format from magic bytes', async () => {
    const parser = new KittyGraphicsParser();
    
    // Don't specify format, let it auto-detect
    const result = await parser.decodeImageData(RED_GIF_BASE64);
    
    expect(result.format).toBe('gif');
  });

  it('should throw error for invalid base64', async () => {
    const parser = new KittyGraphicsParser();
    
    await expect(parser.decodeImageData('invalid-base64!!!')).rejects.toThrow();
  });

  it('should throw error for unsupported format', async () => {
    const parser = new KittyGraphicsParser();
    
    // Use a valid base64 string but claim it's an unsupported format
    await expect(
      parser.decodeImageData(RED_PNG_BASE64, '999')
    ).rejects.toThrow('Unsupported image format');
  });

  it('should handle empty payload gracefully', async () => {
    const parser = new KittyGraphicsParser();
    
    await expect(parser.decodeImageData('')).rejects.toThrow();
  });
});
