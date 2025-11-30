# Overview

A TypeScript terminal emulator.

Uses libghostty-vt wasm library for terminal key encoding, OSC (operating system command) and SGR (Select Graphic Rendition) attribute parsing.

The rest of the stateful and glue code for the terminal emulator will be web based and written in typescript.

All logic will be 100% TypeScript and designed in a headless way.

The web portion will be the "display" only, which will be two parts: an `<input>` to process keystrokes and a `<section id="display"></section>` using a monospace font and a know width/height based on characters as the terminal "screen".  The emulator will "paint" the headless state to this display by appending single character `<span>` HTML elements using CSS absolute positioning with a left/top offset using the `ch` unit.


# Project layout

* `caTTY-ts` is the astro project for this terminal emulator
    `caTTY-ts/src/ts/terminal` is where all the headless pure TypeScript code goes
    `caTTY-ts/src/pages/terminal` is where all web UI portion goes.  Implemented in React components stemming from root component `caTTY-ts/src/pages/terminal/_term/TerminalPage.tsx`


# References

* `dotnet-exe-link-libghostty` 
  contains a dotnet program which was created to test the same libghostty-vt library using the C compatible distribution from C#.  it contains three working examples of a OSC, SGR and Key Encoding demo CLI programs.

  This program is complicated by trying to run a terminal emulator and run it from a terminal, which has a lot of issues.

  caTTY will solve this by using a HTML web frontend to "paint" the terminal to and implement all state and logic as completely headless, utilizing libghostty-vt WASM for relevant terminal emulation functionality

* `ghostty-examples/web/encode.html` contains a known correct and working key encoding demo using the libghostty-vt WASM library
* `ghostty-examples/web/sgr.html` contains a known correct and working SGR demo using the libghostty-vt WASM library
* `caTTY-ts/WASM_NOTES.md` contains a list of all the libghostty-vt symbols as discovered by analyzing the WASM binary.  These should be used as a reference only, the encode.html and sgr.html demos show how to properly utilize the WASM functionality from a javascript runtime, which requires some special buffer helper patterns.

# Design

* The libghostty-vt function wrappers should encapsulate all the complexities of integration the TypeScript/JavaScript runtime with the libghostty-vt WASM C API, such as the custom buffers needed to pass data, etc.  The end-result should be a simple TypeScript, type safe function invocation that regular TypeScript code can invoke and not need to know anything about the underlying WASM API and it's special data structure requirements. 

# Tasks

* Implement TypeScript wrappers for all key encoding libghostty-vt functions using the encode.html demo as a reference.
* Implement a encoding demo page at `caTTY-ts/src/pages/demos/keyencode/index.astro` which renders the whole demo as a React component `KeyEncodeDemoPage.tsx`
* Implement TypeScript wrappers for all SGR libghostty-vt functions using the sgr.html demo as a reference.
* Implement a encoding demo page at `caTTY-ts/src/pages/demos/sgr/index.astro` which renders the whole demo as a React component `SgrDemoPage.tsx`
* Implement TypeScript wrappers for all OSC libghostty-vt functions, use the C# project OSC demo as reference.  The WASM function wrappers must be inferred from how the C# one works and follow the encode.html and sgr.html patterns for how the buffers etc work.
* Implement a encoding demo page at `caTTY-ts/src/pages/demos/sgr/index.astro` which renders the whole demo as a React component `SgrDemoPage.tsx`

