# caTTY Terminal Emulator – Feature Gaps (Baseline)

This document lists **important terminal-emulation gaps** found by reviewing the current parser + state machine in `packages/terminal-emulation` and the web input bridge in `app/src/ts/terminal/TerminalController.ts`.

I’ve prioritized items that are common in “baseline” terminal emulators (ECMA-48/VT100-ish + widely used xterm extensions) and that tend to affect real-world apps (shell line editing, tmux/screen, vim/emacs, TUIs).

## Summary

- Rendering + core CSI cursor movement, screen clearing, scroll regions, alternate screen, SGR, and basic OSC title/color queries are in good shape.
- The biggest remaining risks are a few **DEC private mode save/restore** gaps (XTSAVE/XTRESTORE), plus missing semantics for some **DCS query** features (apps probing via DCS).

---

## High Priority (strong “baseline” expectations)

### 1) DCS (and other ECMA-48 “string” controls) are now consumed correctly (payload no longer leaks)

**Why it matters:**
- Modern terminals must at least *consume* these sequences correctly, even if they do not implement the feature, otherwise the **payload bytes can show up on screen** (or corrupt the parser state).
- xterm uses DCS for several common “query” features (e.g. DECRQSS, XTGETTCAP). Some apps probe capabilities this way.

**Current state:**
- Implemented (baseline safety): `DCS` (`ESC P ... ST`) plus `SOS` (`ESC X`), `PM` (`ESC ^`), and `APC` (`ESC _`) are parsed as control strings and **consumed until `ST`** so payload never renders as normal bytes.
- Implemented (feature semantics): `DECRQSS` (DCS $ q) is now handled and responds to queries for SGR and DECSTBM. Other DCS sequences are consumed/ignored.

**Key sequences:**
- `DCS ... ST` (7-bit: `ESC P ... ESC \\`)
- Also related string controls worth consuming correctly (even if ignored): `SOS` (`ESC X`), `PM` (`ESC ^`), `APC` (`ESC _`).

**Where it lives:**
- Parser changes: `packages/terminal-emulation/src/terminal/Parser.ts`
- DCS handler plumbing: `packages/terminal-emulation/src/terminal/stateful/handlers/dcs.ts`
- Trace support: `packages/terminal-emulation/src/terminal/TerminalTrace.ts`

---

### 2) Bracketed paste mode (DECSET 2004) is implemented end-to-end

**Why it matters:**
- Bracketed paste is a de-facto baseline feature for shells/editors. It prevents pasted text from being interpreted as “typed input” (e.g. avoids executing pasted newlines in shells without confirmation).

**Current state:**
- Implemented: `TerminalController` now tracks `CSI ? 2004 h/l` via `terminal.onDecMode(...)` and wraps paste as `ESC[200~...ESC[201~` when enabled.

**Expected behavior:**
- When bracketed paste is enabled, terminal should send:
  - `ESC [ 200 ~` + pasted text + `ESC [ 201 ~`

**Where it lives:**
- `app/src/ts/terminal/TerminalController.ts`

---

### 3) Insert/Delete Character editing ops (ICH/DCH) are implemented

**Why it matters:**
- Shell line editing (readline/zle/fish) and many TUIs rely on **character insertion/deletion within a line**.
- Without these, you can still “mostly work”, but you’ll see cursor artifacts, broken prompt redraws, or incorrect line updates.

**Key CSI sequences (common):**
- `CSI Ps @` — **ICH** Insert blank characters (shift right)
- `CSI Ps P` — **DCH** Delete characters (shift left)

**Current state:**
- Implemented: `CSI Ps @` (ICH) and `CSI Ps P` (DCH) are parsed and executed by shifting cells within the current line.

**Where it lives:**
- Parsing: `packages/terminal-emulation/src/terminal/ParseCsi.ts`
- Execution: `packages/terminal-emulation/src/terminal/StatefulTerminal.ts`

---

### 4) SI/SO (Shift In/Out) and single-shifts

**Why it matters:**
- VT100 character set switching is used for line-drawing (DEC Special Graphics) and some legacy apps.
- You already implemented *designation* (e.g. `ESC ( 0`), and you have character translation logic, but without *invocation* the feature is incomplete.

**Key controls:**
- `SO` (0x0E) — invoke G1 as GL
- `SI` (0x0F) — invoke G0 as GL
- (Optional but related) `SS2` (`ESC N`) and `SS3` (`ESC O`) affect next character only.

**Current state:**
- Implemented: `SO` (0x0E) and `SI` (0x0F) are handled as C0 controls and switch the invoked character set between `G1` and `G0` respectively.

**Where to implement:**
- Implemented in:
  - `packages/terminal-emulation/src/terminal/Parser.ts`
  - `packages/terminal-emulation/src/terminal/StatefulTerminal.ts`

---

### 5) Basic ESC single-character functions: IND/NEL/HTS

**Why it matters:**
- These are core VT100/xterm functions and are commonly used in older code and some terminfo capabilities.

**Key sequences:**
- `ESC D` — **IND** (Index): move down one line (like LF but scrolls within region)
- `ESC E` — **NEL** (Next Line): CR + LF
- `ESC H` — **HTS** (Tab Set): sets a tab stop at current column

**Current state:**
- Implemented: `ESC D` (IND), `ESC E` (NEL), and `ESC H` (HTS) are parsed and executed.

**Where to implement:**
- Implemented in:
  - `packages/terminal-emulation/src/terminal/Parser.ts`
  - `packages/terminal-emulation/src/terminal/StatefulTerminal.ts`

---

### 6) Reset controls: RIS and DECSTR

**Why it matters:**
- Apps (and test suites like vttest) use resets to restore a known state.
- Correct reset behavior reduces “sticky state” bugs (scroll region, origin mode, modes, character sets, SGR, etc.).

