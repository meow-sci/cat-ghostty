
# Demo Programs

This project includes three demo programs showcasing different libghostty-vt functionality:

## Key Encoder Demo

Test the libghostty key encoder with various key combinations:

```bash
dotnet run -- --key-demo
```

Demonstrates key event encoding with the Kitty keyboard protocol. Compared output from various tests in the encode.html known example from the Ghostty repo, appears to be working correctly.

## OSC Parser Demo

Test the OSC (Operating System Command) parser:

```bash
dotnet run -- --osc-demo
```

Demonstrates parsing OSC sequences including window titles, PWD reports, clipboard operations, and color operations.

## SGR Parser Demo

Test the SGR (Select Graphic Rendition) attribute parser:

```bash
dotnet run -- --sgr-demo
```

Demonstrates parsing terminal styling sequences including:
- Text attributes (bold, italic, underline styles, etc.)
- 8-color and 16-color (bright) palettes
- 256-color mode
- RGB/truecolor mode
- Underline styles and colors
- Complex multi-attribute sequences

# build

```bash
dotnet publish -c Debug -r win-x64 --self-contained true
```

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```


# osc notes

at the moment, it looks like all the osc command response data structures are not defined for the C API

these should be defined in the GhosttyOscCommandData but currently (as of 2025-11-22) only 

```c
typedef enum {
  /** Invalid data type. Never results in any data extraction. */
  GHOSTTY_OSC_DATA_INVALID = 0,
  
  /** 
   * Window title string data.
   *
   * Valid for: GHOSTTY_OSC_COMMAND_CHANGE_WINDOW_TITLE
   *
   * Output type: const char ** (pointer to null-terminated string)
   *
   * Lifetime: Valid until the next call to any ghostty_osc_* function with 
   * the same parser instance. Memory is owned by the parser.
   */
  GHOSTTY_OSC_DATA_CHANGE_WINDOW_TITLE_STR = 1,
} GhosttyOscCommandData;
```

is defined, and in zig's `osc.zig` only that case is implemented

```zig
/// C: GhosttyOscCommandData
pub const CommandData = enum(c_int) {
    invalid = 0,
    change_window_title_str = 1,

    /// Output type expected for querying the data of the given kind.
    pub fn OutType(comptime self: CommandData) type {
        return switch (self) {
            .invalid => void,
            .change_window_title_str => [*:0]const u8,
        };
    }
};
```