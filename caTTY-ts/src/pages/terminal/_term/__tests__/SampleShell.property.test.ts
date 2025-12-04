import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { SampleShell } from '../SampleShell.js';

describe('SampleShell Property Tests', () => {
  /**
   * Feature: headless-terminal-emulator, Property 61: ls command output format
   * Validates: Requirements 21.2
   * 
   * For any invocation of the "ls" command, SampleShell should output exactly five dummy filenames
   */
  it('Property 61: ls command outputs exactly five filenames', () => {
    fc.assert(
      fc.property(
        fc.constantFrom('ls', 'ls ', 'ls  ', '  ls  ', '\tls\t'), // Various whitespace variations
        (lsCommand) => {
          let output = '';
          const shell = new SampleShell({
            onOutput: (data: string) => {
              output += data;
            },
          });

          // Send the ls command
          const encoder = new TextEncoder();
          shell.processInput(encoder.encode(lsCommand));
          shell.processInput(encoder.encode('\r')); // Enter key

          // Count the number of filenames in output
          const lines = output.split('\r\n').filter((line) => line.trim().length > 0);
          const filenames = lines.filter((line) => line.match(/^file\d+\.txt$/));

          // Should have exactly 5 filenames
          expect(filenames.length).toBe(5);
          expect(filenames).toEqual([
            'file1.txt',
            'file2.txt',
            'file3.txt',
            'file4.txt',
            'file5.txt',
          ]);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 62: echo command reflects input
   * Validates: Requirements 21.3
   * 
   * For any string argument passed to the "echo" command, SampleShell should output that exact string back to the terminal
   */
  it('Property 62: echo command reflects input', () => {
    fc.assert(
      fc.property(
        fc.string({ minLength: 0, maxLength: 100 }).filter((s) => !s.includes('\r') && !s.includes('\n')),
        (args) => {
          let output = '';
          const shell = new SampleShell({
            onOutput: (data: string) => {
              output += data;
            },
          });

          // Send the echo command with arguments
          const encoder = new TextEncoder();
          const command = `echo ${args}`;
          shell.processInput(encoder.encode(command));
          shell.processInput(encoder.encode('\r')); // Enter key

          // Extract the echoed content (between first \r\n and second \r\n)
          const lines = output.split('\r\n');
          const echoedLine = lines[1]; // First line is the echoed command, second is the output

          // The shell parses arguments by splitting on whitespace, so we need to account for that
          // When args is just whitespace, it becomes empty after parsing
          const expectedOutput = args.split(/\s+/).filter(s => s.length > 0).join(' ');
          
          // Should echo back the parsed arguments
          expect(echoedLine).toBe(expectedOutput);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 63: Ctrl+L clears screen
   * Validates: Requirements 21.4
   * 
   * For any terminal state, when Ctrl+L is received, SampleShell should send escape sequences that clear the screen and reset cursor to position (0, 0)
   */
  it('Property 63: Ctrl+L sends clear screen sequences', () => {
    fc.assert(
      fc.property(
        fc.string({ minLength: 0, maxLength: 50 }).filter((s) => !s.includes('\r') && !s.includes('\n') && !s.includes('\x0C')),
        (beforeText) => {
          let output = '';
          const shell = new SampleShell({
            onOutput: (data: string) => {
              output += data;
            },
          });

          // Send some text first
          const encoder = new TextEncoder();
          if (beforeText.length > 0) {
            shell.processInput(encoder.encode(beforeText));
          }

          // Clear output buffer
          output = '';

          // Send Ctrl+L
          shell.processInput(encoder.encode('\x0C'));

          // Should contain CSI J (clear screen) and CSI H (cursor home)
          expect(output).toContain('\x1b[2J'); // CSI 2 J - clear entire screen
          expect(output).toContain('\x1b[H');  // CSI H - cursor home
          expect(output).toContain('$ ');      // Should display prompt after clearing
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 64: Unknown command error handling
   * Validates: Requirements 21.5
   * 
   * For any unrecognized command, SampleShell should output an error message indicating the command was not found
   */
  it('Property 64: unknown commands produce error messages', () => {
    fc.assert(
      fc.property(
        fc.string({ minLength: 1, maxLength: 20 })
          .filter((s) => !s.includes('\r') && !s.includes('\n') && !s.includes(' '))
          .filter((s) => s !== 'ls' && s !== 'echo'), // Exclude known commands
        (unknownCommand) => {
          let output = '';
          const shell = new SampleShell({
            onOutput: (data: string) => {
              output += data;
            },
          });

          // Send the unknown command
          const encoder = new TextEncoder();
          shell.processInput(encoder.encode(unknownCommand));
          shell.processInput(encoder.encode('\r')); // Enter key

          // Should contain error message with the command name
          expect(output).toContain(`${unknownCommand}: command not found`);
          expect(output).toContain('$ '); // Should display prompt after error
        }
      ),
      { numRuns: 100 }
    );
  });
});
