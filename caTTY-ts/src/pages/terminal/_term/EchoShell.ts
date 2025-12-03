/**
 * EchoShell - A simple shell simulation for testing the terminal.
 * 
 * This shell echoes back user input and supports a few basic commands.
 */

export class EchoShell {
  private inputBuffer: string = '';
  private onOutput: (data: string) => void;

  constructor(onOutput: (data: string) => void) {
    this.onOutput = onOutput;
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
        this.processCommand();
      } else if (code === 0x7F || code === 0x08) {
        // Backspace or Delete
        if (this.inputBuffer.length > 0) {
          this.inputBuffer = this.inputBuffer.slice(0, -1);
          // Send backspace sequence to terminal
          this.onOutput('\x08 \x08');
        }
      } else if (code >= 0x20 && code < 0x7F) {
        // Printable character
        this.inputBuffer += char;
        // Echo the character back
        this.onOutput(char);
      } else if (code === 0x03) {
        // Ctrl+C
        this.inputBuffer = '';
        this.onOutput('^C\r\n$ ');
      }
    }
  }

  /**
   * Processes a complete command.
   */
  private processCommand(): void {
    const command = this.inputBuffer.trim();
    this.inputBuffer = '';

    // Move to new line
    this.onOutput('\r\n');

    if (command === '') {
      // Empty command, just show prompt
      this.onOutput('$ ');
      return;
    }

    // Process built-in commands
    if (command === 'help') {
      this.onOutput('Available commands:\r\n');
      this.onOutput('  help     - Show this help message\r\n');
      this.onOutput('  clear    - Clear the screen\r\n');
      this.onOutput('  echo     - Echo arguments\r\n');
      this.onOutput('  date     - Show current date and time\r\n');
      this.onOutput('  colors   - Show color test\r\n');
      this.onOutput('  exit     - Exit message\r\n');
      this.onOutput('$ ');
    } else if (command === 'clear') {
      // Send clear screen sequence
      this.onOutput('\x1b[2J\x1b[H');
      this.onOutput('$ ');
    } else if (command.startsWith('echo ')) {
      const args = command.substring(5);
      this.onOutput(args + '\r\n');
      this.onOutput('$ ');
    } else if (command === 'date') {
      const now = new Date();
      this.onOutput(now.toString() + '\r\n');
      this.onOutput('$ ');
    } else if (command === 'colors') {
      this.showColorTest();
      this.onOutput('$ ');
    } else if (command === 'exit') {
      this.onOutput('Goodbye! (Terminal will remain active)\r\n');
      this.onOutput('$ ');
    } else {
      // Unknown command
      this.onOutput(`Command not found: ${command}\r\n`);
      this.onOutput('Type "help" for available commands.\r\n');
      this.onOutput('$ ');
    }
  }

  /**
   * Shows a color test with various SGR attributes.
   */
  private showColorTest(): void {
    this.onOutput('Color test:\r\n');
    
    // Basic colors
    this.onOutput('\x1b[31mRed\x1b[0m ');
    this.onOutput('\x1b[32mGreen\x1b[0m ');
    this.onOutput('\x1b[33mYellow\x1b[0m ');
    this.onOutput('\x1b[34mBlue\x1b[0m ');
    this.onOutput('\x1b[35mMagenta\x1b[0m ');
    this.onOutput('\x1b[36mCyan\x1b[0m\r\n');
    
    // Bold colors
    this.onOutput('\x1b[1;31mBold Red\x1b[0m ');
    this.onOutput('\x1b[1;32mBold Green\x1b[0m ');
    this.onOutput('\x1b[1;33mBold Yellow\x1b[0m\r\n');
    
    // Background colors
    this.onOutput('\x1b[41mRed BG\x1b[0m ');
    this.onOutput('\x1b[42mGreen BG\x1b[0m ');
    this.onOutput('\x1b[43mYellow BG\x1b[0m\r\n');
    
    // Text attributes
    this.onOutput('\x1b[1mBold\x1b[0m ');
    this.onOutput('\x1b[3mItalic\x1b[0m ');
    this.onOutput('\x1b[4mUnderline\x1b[0m ');
    this.onOutput('\x1b[9mStrikethrough\x1b[0m\r\n');
  }

  /**
   * Resets the shell state.
   */
  reset(): void {
    this.inputBuffer = '';
  }
}
