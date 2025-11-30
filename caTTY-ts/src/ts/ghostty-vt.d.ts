/**
 * Type definitions for the raw `ghostty-vt.wasm` bundle exports.
 *
 * These are LOW-LEVEL bindings directly mirroring the WebAssembly export list
 * discovered in `WASM_NOTES.md`. All functions operate on numeric pointers
 * (linear memory offsets) and primitive integers. Higher-level, safe wrappers
 * should be built on top of these (e.g. in `keyEncode.ts`, `sgr.ts`, `osc.ts`).
 *
 * Because we don't have the original C headers here, argument arity and
 * semantics are expressed generically. Wrapper layers can progressively
 * specialize signatures once patterns are confirmed from working demos.
 */

// Pointer alias for clarity. All pointers are 32-bit offsets into wasm memory.
export type Ptr<T = unknown> = number;

// Generic raw wasm function signature: accepts zero or more numeric arguments, returns a numeric value.
export type GhosttyRawFunc = (...args: number[]) => number;

// Some exported functions are known (from naming) to behave like void-return.
// For now we still type them as returning number (wasm functions always return
// something or 0) to keep a uniform surface; wrapper code can coerce to void.

/** Enumerated key actions (inferred from demo). */
export enum GhosttyKeyAction {
	Release = 0,
	Press = 1,
	Repeat = 2,
}

/** Bitflags for key modifiers (names inferred; exact mask values TBD). */
export enum GhosttyKeyMod {
	Shift = 1 << 0,
	Alt = 1 << 1,
	Control = 1 << 2,
	Meta = 1 << 3,
}

/**
 * Raw wasm export surface.
 * Each function is currently typed as `GhosttyRawFunc`; higher-level wrappers
 * should introduce stricter signatures (e.g. `(encoder: Ptr, event: Ptr, out: Ptr, outLen: Ptr) => number`).
 */
export interface GhosttyVtExports {
	memory: WebAssembly.Memory;

	// Key Event
	ghostty_key_event_new: GhosttyRawFunc;
	ghostty_key_event_free: GhosttyRawFunc;
	ghostty_key_event_set_action: GhosttyRawFunc;
	ghostty_key_event_get_action: GhosttyRawFunc;
	ghostty_key_event_set_key: GhosttyRawFunc;
	ghostty_key_event_get_key: GhosttyRawFunc;
	ghostty_key_event_set_mods: GhosttyRawFunc;
	ghostty_key_event_get_mods: GhosttyRawFunc;
	ghostty_key_event_set_consumed_mods: GhosttyRawFunc;
	ghostty_key_event_get_consumed_mods: GhosttyRawFunc;
	ghostty_key_event_set_composing: GhosttyRawFunc;
	ghostty_key_event_get_composing: GhosttyRawFunc;
	ghostty_key_event_set_utf8: GhosttyRawFunc; // likely (eventPtr, strPtr, len)
	ghostty_key_event_get_utf8: GhosttyRawFunc; // likely (eventPtr, outBufPtr, outLenPtr)
	ghostty_key_event_set_unshifted_codepoint: GhosttyRawFunc;
	ghostty_key_event_get_unshifted_codepoint: GhosttyRawFunc;

	// Key Encoder
	ghostty_key_encoder_new: GhosttyRawFunc;
	ghostty_key_encoder_free: GhosttyRawFunc;
	ghostty_key_encoder_setopt: GhosttyRawFunc; // (encoderPtr, optId, value)
	ghostty_key_encoder_encode: GhosttyRawFunc; // (encoderPtr, eventPtr, outBufPtr, outLenPtr?)

	// OSC Parser
	ghostty_osc_new: GhosttyRawFunc;
	ghostty_osc_free: GhosttyRawFunc;
	ghostty_osc_next: GhosttyRawFunc;
	ghostty_osc_reset: GhosttyRawFunc;
	ghostty_osc_end: GhosttyRawFunc;
	ghostty_osc_command_type: GhosttyRawFunc;
	ghostty_osc_command_data: GhosttyRawFunc; // (oscPtr, outBufPtr, outLenPtr)

