using System;
using System.IO;
using System.Runtime.InteropServices;

internal static class ImGuiTestAppNative
{
    // On macOS, DllImport("example_imgui_lib") maps to libexample_imgui_lib.dylib.
    // On Linux, it maps to libexample_imgui_lib.so.
    // On Windows, it maps to example_imgui_lib.dll.
    private const string LibraryName = "example_imgui_lib";

    // The exported function blocks until the window is closed.
    [DllImport(LibraryName, EntryPoint = "imgui_testapp_run", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Run();
}

internal static class Program
{
    // For macOS/GLFW, creating the window + pumping events generally needs to happen
    // on the process main thread. A console app's Main() is the main thread.
    public static int Main(string[] args)
    {
        TryLoadNativeLibraryFromAppDirectory();

        Console.WriteLine("Launching ImGui test app (close the window to return)...");
        var rc = ImGuiTestAppNative.Run();
        Console.WriteLine($"ImGui test app exited with code {rc}.");
        return rc;
    }

    private static void TryLoadNativeLibraryFromAppDirectory()
    {
        // If the dynamic loader can already find the library via rpath / DYLD_LIBRARY_PATH,
        // this is unnecessary. This helper makes local dev easier by loading from the
        // managed executable directory.
        var baseDir = AppContext.BaseDirectory;

        string? fileName = null;
        if (OperatingSystem.IsMacOS())
            fileName = "libexample_imgui_lib.dylib";
        else if (OperatingSystem.IsLinux())
            fileName = "libexample_imgui_lib.so";
        else if (OperatingSystem.IsWindows())
            fileName = "example_imgui_lib.dll";

        if (fileName == null)
            return;

        var path = Path.Combine(baseDir, fileName);
        if (!File.Exists(path))
            return;

        NativeLibrary.Load(path);
    }
}
