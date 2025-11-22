namespace dotnet_exe_link_libghostty;

public static class Catty
{
    public static int Main(string[] args)
    {
        Console.WriteLine("here!");
        if (args != null && args.Length > 0 && (args[0] == "--key-demo" || args[0] == "keydemo"))
        {
            return KeyDemoProgram.Run(args);
        }
        return 0;
    }
}