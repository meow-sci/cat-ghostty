using System.Reflection;
using Brutal.ImGuiApi;
using KSA;

namespace caTTY.Display.Rendering;

/// <summary>
///     Manages fonts used in the caTTY terminal.
/// </summary>
public class CaTTYFontManager
{
  public static Dictionary<string, ImFontPtr> LoadedFonts = new();
  private static bool _fontsLoaded;


  /// <summary>
  ///     Loads fonts explicitly for the game mod.
  ///     Based on BRUTAL ImGui font loading pattern for game mods.
  /// </summary>
  public static void LoadFonts()
  {
    if (_fontsLoaded)
    {
      return;
    }

    try
    {
      // Get the directory where the mod DLL is located
      string? dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

      if (!string.IsNullOrEmpty(dllDir))
      {
        string fontsDir = Path.Combine(dllDir, "TerminalFonts");

        if (Directory.Exists(fontsDir))
        {
          // Get all .ttf and .otf files from Fonts folder
                    string[] fontFiles = Directory.GetFiles(fontsDir, "*.iamttf");

          if (fontFiles.Length > 0)
          {
            ImGuiIOPtr io = ImGui.GetIO();
            ImFontAtlasPtr atlas = io.Fonts;

            for (int i = 0; i < fontFiles.Length; i++)
            {
              string fontPath = fontFiles[i];
              string fontName = Path.GetFileNameWithoutExtension(fontPath);

              Console.WriteLine($"CaTTYFontManager: Loading font: {fontPath}");


              if (File.Exists(fontPath))
              {
                // Use a reasonable default font size (32pt)
                float fontSize = 32.0f;
                var fontPathStr = new ImString(fontPath);

                bool fontPixelSnap = GameSettings.GetFontPixelSnap();
                float fontDensity = GameSettings.GetFontDensity() / 100f;

                                ImFontPtr font = atlas.AddFontFromFileTTF(fontPathStr, fontSize);
                LoadedFonts[fontName] = font;


                Console.WriteLine($"CaTTYFontManager: Loaded font '{fontName}' from {fontPath}");
              }
            }

            Console.WriteLine(
                $"CaTTYFontManager: Loaded {LoadedFonts.Count} fonts - {string.Join(", ", LoadedFonts.Keys)}");
          }
          else
          {
            Console.WriteLine("CaTTYFontManager: No font files found in Fonts folder");
          }
        }
        else
        {
          Console.WriteLine($"CaTTYFontManager: Fonts directory not found at: {fontsDir}");
        }
      }

      _fontsLoaded = true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"CaTTYFontManager: Error loading fonts: {ex.Message}");
    }
  }
}
