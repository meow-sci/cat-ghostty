The base custom shell code which supports line editing and history via up/down is missing some features:

* ctrl+c shortcut should cancel/clear the current command buffer being edited
* ctrl+backspace shortcut should delete by "word" on the current command buffer
* cursor position for command buffer not implemented
    * left/right arrows should allow per-character moving of cursor
    * ctrl+left/right arrow should jump by word
    * home/end should jump to start or end of line
    * del should delete the next char (to the right of cursor), opposite of what backspace does by default