	// Safety helpers
	ghostty_paste_is_safe: GhosttyRawFunc; // (u8Ptr, len) => bool as number

	// Color (RGB) helper
	ghostty_color_rgb_get: GhosttyRawFunc; // (colorPtr, outRPtr, outGPtr, outBPtr)

	// SGR Parser
	ghostty_sgr_new: GhosttyRawFunc;
	ghostty_sgr_free: GhosttyRawFunc;
	ghostty_sgr_reset: GhosttyRawFunc;
	ghostty_sgr_set_params: GhosttyRawFunc; // (sgrPtr, paramsPtr, len)
	ghostty_sgr_next: GhosttyRawFunc; // iterate attributes
	ghostty_sgr_unknown_full: GhosttyRawFunc; // maybe error detail
	ghostty_sgr_unknown_partial: GhosttyRawFunc;
	ghostty_sgr_attribute_tag: GhosttyRawFunc; // (sgrPtr, attrPtr?) returns tag id
	ghostty_sgr_attribute_value: GhosttyRawFunc; // (sgrPtr, attrPtr?) returns value

	// WASM allocation helpers
	ghostty_wasm_alloc_opaque: GhosttyRawFunc; // (size?) returns Ptr
	ghostty_wasm_free_opaque: GhosttyRawFunc; // (ptr)
	ghostty_wasm_alloc_u8_array: GhosttyRawFunc; // (length) => ptr
	ghostty_wasm_free_u8_array: GhosttyRawFunc; // (ptr, length?)
	ghostty_wasm_alloc_u16_array: GhosttyRawFunc;
	ghostty_wasm_free_u16_array: GhosttyRawFunc;
	ghostty_wasm_alloc_u8: GhosttyRawFunc; // single byte cell
	ghostty_wasm_free_u8: GhosttyRawFunc;
	ghostty_wasm_alloc_usize: GhosttyRawFunc; // pointer-sized cell
	ghostty_wasm_free_usize: GhosttyRawFunc;
	ghostty_wasm_alloc_sgr_attribute: GhosttyRawFunc;
	ghostty_wasm_free_sgr_attribute: GhosttyRawFunc;
}

/** Wrapper describing an instantiated module instance. */
export interface GhosttyVtInstance {
	exports: GhosttyVtExports;
}

/** Options for creating the instance via a helper loader. */
export interface GhosttyVtInstantiateOptions {
	/** Optional custom `env.log` implementation for wasm-side logging. */
	log?: (text: string) => void;
}

/** Loader return type. */
export interface GhosttyVtLoaded {
	instance: GhosttyVtInstance;
	memory: WebAssembly.Memory;
	exports: GhosttyVtExports;
}

/**
 * Suggested async loader helper signature (implementation lives in runtime TS).
 * Accepts either a URL string, `Response`, `ArrayBuffer`, or `Uint8Array`.
 */
export function loadGhosttyVt(
	source: string | URL | Response | ArrayBuffer | Uint8Array,
	opts?: GhosttyVtInstantiateOptions
): Promise<GhosttyVtLoaded>;

/** Narrow utility: convert UTF-8 JS string to a wasm heap block (implementation external). */
export function allocUtf8(exports: GhosttyVtExports, text: string): { ptr: Ptr; len: number; free(): void };

/** Allocate a u8 array and copy the given bytes (implementation external). */
export function allocU8Array(exports: GhosttyVtExports, bytes: ArrayLike<number>): { ptr: Ptr; len: number; free(): void };

/** Read back a u8 array from wasm memory. */
export function readU8Array(memory: WebAssembly.Memory, ptr: Ptr, len: number): Uint8Array;

/** Convenience to extract encoded output (ESC sequence) into string form. */
export function decodeEscaped(bytes: ArrayLike<number>): string;

/** High-level key encoding result shape (produced by wrapper layer, not raw wasm). */
export interface KeyEncodeResult {
	hex: string; // space-separated hex
	raw: Uint8Array; // raw bytes
	escaped: string; // string with \x1b tokens
}

