# project overview

implement a very simple, minimum viable terminal emulator. 

this will leverage libghostty-vt for all key encoding, osc (Operating System Command) sequence parsing, sgr (Select Graphic Rendition) sequence parsing.

the implementation will be the "glue" which provides I/O for collecting user input and displaying the terminal emulator output.

it must be implemented with a clean MVC (model view controller) architecture with clear demarcation boundaries between functionality.

the model (state) will be custom code which implements the minimum scope of state that a terminal emulator needs to maintain.

the view will be simple characters printed to the console via stdout.

the controller will be custom code which collects user input from stdin, feeds it into the model/state, and then applies the result to the view.

# technology

* dotnet 9
* console application
* stdin for user input
* stdout to display the output

# assumptions

* hard-coded to a terminal size of 80x24 (80 wide, 24 high)

# code style

* idiomatic dotnet c#
* best practices
* cleanly abstracted functionality boundaries with classes

# plan

