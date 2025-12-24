using System.Text;
using Brutal.ImGuiApi;
using KSA;
using BrutalImGui = Brutal.ImGuiApi.ImGui;
using float2 = Brutal.Numerics.float2;
using float4 = Brutal.Numerics.float4;

namespace caTTY.TestGameMod.ImGui;

/// <summary>
/// ImGui terminal controller that handles display and input for the terminal emulator.
/// This is the shared controller implementation that is used by both the TestApp and GameMod.
/// </summary>
public class TestModFonts
{
  private bool _isVisible = true;
  private bool _hasFocus = false;
  private bool _disposed = false;

  // Font and rendering settings (now config-based)
  private float _fontSize = 64.0f;
  private float _charWidth = 19.2f;
  private float _lineHeight = 36.0f;

  // Input handling
  private readonly StringBuilder _inputBuffer = new();

  /// <summary>
  /// Gets or sets whether the terminal window is visible.
  /// </summary>
  public bool IsVisible
  {
    get => _isVisible;
    set => _isVisible = value;
  }

  /// <summary>
  /// Gets whether the terminal window currently has focus.
  /// </summary>
  public bool HasFocus => _hasFocus;

  /// <summary>
  /// Gets the current font size for debugging purposes.
  /// </summary>
  public float CurrentFontSize => _fontSize;

  /// <summary>
  /// Gets the current character width for debugging purposes.
  /// </summary>
  public float CurrentCharacterWidth => _charWidth;

  /// <summary>
  /// Gets the current line height for debugging purposes.
  /// </summary>
  public float CurrentLineHeight => _lineHeight;


  public TestModFonts()
  {
  }



  /// <summary>
  /// Renders the terminal window using ImGui.
  /// </summary>
  public void Render()
  {
    if (!_isVisible)
      return;

    // Push monospace font if available
    PushMonospaceFont(out bool fontUsed);

    try
    {
      // Create terminal window
      BrutalImGui.Begin("Terminal", ref _isVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

      // Track focus state
      _hasFocus = BrutalImGui.IsWindowFocused();

      // Display terminal info
      BrutalImGui.Text($"Terminal: 80x24");
      BrutalImGui.SameLine();



      BrutalImGui.End();
    }
    finally
    {
      MaybePopFont(fontUsed);
    }
  }


  
  /// <summary>
  /// Pushes a monospace font if available.
  /// </summary>
  private void PushMonospaceFont(out bool fontUsed)
  {
    // First try the standard FontManager (works in standalone apps)
    try
    {
      if (FontManager.Fonts.TryGetValue("HackNerdFontMono-BoldItalic", out ImFontPtr fontPtr))
      {
        BrutalImGui.PushFont(fontPtr, _fontSize);
        fontUsed = true;
        return;
      }
    }
    catch (Exception ex)
    {
      // FontManager.Fonts may not be available in game mod context
      Console.WriteLine($"FontManager.Fonts not available: {ex.Message}");
      Console.WriteLine(ex.StackTrace);

    }

    // Try the GameMod's font loading system (works in game mod context)
    try
    {
      // Use reflection to call the GameMod's GetFont method
      var gameModType = Type.GetType("caTTY.GameMod.TerminalMod, caTTY");
      if (gameModType != null)
      {
        var getFontMethod = gameModType.GetMethod("GetFont", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (getFontMethod != null)
        {
          var result = getFontMethod.Invoke(null, new object[] { "HackNerdFontMono-BoldItalic" });
          if (result is ImFontPtr font)
          {
            BrutalImGui.PushFont(font, _fontSize);
            fontUsed = true;
            return;
          }
        }
      }
    }
    catch (Exception ex)
    {
      // GameMod font loading not available or failed
      System.Diagnostics.Debug.WriteLine($"GameMod font loading failed: {ex.Message}");
    }

    fontUsed = false;
  }

  /// <summary>
  /// Pops the font if it was pushed.
  /// </summary>
  private static void MaybePopFont(bool wasUsed)
  {
    if (wasUsed)
    {
      BrutalImGui.PopFont();
    }
  }


  /// <summary>
  /// Disposes the terminal controller and cleans up resources.
  /// </summary>
  public void Dispose()
  {
    if (!_disposed)
    {

      _disposed = true;
    }
  }
}