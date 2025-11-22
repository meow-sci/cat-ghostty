namespace dotnet_exe_link_libghostty;

using dotnet_exe_link_libghostty.Terminal;

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
        }
        
        Console.WriteLine("Usage:");
        Console.WriteLine("  --key-demo    : Run key encoder demo");
        Console.WriteLine("  --osc-demo    : Run OSC parser demo");
        Console.WriteLine("  --sgr-demo    : Run SGR parser demo");
        Console.WriteLine("  --terminal    : Run terminal emulator");
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
}