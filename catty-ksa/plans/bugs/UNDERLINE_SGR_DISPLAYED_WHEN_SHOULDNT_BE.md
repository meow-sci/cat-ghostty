when nvim first launches it renders most of the content with a underline and this is not correct.

when i page up/down to get the screen to repaint everything fixes itself, so it's only an issue during the initial nvim full screen TUI painting.

somehow this must be related to a SGR command missed, or some kind of SGR convention not resetting a styles when it was expected to.

analyze SGR underlying related handling and ensure they are all ECMA/vt100/xterm compliant behaviors, look for any kind of stale state bugs etc that might arise.

