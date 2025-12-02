# Overall Plan

To embed the `libghostty-vt` WASM library into a web project to provide some headless terminal emulation capabilities (OSC aka operating system command parsing, SGR aka Select Graphic Rendition parsing and Key Encoding).

Additional state and glue code must be implemented in TypeScript in a headless design which to implement the remaining functionality of a terminal emulator that `libghostty-vt` does not provide.  Together these form the "model" of a MVC design.

A well demarcated TypeScript implementation must also be implemented for the "controller" aspect of the terminal emulator which is also headless and will glue together the browser DOM elements and user interaction events and shuffle the data in/out of the model and then paint it to the view.

The view will be a simple browser DOM `<pre>` of a fixed width/height characters that the terminal screen state is projected onto using CSS absolute positioning of each character in a discrete `<span>`

Custom code must be created to implement the terminal "frontend", which covers, at least, capturing focus and user input,
sending that input to libghostty-vt for terminal emulation, and then visually displaying a terminal UI in an ImGui window
using Vulkan.


# Technology Overview

`libghostty-vt` is a library which provides headless terminal emulation capabilities via a C interface.

libghostty-vt has zero dependencies, is cross-platform and exposes it's API as a extern C interface for maximum interoperatbility and portability.


# Reference Code

The libghostty-vt WASM api (from the C API) has its exported functions noted in `WASM_NOTES.md`
