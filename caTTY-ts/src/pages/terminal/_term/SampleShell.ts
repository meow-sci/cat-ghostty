/**
 * SampleShell - A demonstration shell backend for testing terminal functionality.
 * 
 * Supports basic commands: ls, echo, and Ctrl+L for screen clearing.
 */

export interface ShellConfig {
  onOutput: (data: string) => void;
}

export class SampleShell {
  private onOutput: (data: string) => void;
  private currentLine: string = '';
  private prompt: string = '$ ';

  constructor(config: ShellConfig) {
    this.onOutput = config.onOutput;
  }

  /**
   * Processes input data from the terminal.
   * @param data Input data as Uint8Array
   */
  processInput(data: Uint8Array): void {
    const text = new TextDecoder().decode(data);

    for (const char of text) {
      const code = char.charCodeAt(0);

      // Handle special characters
      if (code === 0x0D || code === 0x0A) {
        // Enter key - process command
        this.handleEnter();
      } else if (code === 0x7F || code === 0x08) {
        // Backspace or Delete
        this.handleBackspace();
      } else if (code === 0x0C) {
        // Ctrl+L - clear screen
        this.handleClearScreen();
      } else if (code >= 0x20 && code < 0x7F) {
        // Printable character
        this.currentLine += char;
        // Echo the character back
        this.onOutput(char);
      }
    }
  }

  /**
   * Handles backspace key.
   */
  private handleBackspace(): void {
    if (this.currentLine.length > 0) {
      this.currentLine = this.currentLine.slice(0, -1);
      // Send backspace sequence to terminal
      this.onOutput('\x08 \x08');
    }
  }

  /**
   * Handles Enter key - processes the command.
   */
  private handleEnter(): void {
    const command = this.currentLine.trim();
    this.currentLine = '';

    // Move to new line
    this.onOutput('\r\n');

    if (command === '') {
      // Empty command, just show prompt
      this.displayPrompt();
      return;
    }

    // Parse command and arguments
    const parts = command.split(/\s+/);
    const cmd = parts[0];
    const args = parts.slice(1).join(' ');

    // Route to appropriate command handler
    if (cmd === 'ls') {
      this.handleLs();
    } else if (cmd === 'echo') {
      this.handleEcho(args);
    } else {
      this.handleUnknownCommand(cmd);
    }
  }

  /**
   * Handles the ls command.
   */
  private handleLs(): void {
    this.onOutput('file1.txt\r\n');
    this.onOutput('file2.txt\r\n');
    this.onOutput('file3.txt\r\n');
    this.onOutput('file4.txt\r\n');
    this.onOutput('file5.txt\r\n');
    this.displayPrompt();
  }

  /**
   * Handles the echo command.
   */
  private handleEcho(args: string): void {
    this.onOutput(args + '\r\n');
    this.displayPrompt();
  }

  /**
   * Handles Ctrl+L - clear screen.
   */
  private handleClearScreen(): void {
    // Send CSI J (clear screen) and CSI H (cursor home)
    this.onOutput('\x1b[2J\x1b[H');
    this.displayPrompt();
  }

  /**
   * Handles unknown commands.
   */
  private handleUnknownCommand(command: string): void {
    this.onOutput(`${command}: command not found\r\n`);
    this.displayPrompt();
  }

  /**
   * Displays the prompt.
   */
  private displayPrompt(): void {
    this.onOutput(this.prompt);
  }

  /**
   * Resets the shell state.
   */
  reset(): void {
    this.currentLine = '';
  }
}
