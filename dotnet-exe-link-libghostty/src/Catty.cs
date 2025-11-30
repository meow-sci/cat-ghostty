namespace dotnet_exe_link_libghostty;

using dotnet_exe_link_libghostty.Terminal;
using System.Text;

public static class Catty
{
    public static int Main(string[] args)
    {
        if (args != null && args.Length > 0)
        {
            if (args[0] == "--key-demo" || args[0] == "keydemo")
            {
                return KeyDemoProgram.Run(args);
            }
            else if (args[0] == "--osc-demo" || args[0] == "oscdemo")
            {
                return OscDemoProgram.Run(args);
            }
            else if (args[0] == "--sgr-demo" || args[0] == "sgrdemo")
            {
                return SgrDemoProgram.Run(args);
            }
            else if (args[0] == "--terminal" || args[0] == "terminal")
            {
                return RunTerminalEmulator(args);
            }
            else if (args[0] == "--terminal-test" || args[0] == "terminaltest")
            {
                return RunTerminalTest(args);
            }
        }
        
        Console.WriteLine("Usage:");
        Console.WriteLine("  --key-demo       : Run key encoder demo");
        Console.WriteLine("  --osc-demo       : Run OSC parser demo");
        Console.WriteLine("  --sgr-demo       : Run SGR parser demo");
        Console.WriteLine("  --terminal       : Run terminal emulator (full screen)");
        Console.WriteLine("  --terminal-test  : Test terminal I/O without full screen");
        return 0;
    }
    
    private static int RunTerminalEmulator(string[] args)
    {
        try
        {
            using var emulator = new TerminalEmulator();
            emulator.Start();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Terminal emulator error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }
    
    private static int RunTerminalTest(string[] args)
    {
        Console.WriteLine("=== Terminal I/O Test ===");
        Console.WriteLine("Starting shell process...");
        
        try
        {
            using var processManager = new ProcessManager();
            processManager.Start();
            
            Console.WriteLine("Shell started. Waiting for output...");
            Thread.Sleep(500); // Give shell time to start
            
            // Read and display initial output (prompt)
            var output = processManager.ReadOutput();
            if (output != null)
            {
                Console.Write("Initial output: ");
                Console.WriteLine(Encoding.UTF8.GetString(output));
            }
            else
            {
                Console.WriteLine("No initial output");
            }
            
            // Send a simple command
            Console.WriteLine("\nSending command: echo 'Hello from terminal test'");
            var command = Encoding.UTF8.GetBytes("echo 'Hello from terminal test'\n");
            processManager.SendInput(command);
            
            // Read response
            Thread.Sleep(200);
            for (int i = 0; i < 10; i++)
            {
                output = processManager.ReadOutput();
                if (output != null)
                {
                    Console.Write(Encoding.UTF8.GetString(output));
                }
                
                var error = processManager.ReadError();
                if (error != null)
                {
                    Console.Error.Write(Encoding.UTF8.GetString(error));
                }
                
                Thread.Sleep(100);
            }
            
            Console.WriteLine("\n\nTest complete. Process is running: " + processManager.IsRunning);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Test error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}