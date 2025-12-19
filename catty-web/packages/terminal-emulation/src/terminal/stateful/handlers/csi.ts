import type { CsiMessage } from "../../TerminalEmulationTypes";

import type { TerminalActions } from "../actions";

import {
  generateCursorPositionReport,
  generateDeviceAttributesPrimaryResponse,
  generateDeviceAttributesSecondaryResponse,
  generateDeviceStatusReportResponse,
  generateTerminalSizeResponse,
} from "../responses";

export function handleCsi(actions: TerminalActions, msg: CsiMessage): void {
  switch (msg._type) {
    case "csi.cursorUp":
      actions.setCursorY(actions.getCursorY() - Math.max(1, msg.count));
      actions.clampCursor();
      return;

    case "csi.cursorDown":
      actions.setCursorY(actions.getCursorY() + Math.max(1, msg.count));
      actions.clampCursor();
      return;

    case "csi.cursorForward":
      actions.setCursorX(actions.getCursorX() + Math.max(1, msg.count));
      actions.clampCursor();
      return;

    case "csi.cursorBackward":
      actions.setCursorX(actions.getCursorX() - Math.max(1, msg.count));
      actions.clampCursor();
      return;

    case "csi.cursorNextLine":
      actions.setCursorY(actions.getCursorY() + Math.max(1, msg.count));
      actions.setCursorX(0);
      actions.clampCursor();
      return;

    case "csi.cursorPrevLine":
      actions.setCursorY(actions.getCursorY() - Math.max(1, msg.count));
      actions.setCursorX(0);
      actions.clampCursor();
      return;

    case "csi.cursorHorizontalAbsolute":
      actions.setCursorX(Math.max(0, Math.min(actions.getCols() - 1, msg.column - 1)));
      actions.setWrapPending(false);
      return;

    case "csi.verticalPositionAbsolute":
      actions.setCursorY(actions.mapRowParamToCursorY(msg.row));
      actions.clampCursor();
      return;

    case "csi.cursorPosition":
      actions.setCursorY(actions.mapRowParamToCursorY(msg.row));
      actions.setCursorX(Math.max(0, Math.min(actions.getCols() - 1, msg.column - 1)));
      actions.clampCursor();
      return;

    case "csi.eraseInLine":
      actions.clearLine(msg.mode);
      return;

    case "csi.selectiveEraseInLine":
      actions.clearLineSelective(msg.mode);
      return;

    case "csi.cursorForwardTab":
      actions.cursorForwardTab(msg.count);
      return;

    case "csi.cursorBackwardTab":
      actions.cursorBackwardTab(msg.count);
      return;

    case "csi.tabClear":
      if (msg.mode === 3) {
        actions.clearAllTabStops();
        return;
      }
      actions.clearTabStopAtCursor();
      return;

    case "csi.eraseInDisplay":
      actions.clearDisplay(msg.mode);
      return;

    case "csi.selectiveEraseInDisplay":
      actions.clearDisplaySelective(msg.mode);
      return;

    case "csi.selectCharacterProtection":
      actions.setCharacterProtection(msg.protected);
      return;

    case "csi.insertChars":
      actions.insertCharsInLine(Math.max(1, msg.count));
      return;

    case "csi.deleteChars":
      actions.deleteCharsInLine(Math.max(1, msg.count));
      return;

    case "csi.deleteLines":
      actions.deleteLinesInRegion(Math.max(1, msg.count));
      return;

    case "csi.insertLines":
      actions.insertLinesInRegion(Math.max(1, msg.count));
      return;

    case "csi.scrollUp":
      actions.scrollUpInRegion(msg.lines);
      return;

    case "csi.scrollDown":
      actions.scrollDownInRegion(msg.lines);
      return;

    case "csi.saveCursorPosition":
      actions.saveCursorPosition();
      return;

    case "csi.restoreCursorPosition":
      actions.restoreCursorPosition();
      return;

    case "csi.decModeSet":
      // DECOM (CSI ? 6 h): origin mode
      if (msg.modes.includes(6)) {
        actions.setOriginMode(true);
      }
      // DECAWM (CSI ? 7 h): auto-wrap
      if (msg.modes.includes(7)) {
        actions.setAutoWrapMode(true);
      }
      // DECTCEM (CSI ? 25 h): show cursor
      if (msg.modes.includes(25)) {
        actions.setCursorVisibility(true);
      }
      // Application cursor keys (CSI ? 1 h)
      if (msg.modes.includes(1)) {
        actions.setApplicationCursorKeys(true);
      }
      // UTF-8 mode (CSI ? 2027 h)
      if (msg.modes.includes(2027)) {
        actions.setUtf8Mode(true);
      }
      // Alternate screen buffer modes
      if (msg.modes.includes(47)) {
        // DECSET 47: Switch to alternate screen buffer
        actions.switchToAlternateScreen();
      }
      if (msg.modes.includes(1047)) {
        // DECSET 1047: Save cursor and switch to alternate screen buffer
        actions.switchToAlternateScreenWithCursorSave();
      }
      if (msg.modes.includes(1049)) {
        // DECSET 1049: Save cursor, switch to alternate screen, and clear it
        actions.switchToAlternateScreenWithCursorSaveAndClear();
      }
      actions.emitDecMode({ action: "set", raw: msg.raw, modes: msg.modes });
      return;

    case "csi.decModeReset":
      // DECOM (CSI ? 6 l): origin mode
      if (msg.modes.includes(6)) {
        actions.setOriginMode(false);
      }
      // DECAWM (CSI ? 7 l): auto-wrap
      if (msg.modes.includes(7)) {
        actions.setAutoWrapMode(false);
      }
      // DECTCEM (CSI ? 25 l): hide cursor
      if (msg.modes.includes(25)) {
        actions.setCursorVisibility(false);
      }
      // Application cursor keys (CSI ? 1 l)
      if (msg.modes.includes(1)) {
        actions.setApplicationCursorKeys(false);
      }
      // UTF-8 mode (CSI ? 2027 l)
      if (msg.modes.includes(2027)) {
        actions.setUtf8Mode(false);
      }
      // Alternate screen buffer modes
      if (msg.modes.includes(47)) {
        // DECRST 47: Switch back to normal screen buffer
        actions.switchToPrimaryScreen();
      }
      if (msg.modes.includes(1047)) {
        // DECRST 1047: Switch to normal screen and restore cursor
        actions.switchToPrimaryScreenWithCursorRestore();
      }
      if (msg.modes.includes(1049)) {
        // DECRST 1049: Switch to normal screen and restore cursor
        actions.switchToPrimaryScreenWithCursorRestore();
      }
      actions.emitDecMode({ action: "reset", raw: msg.raw, modes: msg.modes });
      return;

    case "csi.decSoftReset":
      actions.softReset();
      return;

    case "csi.setCursorStyle":
      // DECSCUSR (CSI Ps SP q)
      actions.setCursorStyle(msg.style);
      return;

    // Device query handling
    case "csi.deviceAttributesPrimary":
      // Primary DA query: respond with device attributes
      actions.emitResponse(generateDeviceAttributesPrimaryResponse());
      return;

    case "csi.deviceAttributesSecondary":
      // Secondary DA query: respond with terminal version
      actions.emitResponse(generateDeviceAttributesSecondaryResponse());
      return;

    case "csi.cursorPositionReport":
      // CPR query: respond with current cursor position
      actions.emitResponse(generateCursorPositionReport(actions.getCursorX(), actions.getCursorY()));
      return;

    case "csi.deviceStatusReport":
      // DSR ready query: respond with CSI 0 n
      actions.emitResponse(generateDeviceStatusReportResponse());
      return;

    case "csi.terminalSizeQuery":
      // Terminal size query: respond with dimensions
      actions.emitResponse(generateTerminalSizeResponse(actions.getRows(), actions.getCols()));
      return;

    case "csi.characterSetQuery":
      // Character set query: respond with current character set
      actions.emitResponse(actions.generateCharacterSetQueryResponse());
      return;

    case "csi.eraseCharacter":
      actions.eraseCharacters(msg.count);
      return;

    case "csi.insertMode":
      // IRM (Insert/Replace Mode) - store the mode but don't implement insertion yet
      // This prevents the sequence from being unknown and potentially causing issues
      return;

    case "csi.windowManipulation":
      // Window manipulation commands - handle title stack operations for vi compatibility
      actions.handleWindowManipulation(msg.operation, msg.params);
      return;

    case "csi.setScrollRegion":
      actions.setScrollRegion(msg.top, msg.bottom);
      return;

    case "csi.enhancedSgrMode":
      // Enhanced SGR sequences with > prefix (e.g., CSI > 4 ; 2 m)
      actions.handleEnhancedSgrMode(msg.params);
      return;

    case "csi.privateSgrMode":
      // Private SGR sequences with ? prefix (e.g., CSI ? 4 m)
      actions.handlePrivateSgrMode(msg.params);
      return;

    case "csi.sgrWithIntermediate":
      // SGR sequences with intermediate characters (e.g., CSI 0 % m)
      actions.handleSgrWithIntermediate(msg.params, msg.intermediate);
      return;

    case "csi.unknownViSequence":
      // Unknown vi sequences (e.g., CSI 11M) - gracefully acknowledge but don't implement
      actions.handleUnknownViSequence(msg.sequenceNumber);
      return;

    case "csi.savePrivateMode":
      actions.savePrivateMode(msg.modes);
      return;

    case "csi.restorePrivateMode":
      actions.restorePrivateMode(msg.modes);
      return;

    // ignored (for MVP)
    case "csi.mouseReportingMode":
    case "csi.unknown":
      return;
  }
}
