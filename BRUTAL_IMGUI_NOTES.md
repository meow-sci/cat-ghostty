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
