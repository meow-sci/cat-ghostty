const std = @import("std");
const hello_world = @import("hello_world");

pub fn main() !void {
    // Prints to stderr, ignoring potential errors.
    std.debug.print("All your {s} are belong to us.\n", .{"codebase"});
    try hello_world.bufferedPrint();
}
