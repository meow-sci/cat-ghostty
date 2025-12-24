# Fonts

Fonts will be loaded automatically.  Any font file ending in ".ttf" in a `Content/` folder in the pwd of the program will be loaded, the "name" it can be looked up by at runtime matches the filename before the `.ttf` filename extension.

For example file `Content/Hack.ttf` can be referenced in code by the name `Hack`

# Setting font in BRUTAL ImGui code

This uses a Push / Pop sematic pattern.

Here are some example functions which simplify that for a known font name.

```cs
using KSA; // FontManager is under KSA namespace

private static void PushHackFont(out bool fontUsed, float size)
{
  if (FontManager.Fonts.TryGetValue("HackNerdFontMono-Regular", out ImFontPtr fontPtr))
  {
    ImGui.PushFont(fontPtr, size);
    fontUsed = true;
    return;
  }

  fontUsed = false;
}

private static void MaybePopFont(bool wasUsed)
{
  if (wasUsed) {
    ImGui.PopFont();
  }
}
```

## Preferred Font

`HackNerdFontMono-Regular.ttf` file with name `HackNerdFontMono-Regular` in code

This is the "Hack" font with nerd extensions (glyphs used in terminals) in regular weight and monospaced.


## GameMod font loading

When run inside a game mod, fonts must be loaded explicitly (as opposed to standalone apps which auto detect font files)

Here's an example (janky, but useful as reference) class which loads fonts programmatically in BRUTAL ImGui

```cs
public partial class Compendium
{
    public static string[] fontFiles = Array.Empty<string>();
    public static string[] fontNames = Array.Empty<string>();
    public static Dictionary<string, ImFontPtr> loadedFonts = new Dictionary<string, ImFontPtr>();

    public static void LoadFonts(string? dllDir, float fontSizeCurrent)
    {
        if (!string.IsNullOrEmpty(dllDir))
        {
            string fontsDir = Path.Combine(dllDir, "Fonts");
            
            if (Directory.Exists(fontsDir))
            {
                // Get all .ttf and .otf files from Fonts folder
                var ttfFiles = Directory.GetFiles(fontsDir, "*.ttf");
                var otfFiles = Directory.GetFiles(fontsDir, "*.otf");
                fontFiles = ttfFiles.Concat(otfFiles).ToArray();
                
                if (fontFiles.Length > 0)
                {
                    unsafe
                    {
                        ImGuiIO* io = ImGui.GetIO();
                        if (io != null)
                        {
                            ImFontAtlasPtr atlas = io->Fonts;
                            fontNames = new string[fontFiles.Length];
                            
                            for (int i = 0; i < fontFiles.Length; i++)
                            {
                                string fontPath = fontFiles[i];
                                string fontName = Path.GetFileNameWithoutExtension(fontPath);
                                fontNames[i] = fontName;
                                
                                if (File.Exists(fontPath))
                                {
                                    ImString fontPathStr = new ImString(fontPath);
                                    ImFontPtr font = atlas.AddFontFromFileTTF(fontPathStr, fontSizeCurrent);
                                    loadedFonts[fontName] = font;
                                }
                            }
                            // Now writes to console a single string with all loaded font names
                            Console.WriteLine($"Compendium: {fontNames.Length} Loaded fonts - " + string.Join(", ", fontNames));

                            // Now if the fontnames array has 'Regular' style, set that as default selected font index
                            for (int i = 0; i < fontNames.Length; i++)
                            {
                                if (fontNames[i].EndsWith("Regular", StringComparison.OrdinalIgnoreCase))
                                {
                                    selectedFontIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Compendium: No font files found in Fonts folder");
                }
            }
            else
            { Console.WriteLine($"Compendium: Fonts directory not found at: {fontsDir}"); }
        }
    }
}
```