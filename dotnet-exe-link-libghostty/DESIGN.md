# project technology

* dotnet 9
* console application
* output as windows exe
* self-contained true on output exe


# third party

* lib/ghostty-vt.dll - a win x64 DLL which contains a C API for "libghostty-vt" which is a headless terminal emulator library
    * the C api header entrypoint is defined in `ghostty-src/src/vt.h` which imports header files in subdirs
    * an example Zig console program is in `ghostty-src/example` which uses a osc (Operating System Command) parser to prove the library is working


# project to generate

`Program.cs` must

* import the libghostty-vt osc parser
* Create a parser instance with ghostty_osc_new()
* Feed bytes to the parser using ghostty_osc_next() 
* Finalize parsing with ghostty_osc_end() to get the command
* Query command type and extract data using ghostty_osc_command_type() and ghostty_osc_command_data()
* Free the parser with ghostty_osc_free() when done

Use `ghostty-src/example/src/main.c` as an example C program and do the same thing but in a dotnet 9 C# compatible console application