**Key sequences:**
- `ESC c` — **RIS** (Full Reset)
- `CSI ! p` — **DECSTR** (Soft Reset)

**Current state:**
- Implemented:
  - `ESC c` (RIS) triggers a best-effort hard reset (clears buffers and resets state).
  - `CSI ! p` (DECSTR) triggers a soft reset (resets state/modes without clearing the screen).

**Where to implement:**
- Implemented in:
  - `packages/terminal-emulation/src/terminal/Parser.ts` (RIS)
  - `packages/terminal-emulation/src/terminal/ParseCsi.ts` (DECSTR)
  - `packages/terminal-emulation/src/terminal/StatefulTerminal.ts` (hard + soft reset behaviors)

---

## Medium Priority (common, but some apps tolerate absence)

### 7) Mouse reporting modes are implemented (ParseCsi type mismatch remains)

**Why it matters:**
- Vim, less, and many TUIs can use the mouse.
- Even if you don’t want to fully support mouse yet, you should at least track enable/disable cleanly so you can add it without refactoring.

**Key xterm modes (DECSET/DECRST):**
- `CSI ? 1000 h/l` (VT200 mouse)
- `CSI ? 1002 h/l` (button-event tracking)
- `CSI ? 1003 h/l` (any-event tracking)
- `CSI ? 1006 h/l` (SGR mouse encoding)
- `CSI ? 1005 h/l` (UTF-8 extended coordinates; less important today)

**Current state:**
- Implemented (minimum click support): `TerminalController` now tracks `CSI ? 1000 h/l` and `CSI ? 1006 h/l` via `terminal.onDecMode(...)` and sends xterm mouse reports on `mousedown`/`mouseup`.
  - Uses SGR encoding (1006) when enabled: `ESC[<b;x;yM` press and `ESC[<b;x;ym` release.
  - Falls back to X10 encoding if 1006 is not enabled.
- Implemented: motion/drag reporting (1002/1003) and wheel events.
- `TerminalEmulationTypes.ts` still defines `CsiMouseReportingMode`, but `ParseCsi.ts` does not emit it; mouse modes are handled as DECSET/DECRST (recommended to keep that way unless/until you want richer typed events).

  - When mouse reporting is enabled, wheel events send xterm wheel button codes (64/65).
  - When mouse reporting is disabled, wheel scrolls the local scrollback viewport (no arrow/page key injection to the PTY).
**Where it lives:**
- Mouse event wiring + encoding: `app/src/ts/terminal/TerminalController.ts`
- Tests: `app/src/ts/terminal/__tests__/TerminalController.window.test.ts`

---

### 8) Tab stop management (TBC/CHT/CBT) is implemented

**Why it matters:**
- Many programs assume fixed 8-column tabs and never manipulate stops; but some TUIs do.

**Key sequences:**
- `CSI Ps I` — CHT (Forward Tabulation)
- `CSI Ps Z` — CBT (Backward Tabulation)
- `CSI Ps g` — TBC (Tab Clear)
- `ESC H` — HTS (Tab Set) (also listed above)

**Current state:**
- Implemented: tab stops are tracked (default every 8 cols), `ESC H` sets a tab stop, `TAB` advances to the next stop, and the remaining controls are supported:
  - `CSI Ps I` (CHT), `CSI Ps Z` (CBT), `CSI Ps g` (TBC 0/3)

---

### 9) Origin mode + autowrap mode (DECOM/DECAWM)

**Why it matters:**
- Some full-screen apps assume these modes behave correctly.

**Key sequences:**
- `CSI ? 6 h/l` — DECOM (Origin mode)
- `CSI ? 7 h/l` — DECAWM (Auto-wrap)

**Current state:**
- Implemented:
  - `CSI ? 6 h/l` (DECOM) now controls whether CUP/VPA address rows relative to the scroll region and clamps cursor Y within the region when enabled.
  - `CSI ? 7 h/l` (DECAWM) now gates auto-wrap: when disabled, printing at the right margin overwrites the last cell instead of wrapping on the next printable.

**Where it lives:**
- `packages/terminal-emulation/src/terminal/StatefulTerminal.ts`

---

### 10) XTSAVE/XTRESTORE (save/restore DEC private modes)

**Why it matters:**
- Used historically in termcap/vi scenarios to preserve private mode state.

**Key sequences:**
- `CSI ? Pm s` — XTSAVE
- `CSI ? Pm r` — XTRESTORE

---

### 11) DSR “status report” (CSI 5 n)

**Why it matters:**
- Some programs send DSR to verify terminal readiness.

**Key sequences:**
- `CSI 5 n` → respond `CSI 0 n`

**Current state:**
- Implemented: `CSI 5 n` now responds with `CSI 0 n`.

---

## Lower Priority / Optional (not baseline for your current goals)

- Graphics: SIXEL/ReGIS (needs full DCS parsing first).
- OSC color/palette mutation (OSC 4/12/etc.) and clipboard OSC 52.
- Rectangular editing variants (DECCARA/DECERA, etc.).

---

## “Parsed but not implemented” items already present

- `CSI 4 h/l` — Insert/Replace Mode (IRM): explicitly parsed with `implemented: false`, and `StatefulTerminal` currently does nothing besides acknowledging.
- `CSI ... t` window manipulation: implemented only for title/icon stack ops; other operations are intentionally ignored (reasonable for the web).

---

## Recommended next steps (minimal, high-value)

1) Implement **XTSAVE/XTRESTORE** (`CSI ? Pm s` / `CSI ? Pm r`) to preserve DEC private mode state in legacy code paths.
2) Implement common **DCS query responses** (e.g. DECRQSS, XTGETTCAP) so probing applications get expected replies.
