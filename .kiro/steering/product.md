# Product Overview

caTTY is a TypeScript-based terminal emulator for the web that integrates the libghostty-vt WebAssembly library for core terminal emulation capabilities.

## Core Functionality

- Terminal key encoding (converting keyboard events to terminal escape sequences)
- OSC (Operating System Command) parsing
- SGR (Select Graphic Rendition) attribute parsing for text styling and colors
- Headless terminal emulation logic with web-based display

## Architecture

The project follows an MVC pattern:
- Model: Headless TypeScript logic + libghostty-vt WASM library
- Controller: Browser DOM event handling and data flow coordination
- View: HTML `<pre>` element with CSS absolute positioning for character rendering

## Demo Pages

The project includes interactive demo pages for:
- Key encoding (`/demos/keyencode`)
- SGR parsing (`/demos/sgr`)
- OSC parsing (`/demos/osc`)
- Full terminal emulator (`/terminal`)
