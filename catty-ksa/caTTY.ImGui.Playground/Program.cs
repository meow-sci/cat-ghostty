using System;
using caTTY.Playground.Experiments;

namespace caTTY.Playground;

/// <summary>
/// ImGui playground application for experimenting with terminal rendering techniques.
/// This application provides a standalone environment for testing ImGui rendering
/// approaches before implementing the full terminal controller.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the ImGui playground application.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    public static void Main(string[] args)
    {
        Console.WriteLine("caTTY ImGui Playground - Terminal Rendering Experiments");
        Console.WriteLine("========================================================");
        Console.WriteLine();
        
        TerminalRenderingExperiments.Run();
        
        Console.WriteLine();
        Console.WriteLine("Exiting...");
        // Console.WriteLine("Press any key to exit...");
        // Console.ReadKey();
    }
}