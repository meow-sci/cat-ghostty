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

## Features

- Implements a simple shell "backend" called SampleShell which supports a few features to demonstrate that the terminal emulator is working
    - ls - display a simple list of five dummy filenames
    - echo [arg] - echo back the contents like echo in bash would do
    - red [arg] - echo text in red color using SGR escape sequences
    - green [arg] - echo text in green color using SGR escape sequences
    - [ctrl + l] - this keystroke clears the screen and resets the cursor position to the beginning

## Backend

- Ability to connect to a real PTY shell session backend, a WebSocket will transport the data between the caTTY terminal and the pty process backend.  Uses `@lydell/node-pty` npm package to provide pty from node.  When the UI launches, it will connect to a websocket on the node backend, when the node backend opens a new websocket it will launch a pty shell and shuffle the data back/forth.  When the WebSocket disconnects, the pty shell will be killed.  If the end-user exits the pty process, the websocket will be killed and no data will be exchanged.

## Kitty Graphics Protocol

Full support for the Kitty graphics protocol.