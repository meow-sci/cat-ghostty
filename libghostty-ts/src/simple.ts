import fs from 'fs';

async function main() {
	// Load the wasm file
	const wasmPath = new URL('../lib/ghostty-vt.wasm', import.meta.url);
	const buf = await fs.promises.readFile(wasmPath);
	const mod = await WebAssembly.instantiate(buf, {
		env: {
			log: (n: number) => {
				// wasm expects an env.log import; just echo the numeric value
				console.log('[WASM log]', n);
			}
		}
	});

	const exports: any = (mod as WebAssembly.WebAssemblyInstantiatedSource).instance.exports;

	const memory = exports.memory as WebAssembly.Memory;
	let memU32 = new Uint32Array(memory.buffer);

	// Helper: read a 32-bit (usize) from memory
	function readU32(ptr: number) {
		return memU32[ptr >>> 2] ?? 0;
	}

	// Helper: read a null-terminated UTF-8 string
	function readCString(ptr: number) {
		const dv = new DataView(memory.buffer);
		let len = 0;
		while (dv.getUint8(ptr + len) !== 0) len++;
		const bytes = new Uint8Array(memory.buffer, ptr, len);
		return new TextDecoder('utf-8').decode(bytes);
	}

	// allocate a slot for a returned opaque pointer (pointer-to-pointer)
	const allocOpaque = exports.ghostty_wasm_alloc_opaque as () => number;
	const freeOpaque = exports.ghostty_wasm_free_opaque as (p: number) => void;

	// Create OSC parser
	const parserPtrSlot = allocOpaque();
	// In case allocation grew memory, refresh typed views.
	memU32 = new Uint32Array(memory.buffer);
	const res = exports.ghostty_osc_new(0 /* allocator ptr (NULL) */, parserPtrSlot) as number;
	console.log('ghostty_osc_new result:', res, 'parserPtrSlot at', parserPtrSlot);
	console.log('ghostty_osc_new JS function length (params count):', (exports.ghostty_osc_new as Function).length);
	if (res !== 0) {
		console.error('Failed to create OSC parser:', res);
		freeOpaque(parserPtrSlot);
		return;
	}
	const parserPtr = readU32(parserPtrSlot);
	console.log('parser pointer:', parserPtr);
	// Some wasm bindings return the created pointer instead of using out parameters.
	try {
		const maybeParser = (exports.ghostty_osc_new as any)(0);
		if (maybeParser) console.log('ghostty_osc_new returned value:', maybeParser);
	} catch {
		// ignore
	}

	// Feed bytes: '0;hello world 🔥' (encode as UTF-8)
	const encoder = new TextEncoder();
	const titleStr = 'hello world 🔥';
	const titleBytes = encoder.encode(titleStr);

	exports.ghostty_osc_next(parserPtr, '0'.charCodeAt(0));
	exports.ghostty_osc_next(parserPtr, ';'.charCodeAt(0));
	for (let i = 0; i < titleBytes.length; i++) {
		exports.ghostty_osc_next(parserPtr, titleBytes[i]);
	}

	// Finalize
	const cmdPtr = exports.ghostty_osc_end(parserPtr, 0) as number;
	console.log('command pointer:', cmdPtr);

	const type = exports.ghostty_osc_command_type(cmdPtr) as number;
	console.log('Command type:', type);

	// Try to extract title string
	const dataSlot = allocOpaque();
	memU32 = new Uint32Array(memory.buffer);
	const found = exports.ghostty_osc_command_data(cmdPtr, 1 /* GHOSTTY_OSC_DATA_CHANGE_WINDOW_TITLE_STR */, dataSlot) as number;
	if (found) {
		const titlePtr = readU32(dataSlot);
		if (titlePtr) {
			const extracted = readCString(titlePtr);
			console.log('Extracted title:', extracted);
		} else {
			console.log('Command data pointer was NULL');
		}
	} else {
		console.log('Failed to extract title');
	}

	// Cleanup
	exports.ghostty_osc_free(parserPtr);
	freeOpaque(parserPtrSlot);
	freeOpaque(dataSlot);
}

main().catch((err) => {
	console.error('Error running WASM program:', err);
});

