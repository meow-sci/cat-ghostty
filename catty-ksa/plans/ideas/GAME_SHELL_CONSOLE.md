# Overview

The game has a built-in console that this feature will repliace it's behavior.

The behavior to replicate is submitting user-entered commands from the line discipline, then hand off the text to the game's existing `TerminalInterface.Execute` and display the results (success or error messages).

This feature should be a *COMPLETE* custom shell that is attached to our terminal emulator instead of a "real" shell like PowerShell, bash etc.

It should be launched in the Sessions menu as a new menu item "Game Console" as a new terminal session.

# KSA DLL references

Key information I've gathered from exploring the KSA game DLL assemblies

- `TerminalInterface` is a type which executes terminal commands and produces output
- `KSA.Program.TerminalInterface` is static reference to the singleton `TerminalInterface` instance
- `bool TerminalInterface.Execute(string)` sends a command, true on command success, false on error
- During game startup, a action handler is registered on `TerminalInterface` like this: 
    `Program.TerminalInterface.OnOutput += (Action<string, TerminalInterfaceOutputType>) ((s, type) => Program.ConsoleWindow.Print($"{type}: {s}"));`
    We can use this as an example of how to hook into results from commands to send the output to our shell for display (e.g. stdout of the executed command)
- `TerminalInterfaceOutputType` is a enum with possible values [Message, Error] (we would treat Message as stdout, and Error as stderr in our simulated shell)

# Behavior

- Simulate how a real shell works generally when submitting a command:
    - When the command is sent (with enter), instead of launching a program, send to `TerminalInterface.Execute(string)`
    - When our OnOutput action handler is invoked, based on the `TerminalInterfaceOutputType` treat it as stderr or stdout
    - When stdout, print with default color
    - When stderr, print with red color
- Typical clear command shortcuts that bash supports should work like ctrl+l
