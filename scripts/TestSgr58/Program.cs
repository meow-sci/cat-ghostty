using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== SGR 58 (Underline Color) Test ===");
        Console.WriteLine();
        
        // Test 1: Basic underline with red color
        Console.WriteLine("Test 1: Basic underline with red color");
        Console.Write("\x1b[4m\x1b[58;2;255;0;0mRed underline text\x1b[0m");
        Console.WriteLine();
        Console.WriteLine();
        
        // Test 2: Double underline with green color
        Console.WriteLine("Test 2: Double underline with green color");
        Console.Write("\x1b[21m\x1b[58;2;0;255;0mGreen double underline text\x1b[0m");
        Console.WriteLine();
        Console.WriteLine();
        
        // Test 3: Curly underline with blue color
        Console.WriteLine("Test 3: Curly underline with blue color");
        Console.Write("\x1b[4:3m\x1b[58;2;0;0;255mBlue curly underline text\x1b[0m");
        Console.WriteLine();
        Console.WriteLine();
        
        // Test 4: Combined sequence (should work the same)
        Console.WriteLine("Test 4: Combined sequence");
        Console.Write("\x1b[4;58;2;255;255;0mYellow underline text\x1b[0m");
        Console.WriteLine();
        Console.WriteLine();
        
        Console.WriteLine("=== End Test ===");
        Console.WriteLine();
        Console.WriteLine("If SGR 58 is working correctly, you should see colored underlines.");
        Console.WriteLine("If not working, underlines will be the default color.");
    }
}
