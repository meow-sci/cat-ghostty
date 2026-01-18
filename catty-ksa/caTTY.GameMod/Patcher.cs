using HarmonyLib;
using Brutal.GlfwApi;
using caTTY.Display.Controllers;


[HarmonyPatch]
internal static class Patcher
{
  private static Harmony? m_harmony = new Harmony("caTTY");

  public static void patch()
  {
    System.Console.WriteLine("[caTTY] Installing Harmony patches...");

    // Patch GameMod assembly (keyboard intercept)
    m_harmony?.PatchAll(typeof(Patcher).Assembly);
    System.Console.WriteLine($"[caTTY] Patched GameMod assembly: {typeof(Patcher).Assembly.GetName().Name}");

    // Patch CustomShells assembly (GameConsoleShell Harmony patches)
    m_harmony?.PatchAll(typeof(caTTY.CustomShells.GameConsoleShell).Assembly);
    System.Console.WriteLine($"[caTTY] Patched CustomShells assembly: {typeof(caTTY.CustomShells.GameConsoleShell).Assembly.GetName().Name}");

    System.Console.WriteLine("[caTTY] Harmony patches installed successfully");
  }

  public static void unload()
  {
    m_harmony?.UnpatchAll("caTTY");
    m_harmony = null;
  }
}

[HarmonyPatch(typeof(KSA.Program))]
class Patch01
{

  [HarmonyPrefix]
  [HarmonyPatch(nameof(KSA.Program.OnKey))]
  static bool Prefix1(GlfwWindow window, GlfwKey key, int scanCode, GlfwKeyAction action, GlfwModifier mods)
  {
    if (TerminalController.IsAnyTerminalActive)
    {
      // skipping Program.OnKey
      return false;
    }
    return true;
  }
}
