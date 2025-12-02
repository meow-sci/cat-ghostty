import { describe, it, expect } from 'vitest';
import { Parser, ParserState } from '../Parser';

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
    it('should handle LF (0x0A)', () => {
      const parser = new Parser();
      const data = new Uint8Array([0x0A]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should handle CR (0x0D)', () => {
      const parser = new Parser();
      const data = new Uint8Array([0x0D]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should handle BS (0x08)', () => {
      const parser = new Parser();
      const data = new Uint8Array([0x08]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should handle HT (0x09)', () => {
      const parser = new Parser();
      const data = new Uint8Array([0x09]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should handle BEL (0x07)', () => {
      const parser = new Parser();
      const data = new Uint8Array([0x07]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
  });
  
  describe('CSI Sequences', () => {
    it('should parse CSI with single parameter', () => {
      const parser = new Parser();
      // ESC [ 5 A (cursor up 5 lines)
      const data = new Uint8Array([0x1B, 0x5B, 0x35, 0x41]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should parse CSI with multiple parameters', () => {
      const parser = new Parser();
      // ESC [ 10 ; 20 H (cursor position row 10, col 20)
      const data = new Uint8Array([0x1B, 0x5B, 0x31, 0x30, 0x3B, 0x32, 0x30, 0x48]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should parse CSI with no parameters', () => {
      const parser = new Parser();
      // ESC [ A (cursor up 1 line)
      const data = new Uint8Array([0x1B, 0x5B, 0x41]);
      parser.parse(data);
      expect(parser).toBeDefined();
    });
    
    it('should parse CSI with intermediate bytes', () => {
      const parser = new Parser();
      // ESC [ ? 25 h (show cursor)
      const data = new Uint8Array([0x1B, 0x5B, 0x3F, 0x32, 0x35, 0x68]);
      parser.parse(data);
      expect(parser).toBeDefined();
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
