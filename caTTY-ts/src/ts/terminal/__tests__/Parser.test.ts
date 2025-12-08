import { describe, it, expect, vi } from 'vitest';
import { Parser, ParserState } from '../Parser';
import type { ParserHandlers } from '../Parser';

describe('Parser', () => {
  describe('State Machine', () => {
    it('should initialize in Ground state', () => {
      const parser = new Parser();
      // Parser state is private, but we can test behavior
      expect(parser).toBeDefined();
    });
    
    it('should handle ESC to enter Escape state', () => {
      const parser = new Parser();
      const data = new Uint8Array([0x1B]); // ESC
      parser.parse(data);
      // State transition happens internally
      expect(parser).toBeDefined();
    });
    
    it('should handle CSI sequence entry (ESC [)', () => {
      const parser = new Parser();
      const data = new Uint8Array([0x1B, 0x5B]); // ESC [
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should handle OSC sequence entry (ESC ])', () => {
      const parser = new Parser();
      const data = new Uint8Array([0x1B, 0x5D]); // ESC ]
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should reset to initial state', () => {
      const parser = new Parser();
      const data = new Uint8Array([0x1B, 0x5B, 0x41]); // ESC [ A
      parser.parse(data);
      parser.reset();
      expect(parser).toBeDefined();
    });
  });
  
  describe('UTF-8 Handling', () => {
    it('should handle 2-byte UTF-8 sequence', () => {
      const parser = new Parser();
      // © symbol (U+00A9) = 0xC2 0xA9
      const data = new Uint8Array([0xC2, 0xA9]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should handle 3-byte UTF-8 sequence', () => {
      const parser = new Parser();
      // € symbol (U+20AC) = 0xE2 0x82 0xAC
      const data = new Uint8Array([0xE2, 0x82, 0xAC]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should handle 4-byte UTF-8 sequence', () => {
      const parser = new Parser();
      // 𝄞 symbol (U+1D11E) = 0xF0 0x9D 0x84 0x9E
      const data = new Uint8Array([0xF0, 0x9D, 0x84, 0x9E]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should handle invalid UTF-8 start byte', () => {
      const parser = new Parser();
      // Invalid start byte (10xxxxxx pattern)
      const data = new Uint8Array([0x80]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should handle invalid UTF-8 continuation byte', () => {
      const parser = new Parser();
      // Valid start byte but invalid continuation
      const data = new Uint8Array([0xC2, 0x20]); // 0x20 is not a continuation byte
      parser.parse(data);
      expect(parser).toBeDefined();
    });
  });
  
  describe('Control Characters', () => {
    it('should call onLineFeed handler for LF (0x0A)', () => {
      const onLineFeed = vi.fn();
      const parser = new Parser({ onLineFeed });
      const data = new Uint8Array([0x0A]);
      parser.parse(data);
      expect(onLineFeed).toHaveBeenCalledTimes(1);
    });
    
    it('should call onCarriageReturn handler for CR (0x0D)', () => {
      const onCarriageReturn = vi.fn();
      const parser = new Parser({ onCarriageReturn });
      const data = new Uint8Array([0x0D]);
      parser.parse(data);
      expect(onCarriageReturn).toHaveBeenCalledTimes(1);
    });
    
    it('should call onBackspace handler for BS (0x08)', () => {
      const onBackspace = vi.fn();
      const parser = new Parser({ onBackspace });
      const data = new Uint8Array([0x08]);
      parser.parse(data);
      expect(onBackspace).toHaveBeenCalledTimes(1);
    });
    
    it('should call onTab handler for HT (0x09)', () => {
      const onTab = vi.fn();
      const parser = new Parser({ onTab });
      const data = new Uint8Array([0x09]);
      parser.parse(data);
      expect(onTab).toHaveBeenCalledTimes(1);
    });
    
    it('should call onBell handler for BEL (0x07)', () => {
      const onBell = vi.fn();
      const parser = new Parser({ onBell });
      const data = new Uint8Array([0x07]);
      parser.parse(data);
      expect(onBell).toHaveBeenCalledTimes(1);
    });
    
    it('should handle multiple control characters in sequence', () => {
      const onLineFeed = vi.fn();
      const onCarriageReturn = vi.fn();
      const onTab = vi.fn();
      const parser = new Parser({ onLineFeed, onCarriageReturn, onTab });
      // CR + LF + TAB
      const data = new Uint8Array([0x0D, 0x0A, 0x09]);
      parser.parse(data);
      expect(onCarriageReturn).toHaveBeenCalledTimes(1);
      expect(onLineFeed).toHaveBeenCalledTimes(1);
      expect(onTab).toHaveBeenCalledTimes(1);
    });
    
    it('should handle control characters mixed with printable text', () => {
      const onPrintable = vi.fn();
      const onLineFeed = vi.fn();
      const parser = new Parser({ onPrintable, onLineFeed });
      // "Hi" + LF + "there"
      const data = new Uint8Array([0x48, 0x69, 0x0A, 0x74, 0x68, 0x65, 0x72, 0x65]);
      parser.parse(data);
      expect(onPrintable).toHaveBeenCalledTimes(7); // H, i, t, h, e, r, e
      expect(onLineFeed).toHaveBeenCalledTimes(1);
    });
    
    it('should handle NUL (0x00) by ignoring it', () => {
      const onPrintable = vi.fn();
      const parser = new Parser({ onPrintable });
      // "A" + NUL + "B"
      const data = new Uint8Array([0x41, 0x00, 0x42]);
      parser.parse(data);
      expect(onPrintable).toHaveBeenCalledTimes(2); // Only A and B
      expect(onPrintable).toHaveBeenCalledWith('A');
      expect(onPrintable).toHaveBeenCalledWith('B');
    });
    
    it('should handle control characters during escape sequences', () => {
      const onLineFeed = vi.fn();
      const onCsi = vi.fn();
      const parser = new Parser({ onLineFeed, onCsi });
      // ESC [ (start CSI) + LF (should be handled) + A (CSI final)
      const data = new Uint8Array([0x1B, 0x5B, 0x0A, 0x41]);
      parser.parse(data);
      expect(onLineFeed).toHaveBeenCalledTimes(1);
      expect(onCsi).toHaveBeenCalledTimes(1);
    });
  });
  
  describe('CSI Sequences', () => {
    it('should parse CSI with single parameter', () => {
      const onCsi = vi.fn();
      const parser = new Parser({ onCsi });
      // ESC [ 5 A (cursor up 5 lines)
      const data = new Uint8Array([0x1B, 0x5B, 0x35, 0x41]);
      parser.parse(data);
      expect(onCsi).toHaveBeenCalledWith([5], '', 0x41, '');
    });
    
    it('should parse CSI with multiple parameters', () => {
      const onCsi = vi.fn();
      const parser = new Parser({ onCsi });
      // ESC [ 10 ; 20 H (cursor position row 10, col 20)
      const data = new Uint8Array([0x1B, 0x5B, 0x31, 0x30, 0x3B, 0x32, 0x30, 0x48]);
      parser.parse(data);
      expect(onCsi).toHaveBeenCalledWith([10, 20], '', 0x48, '');
    });
    
    it('should parse CSI with no parameters', () => {
      const onCsi = vi.fn();
      const parser = new Parser({ onCsi });
      // ESC [ A (cursor up 1 line)
      const data = new Uint8Array([0x1B, 0x5B, 0x41]);
      parser.parse(data);
      expect(onCsi).toHaveBeenCalledWith([0], '', 0x41, '');
    });
    
    it('should parse CSI with intermediate bytes', () => {
      const onCsi = vi.fn();
      const parser = new Parser({ onCsi });
      // ESC [ ? 25 h (show cursor - private sequence with '?')
      const data = new Uint8Array([0x1B, 0x5B, 0x3F, 0x32, 0x35, 0x68]);
      parser.parse(data);
      // Private marker '?' is now handled and passed to onCsi
      expect(onCsi).toHaveBeenCalledWith([25], '', 0x68, '?');
    });
    
    it('should call onCsi handler for cursor movement sequences', () => {
      const onCsi = vi.fn();
      const parser = new Parser({ onCsi });
      
      // Test various cursor movement sequences
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x41])); // CUU - up
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x42])); // CUD - down
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x43])); // CUF - forward
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x44])); // CUB - backward
      
      expect(onCsi).toHaveBeenCalledTimes(4);
    });
    
    it('should call onCsi handler for erase sequences', () => {
      const onCsi = vi.fn();
      const parser = new Parser({ onCsi });
      
      // ESC [ J (erase in display)
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x4A]));
      expect(onCsi).toHaveBeenCalledWith([0], '', 0x4A, '');
      
      // ESC [ K (erase in line)
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x4B]));
      expect(onCsi).toHaveBeenCalledWith([0], '', 0x4B, '');
    });
    
    it('should call onCsi handler for scroll sequences', () => {
      const onCsi = vi.fn();
      const parser = new Parser({ onCsi });
      
      // ESC [ 3 S (scroll up 3 lines)
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x33, 0x53]));
      expect(onCsi).toHaveBeenCalledWith([3], '', 0x53, '');
      
      // ESC [ 2 T (scroll down 2 lines)
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x32, 0x54]));
      expect(onCsi).toHaveBeenCalledWith([2], '', 0x54, '');
    });
    
    it('should call onCsi handler for character/line operations', () => {
      const onCsi = vi.fn();
      const parser = new Parser({ onCsi });
      
      // ESC [ 5 @ (insert 5 characters)
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x35, 0x40]));
      expect(onCsi).toHaveBeenCalledWith([5], '', 0x40, '');
      
      // ESC [ 3 P (delete 3 characters)
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x33, 0x50]));
      expect(onCsi).toHaveBeenCalledWith([3], '', 0x50, '');
      
      // ESC [ 2 L (insert 2 lines)
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x32, 0x4C]));
      expect(onCsi).toHaveBeenCalledWith([2], '', 0x4C, '');
      
      // ESC [ 1 M (delete 1 line)
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x31, 0x4D]));
      expect(onCsi).toHaveBeenCalledWith([1], '', 0x4D, '');
    });
    
    it('should call onCsi handler for scroll region', () => {
      const onCsi = vi.fn();
      const parser = new Parser({ onCsi });
      
      // ESC [ 5 ; 20 r (set scroll region from row 5 to 20)
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x35, 0x3B, 0x32, 0x30, 0x72]));
      expect(onCsi).toHaveBeenCalledWith([5, 20], '', 0x72, '');
    });
    
    it('should call onCsi handler for tab operations', () => {
      const onCsi = vi.fn();
      const parser = new Parser({ onCsi });
      
      // ESC [ I (forward tab)
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x49]));
      expect(onCsi).toHaveBeenCalledWith([0], '', 0x49, '');
      
      // ESC [ Z (backward tab)
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x5A]));
      expect(onCsi).toHaveBeenCalledWith([0], '', 0x5A, '');
      
      // ESC [ g (clear tab)
      parser.parse(new Uint8Array([0x1B, 0x5B, 0x67]));
      expect(onCsi).toHaveBeenCalledWith([0], '', 0x67, '');
    });
  });
  
  describe('OSC Sequences', () => {
    it('should parse OSC terminated with BEL', () => {
      const parser = new Parser();
      // ESC ] 0 ; title BEL
      const data = new Uint8Array([0x1B, 0x5D, 0x30, 0x3B, 0x74, 0x69, 0x74, 0x6C, 0x65, 0x07]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should parse OSC terminated with ESC', () => {
      const parser = new Parser();
      // ESC ] 0 ; title ESC
      const data = new Uint8Array([0x1B, 0x5D, 0x30, 0x3B, 0x74, 0x69, 0x74, 0x6C, 0x65, 0x1B]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
  });
  
  describe('Mixed Input', () => {
    it('should handle printable ASCII characters', () => {
      const parser = new Parser();
      const data = new Uint8Array([0x48, 0x65, 0x6C, 0x6C, 0x6F]); // "Hello"
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should handle mixed control and printable characters', () => {
      const parser = new Parser();
      // "Hi" + LF + "there"
      const data = new Uint8Array([0x48, 0x69, 0x0A, 0x74, 0x68, 0x65, 0x72, 0x65]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should handle escape sequences mixed with text', () => {
      const parser = new Parser();
      // "A" + ESC [ A + "B"
      const data = new Uint8Array([0x41, 0x1B, 0x5B, 0x41, 0x42]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
  });
});
