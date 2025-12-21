using HarmonyLib;
using KSA;

namespace KsaExampleMod
{
    [HarmonyPatch]
    internal static class Patcher
    {
        private static Harmony? _harmony = new Harmony("KsaExampleMod");

        public static void Patch()
        {
            Console.WriteLine("Patching KsaExampleMod...");
            _harmony?.PatchAll(typeof(Patcher).Assembly);
        }

        public static void Unload()
        {
            _harmony?.UnpatchAll(_harmony.Id);
            _harmony = null;
        }

        [HarmonyPatch(typeof(ModLibrary), nameof(ModLibrary.LoadAll))]
        [HarmonyPostfix]
        public static void AfterLoad()
        {
            Console.WriteLine("ModLibrary.LoadAll patched by KsaExampleMod.");
        }
    }
}