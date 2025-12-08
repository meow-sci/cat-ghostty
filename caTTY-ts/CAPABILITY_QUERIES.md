# Terminal Capability Query Support

This document describes the terminal capability query responses implemented in caTTY to ensure compatibility with applications like `viu`, `htop`, and other terminal programs that probe terminal capabilities.

## Overview

Many terminal applications send escape sequence queries to detect terminal capabilities before using advanced features. If the terminal doesn't respond to these queries, applications may hang indefinitely waiting for a response.

## Implemented Query Responses

### 1. Device Status Report (DSR)

**Query:** `ESC [ 5 n`  
**Response:** `ESC [ 0 n` (terminal OK)

**Query:** `ESC [ 6 n` (Cursor Position Report)  
**Response:** `ESC [ <row> ; <col> R` (1-based coordinates)

### 2. Primary Device Attributes (DA1)

**Query:** `ESC [ c`  
**Response:** `ESC [ ? 1 ; 2 ; 6 ; 22 c`

Capabilities reported:
- 1 = 132 columns
- 2 = Printer port
- 6 = Selective erase
- 22 = Color text

### 3. Secondary Device Attributes (DA2)

**Query:** `ESC [ > c`  
**Response:** `ESC [ > 41 ; 0 ; 0 c`

Response format:
- 41 = xterm terminal type
- 0 = version
- 0 = ROM cartridge registration number (not used)

### 4. Kitty Graphics Protocol Query

**Query:** `ESC_G a=q ESC\` or `ESC_G i=<id>,a=q ESC\`  
**Response:** `ESC_G OK ESC\` or `ESC_G i=<id>;OK ESC\`

This indicates support for the Kitty graphics protocol, allowing applications like `viu` to display inline images.

## Implementation Details

### Parser Changes

The Parser was updated to properly handle CSI private markers (characters in the 0x3C-0x3F range like `?`, `>`, `=`, `<`):

- Private markers are now stored and passed to CSI handlers
- The `onCsi` handler signature was updated to include an optional `privateMarker` parameter
- Sequences like `ESC [ > c` are now correctly parsed and dispatched
- When a private marker is encountered in CsiEntry state, it is stored and the parser stays in CsiEntry to handle subsequent bytes

### Terminal Changes

The Terminal class now implements:

- `handleDeviceStatusReport()` - Responds to DSR queries (5 and 6)
- `handleDeviceAttributes()` - Responds to DA1 and DA2 queries based on private marker
- `handleGraphicsQuery()` - Responds to Kitty graphics capability queries

All responses are sent back to the client via the `onDataOutput` event handler.

**Important Note:** DEC private mode sequences (ESC[?...h and ESC[?...l) are parsed but NOT executed to maintain compatibility with existing applications like htop. The parser recognizes these sequences and passes them to handlers, but the Terminal ignores them. This allows capability queries to work while preventing display corruption. Full DEC private mode support will be added in a future update.

## Testing

Comprehensive tests are included in `Terminal.capability-queries.test.ts`:

- Device Status Report (status and cursor position)
- Primary Device Attributes
- Secondary Device Attributes
- Kitty Graphics Protocol queries (simple and with image ID)
- Multiple queries in sequence

## Compatibility

These implementations follow the VT100/VT220/xterm specifications and are compatible with:

- `viu` - Image viewer that uses Kitty graphics protocol
- `htop` - Process viewer
- `vim`/`neovim` - Text editors
- `tmux` - Terminal multiplexer
- Other terminal applications that probe capabilities

## References

- [ECMA-48 / ISO-6429](https://www.ecma-international.org/publications-and-standards/standards/ecma-48/) - Control Functions for Coded Character Sets
- [xterm Control Sequences](https://invisible-island.net/xterm/ctlseqs/ctlseqs.html)
- [Kitty Graphics Protocol](https://sw.kovidgoyal.net/kitty/graphics-protocol/)
- [VT100 User Guide](https://vt100.net/docs/vt100-ug/)
