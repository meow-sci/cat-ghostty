# Building `libghostty-vt` for windows

Follow the instructions on https://ghostty.org/docs/install/build for how to setup a build environment

You can use the following Zig build command to cross-compile to windows to produce `ghostty-vt.dll`

```bash
zig build lib-vt -Dtarget=x86_64-windows
```

# libghostty-vt C api

For convenience for now I copy/pasted the headers to `dotnet-exe-link-libghostty/ghostty-src/src` so I could have AI/LLM look at it in trivially prompts, for reference this was copied from the ghostty git repo `main` branch at commit `9955b43e0c9f96b9c9c3dc7edc79aeb904749b16`


# proof of life using libghostty-vt on windows dotnet 9

`dotnet-exe-link-libghostty/Program.cs` is a simple dotnet 9 console application which calls out to `ghostty-vt.dll` to test it's working by instantiating an osc (Operating System Command) parser, sending a sequence of characters to it, then reading the terminal screen and printing it to stdout.

This is a simple proof that the headless ghostty terminal emulator osc parser is working in a dotnet 9 application on windows.

A copy of a pre-built `ghostty-vt.dll` is checked-in to `dotnet-exe-link-libghostty/lib/`

## to build

```bash
dotnet publish -c Debug -r win-x64 --self-contained true
```

## to run

on macOS via Wine

```bash
wine bin/Debug/net9.0/win-x64/publish/dotnet-exe-link-libghostty.exe 2>/dev/null
```

on Windows, just run the `dotnet-exe-link-libghostty.exe` program from a terminal
