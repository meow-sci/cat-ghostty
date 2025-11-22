namespace dotnet_exe_link_libghostty;

public static class Catty
{
    public static int Main(string[] args)
    {
        Console.WriteLine("here!");
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
        }
        return 0;
    }
}