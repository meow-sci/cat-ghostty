import { describe, it, expect } from "vitest";
import { SgrStyleManager } from "../terminal/SgrStyleManager";
import { processSgrMessages } from "../terminal/SgrStateProcessor";
import { parseSgr, parseSgrParamsAndSeparators } from "../terminal/ParseSgr";

describe("Enhanced SGR CSS Generation", () => {
  
  describe("Enhanced underline mode CSS generation", () => {
    it("should generate correct CSS for enhanced double underline", () => {
      const styleManager = new SgrStyleManager();
      
      // Parse enhanced underline sequence
      const { params, separators, prefix } = parseSgrParamsAndSeparators("\x1b[>4;2m");
      const messages = parseSgr(params, separators, prefix);
      
      // Process messages to get SGR state
      const initialState = {
        bold: false,
        faint: false,
        italic: false,
        underline: false,
        underlineStyle: null,
        blink: false,
        inverse: false,
        hidden: false,
        strikethrough: false,
        foregroundColor: null,
        backgroundColor: null,
        underlineColor: null,
        font: 0,
      } as const;
      
      const sgrState = processSgrMessages(initialState, messages);
      
      // Generate CSS
      const css = styleManager.generateCssForSgr(sgrState);
      
      expect(css).toContain("text-decoration: underline");
      expect(css).toContain("text-decoration-style: double");
    });

    it("should generate correct CSS for enhanced curly underline", () => {
      const styleManager = new SgrStyleManager();
      
      // Parse enhanced underline sequence
      const { params, separators, prefix } = parseSgrParamsAndSeparators("\x1b[>4;3m");
      const messages = parseSgr(params, separators, prefix);
      
      // Process messages to get SGR state
      const initialState = {
        bold: false,
        faint: false,
        italic: false,
        underline: false,
        underlineStyle: null,
        blink: false,
        inverse: false,
        hidden: false,
        strikethrough: false,
        foregroundColor: null,
        backgroundColor: null,
        underlineColor: null,
        font: 0,
      } as const;
      
      const sgrState = processSgrMessages(initialState, messages);
      
      // Generate CSS
      const css = styleManager.generateCssForSgr(sgrState);
      
      expect(css).toContain("text-decoration: underline");
      expect(css).toContain("text-decoration-style: wavy");
    });

    it("should generate correct CSS for enhanced dotted underline", () => {
      const styleManager = new SgrStyleManager();
      
      // Parse enhanced underline sequence
      const { params, separators, prefix } = parseSgrParamsAndSeparators("\x1b[>4;4m");
      const messages = parseSgr(params, separators, prefix);
      
      // Process messages to get SGR state
      const initialState = {
        bold: false,
        faint: false,
        italic: false,
        underline: false,
        underlineStyle: null,
        blink: false,
        inverse: false,
        hidden: false,
        strikethrough: false,
        foregroundColor: null,
        backgroundColor: null,
        underlineColor: null,
        font: 0,
      } as const;
      
      const sgrState = processSgrMessages(initialState, messages);
      
      // Generate CSS
      const css = styleManager.generateCssForSgr(sgrState);
      
      expect(css).toContain("text-decoration: underline");
      expect(css).toContain("text-decoration-style: dotted");
    });

    it("should generate correct CSS for enhanced dashed underline", () => {
      const styleManager = new SgrStyleManager();
      
      // Parse enhanced underline sequence
      const { params, separators, prefix } = parseSgrParamsAndSeparators("\x1b[>4;5m");
      const messages = parseSgr(params, separators, prefix);
      
      // Process messages to get SGR state
      const initialState = {
        bold: false,
        faint: false,
        italic: false,
        underline: false,
        underlineStyle: null,
        blink: false,
        inverse: false,
        hidden: false,
        strikethrough: false,
        foregroundColor: null,
        backgroundColor: null,
        underlineColor: null,
        font: 0,
      } as const;
      
      const sgrState = processSgrMessages(initialState, messages);
      
      // Generate CSS
      const css = styleManager.generateCssForSgr(sgrState);
      
      expect(css).toContain("text-decoration: underline");
      expect(css).toContain("text-decoration-style: dashed");
    });

    it("should generate no underline CSS for enhanced underline off", () => {
      const styleManager = new SgrStyleManager();
      
      // Parse enhanced underline off sequence
      const { params, separators, prefix } = parseSgrParamsAndSeparators("\x1b[>4;0m");
      const messages = parseSgr(params, separators, prefix);
      
      // Start with underline enabled
      const initialState = {
        bold: false,
        faint: false,
        italic: false,
        underline: true,
        underlineStyle: "single" as const,
        blink: false,
        inverse: false,
        hidden: false,
        strikethrough: false,
        foregroundColor: null,
        backgroundColor: null,
        underlineColor: null,
        font: 0,
      };
      
      const sgrState = processSgrMessages(initialState, messages);
      
      // Generate CSS
      const css = styleManager.generateCssForSgr(sgrState);
      
      expect(css).not.toContain("text-decoration: underline");
      expect(css).not.toContain("text-decoration-style");
    });

    it("should generate consistent hash for same enhanced underline style", () => {
      const styleManager = new SgrStyleManager();
      
      // Parse enhanced underline sequence twice
      const { params, separators, prefix } = parseSgrParamsAndSeparators("\x1b[>4;2m");
      const messages = parseSgr(params, separators, prefix);
      
      const initialState = {
        bold: false,
        faint: false,
        italic: false,
        underline: false,
        underlineStyle: null,
        blink: false,
        inverse: false,
        hidden: false,
        strikethrough: false,
        foregroundColor: null,
        backgroundColor: null,
        underlineColor: null,
        font: 0,
      } as const;
      
      const sgrState1 = processSgrMessages(initialState, messages);
      const sgrState2 = processSgrMessages(initialState, messages);
      
      // Generate CSS classes
      const class1 = styleManager.getStyleClass(sgrState1);
      const class2 = styleManager.getStyleClass(sgrState2);
      
      expect(class1).toBe(class2);
    });
  });
});