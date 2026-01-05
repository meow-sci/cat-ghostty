using System;
using System.Diagnostics;
using System.Reflection;
using Brutal.ImGuiApi;
using caTTY.Display.Configuration;
using caTTY.Display.Controllers.TerminalUi.Fonts;
using caTTY.Display.Rendering;
using caTTY.Display.Utils;
using KSA;

namespace caTTY.Display.Controllers.TerminalUi;

/// <summary>
///     Handles font loading, metrics calculation, and font size adjustments for the terminal UI.
/// </summary>
internal class TerminalUiFonts
{
  private readonly TerminalRenderingConfig _config;
  private TerminalFontConfig _fontConfig;
  private string _currentFontFamily;

  // Font loader handles font discovery and loading
  private FontLoader _fontLoader;

  // Font metrics calculator handles character width/height calculations
  private FontMetricsCalculator _metricsCalculator;

  // Font loading state
  private bool _fontsLoaded = false;

  // Current metrics (delegated to FontMetricsCalculator)
  public float CurrentCharacterWidth => _metricsCalculator.CurrentCharacterWidth;
  public float CurrentLineHeight => _metricsCalculator.CurrentLineHeight;
  public float CurrentFontSize { get; private set; }

  public TerminalUiFonts(TerminalRenderingConfig config, TerminalFontConfig fontConfig, string currentFontFamily)
  {
    _config = config ?? throw new ArgumentNullException(nameof(config));
    _fontConfig = fontConfig ?? throw new ArgumentNullException(nameof(fontConfig));
    _currentFontFamily = currentFontFamily ?? throw new ArgumentNullException(nameof(currentFontFamily));

    // Initialize font loader
    _fontLoader = new FontLoader(_fontConfig);

    // Initialize metrics calculator
    _metricsCalculator = new FontMetricsCalculator(_config);

    CurrentFontSize = _fontConfig.FontSize;
  }

  /// <summary>
  ///     Gets the current font configuration.
  /// </summary>
  public TerminalFontConfig CurrentFontConfig => _fontConfig;

  /// <summary>
  ///     Gets the current regular font name.
  /// </summary>
  public string CurrentRegularFontName => _fontConfig.RegularFontName;

  /// <summary>
  ///     Gets the current bold font name.
  /// </summary>
  public string CurrentBoldFontName => _fontConfig.BoldFontName;

  /// <summary>
  ///     Gets the current italic font name.
  /// </summary>
  public string CurrentItalicFontName => _fontConfig.ItalicFontName;

  /// <summary>
  ///     Gets the current bold+italic font name.
  /// </summary>
  public string CurrentBoldItalicFontName => _fontConfig.BoldItalicFontName;

  /// <summary>
  ///     Gets the current font family.
  /// </summary>
  public string CurrentFontFamily => _currentFontFamily;

  /// <summary>
  ///     Ensures fonts are loaded before rendering.
  /// </summary>
  public void EnsureFontsLoaded()
  {
    if (_fontsLoaded)
    {
      return;
    }

    try
    {
      Console.WriteLine("TerminalUiFonts: Performing deferred font loading...");

      // Load fonts from ImGui font system
      LoadFonts();

      // Calculate character metrics from loaded fonts
      CalculateCharacterMetrics();

      // Log configuration for debugging
      LogFontConfiguration();

      _fontsLoaded = true;
      Console.WriteLine("TerminalUiFonts: Deferred font loading completed successfully");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalUiFonts: Error during deferred font loading: {ex.Message}");

      // Set fallback values to prevent crashes (metrics calculator uses defaults from config)
      _metricsCalculator.ResetToDefaults();

      // Mark as loaded to prevent repeated attempts
      _fontsLoaded = true;
    }
  }

  /// <summary>
  ///     Loads fonts from the ImGui font system by name.
  /// </summary>
  private void LoadFonts()
  {
    // Delegate to font loader
    _fontLoader.LoadFonts();
  }


  /// <summary>
  ///     Calculates character metrics from the loaded fonts.
  /// </summary>
  private void CalculateCharacterMetrics()
  {
    // Delegate to FontMetricsCalculator
    _metricsCalculator.CalculateCharacterMetrics(_fontLoader.RegularFont, _fontConfig.FontSize);
  }

  /// <summary>
  ///     Selects the appropriate font based on SGR attributes.
  /// </summary>
  /// <param name="attributes">The SGR attributes of the character</param>
  /// <returns>The appropriate font pointer for the attributes</returns>
  public ImFontPtr SelectFont(Core.Types.SgrAttributes attributes)
  {
    if (attributes.Bold && attributes.Italic)
      return _fontLoader.BoldItalicFont;
    else if (attributes.Bold)
      return _fontLoader.BoldFont;
    else if (attributes.Italic)
      return _fontLoader.ItalicFont;
    else
      return _fontLoader.RegularFont;
  }

  /// <summary>
  ///     Logs the current font configuration for debugging purposes.
  /// </summary>
  private void LogFontConfiguration()
  {
    try
    {
      Console.WriteLine($"TerminalUiFonts Font Config:");
      Console.WriteLine($"  Regular: {_fontConfig.RegularFontName}");
      Console.WriteLine($"  Bold: {_fontConfig.BoldFontName}");
      Console.WriteLine($"  Italic: {_fontConfig.ItalicFontName}");
      Console.WriteLine($"  BoldItalic: {_fontConfig.BoldItalicFontName}");
      Console.WriteLine($"  FontSize: {_fontConfig.FontSize}");
      Console.WriteLine($"  AutoDetectContext: {_fontConfig.AutoDetectContext}");
      Console.WriteLine($"  Calculated CharWidth: {CurrentCharacterWidth:F1}, LineHeight: {CurrentLineHeight:F1}");
    }
    catch (Exception ex)
    {
      // Ignore logging failures to prevent crashes
      Debug.WriteLine($"Failed to log font configuration: {ex.Message}");
    }
  }

  /// <summary>
  ///     Pushes a UI font for menu rendering.
  /// </summary>
  public void PushUIFont(out bool fontUsed)
  {
    // Delegate to font loader
    _fontLoader.PushUIFont(out fontUsed);
  }

  /// <summary>
  ///     Pushes a monospace font for terminal content rendering.
  /// </summary>
  public void PushTerminalContentFont(out bool fontUsed)
  {
    // Delegate to font loader
    _fontLoader.PushTerminalContentFont(out fontUsed);
  }

  /// <summary>
  ///     Pushes a monospace font if available.
  ///     DEPRECATED: Use PushUIFont() for UI elements or PushTerminalContentFont() for terminal content.
  /// </summary>
  public void PushMonospaceFont(out bool fontUsed)
  {
    // For backward compatibility, delegate to terminal content font
    PushTerminalContentFont(out fontUsed);
  }

  /// <summary>
  ///     Pops the font if it was pushed.
  /// </summary>
  public static void MaybePopFont(bool wasUsed)
  {
    if (wasUsed)
    {
      ImGui.PopFont();
    }
  }

  /// <summary>
  ///     Increases the font size by 1.0f.
  /// </summary>
  public void IncreaseFontSize(Action onFontChanged)
  {
    try
    {
      var newFontConfig = new TerminalFontConfig
      {
        FontSize = Math.Min(LayoutConstants.MAX_FONT_SIZE, _fontConfig.FontSize + 1.0f),
        RegularFontName = _fontConfig.RegularFontName,
        BoldFontName = _fontConfig.BoldFontName,
        ItalicFontName = _fontConfig.ItalicFontName,
        BoldItalicFontName = _fontConfig.BoldItalicFontName,
        AutoDetectContext = _fontConfig.AutoDetectContext
      };
      UpdateFontConfig(newFontConfig, onFontChanged);

      Console.WriteLine($"TerminalUiFonts: Font size increased to {newFontConfig.FontSize}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalUiFonts: Error increasing font size: {ex.Message}");
    }
  }

  /// <summary>
  ///     Decreases the font size by 1.0f.
  /// </summary>
  public void DecreaseFontSize(Action onFontChanged)
  {
    try
    {
      var newFontConfig = new TerminalFontConfig
      {
        FontSize = Math.Max(LayoutConstants.MIN_FONT_SIZE, _fontConfig.FontSize - 1.0f),
        RegularFontName = _fontConfig.RegularFontName,
        BoldFontName = _fontConfig.BoldFontName,
        ItalicFontName = _fontConfig.ItalicFontName,
        BoldItalicFontName = _fontConfig.BoldItalicFontName,
        AutoDetectContext = _fontConfig.AutoDetectContext
      };
      UpdateFontConfig(newFontConfig, onFontChanged);

      Console.WriteLine($"TerminalUiFonts: Font size decreased to {newFontConfig.FontSize}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalUiFonts: Error decreasing font size: {ex.Message}");
    }
  }

  /// <summary>
  ///     Updates the font configuration.
  /// </summary>
  public void UpdateFontConfig(TerminalFontConfig newFontConfig, Action onFontChanged)
  {
    if (newFontConfig == null)
    {
      throw new ArgumentNullException(nameof(newFontConfig));
    }

    try
    {
      // Validate the new configuration before applying any changes
      newFontConfig.Validate();

      // Store previous metrics for comparison logging
      float previousCharWidth = CurrentCharacterWidth;
      float previousLineHeight = CurrentLineHeight;
      float previousFontSize = CurrentFontSize;
      string previousRegularFont = _fontConfig.RegularFontName;

      // Log the configuration change attempt
      Console.WriteLine("TerminalUiFonts: Attempting runtime font configuration update");
      Console.WriteLine($"  Previous: Font={previousRegularFont}, Size={previousFontSize:F1}, CharWidth={previousCharWidth:F1}, LineHeight={previousLineHeight:F1}");
      Console.WriteLine($"  New: Font={newFontConfig.RegularFontName}, Size={newFontConfig.FontSize:F1}");

      // Update font configuration
      _fontConfig = newFontConfig;

      // Create new font loader with updated configuration
      _fontLoader = new FontLoader(_fontConfig);

      // Metrics calculator doesn't need to be recreated (it uses immutable config reference)
      // Just recalculate metrics after loading new fonts

      // Reset font loading state to trigger reload
      _fontsLoaded = false;

      // Reload fonts from ImGui font system immediately
      LoadFonts();

      // Recalculate character metrics based on new fonts immediately
      CalculateCharacterMetrics();

      // Update font size immediately
      CurrentFontSize = _fontConfig.FontSize;

      // Notify caller that font changed
      onFontChanged?.Invoke();

      // Log successful configuration change with detailed metrics
      Console.WriteLine("TerminalUiFonts: Runtime font configuration updated successfully");
      Console.WriteLine($"  Applied: Font={_fontConfig.RegularFontName}, Size={CurrentFontSize:F1}, CharWidth={CurrentCharacterWidth:F1}, LineHeight={CurrentLineHeight:F1}");
      Console.WriteLine($"  Metrics change: CharWidth {previousCharWidth:F1} -> {CurrentCharacterWidth:F1} ({(CurrentCharacterWidth - previousCharWidth):+F1;-F1;0})");
      Console.WriteLine($"  Metrics change: LineHeight {previousLineHeight:F1} -> {CurrentLineHeight:F1} ({(CurrentLineHeight - previousLineHeight):+F1;-F1;0})");

      // Log detailed font configuration for debugging
      LogFontConfiguration();
    }
    catch (ArgumentException ex)
    {
      // Log validation failure and re-throw
      Console.WriteLine($"TerminalUiFonts: Font configuration validation failed: {ex.Message}");
      throw;
    }
    catch (Exception ex)
    {
      // Log unexpected error and re-throw
      Console.WriteLine($"TerminalUiFonts: Error updating font configuration: {ex.Message}");
      throw;
    }
  }

  /// <summary>
  ///     Selects and applies a new font family.
  /// </summary>
  public void SelectFontFamily(string displayName, Action onFontChanged)
  {
    try
    {
      Console.WriteLine($"TerminalUiFonts: Selecting font family: {displayName}");

      // Create new font configuration for the selected family
      var newFontConfig = CaTTYFontManager.CreateFontConfigForFamily(displayName, _fontConfig.FontSize);

      // Validate the new configuration
      newFontConfig.Validate();

      // Apply the new configuration immediately
      UpdateFontConfig(newFontConfig, onFontChanged);

      // Update current selection
      _currentFontFamily = displayName;

      Console.WriteLine($"TerminalUiFonts: Successfully switched to font family: {displayName}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalUiFonts: Failed to select font family {displayName}: {ex.Message}");
      // Keep current font on error - no changes to _currentFontFamily or _fontConfig
    }
  }

  /// <summary>
  ///     Loads font settings from persistent configuration during initialization.
  /// </summary>
  public void LoadFontSettingsInConstructor()
  {
    try
    {
      var config = ThemeConfiguration.Load();

      // Console.WriteLine($"TerminalUiFonts: Constructor - Loaded config FontFamily: '{config.FontFamily}', FontSize: {config.FontSize}");

      // Apply saved font family if available
      if (!string.IsNullOrEmpty(config.FontFamily))
      {
        try
        {
          // Console.WriteLine($"TerminalUiFonts: Constructor - Attempting to create font config for family: '{config.FontFamily}'");

          // Create font configuration manually since CaTTYFontManager.CreateFontConfigForFamily is broken
          var savedFontConfig = CaTTYFontManager.CreateFontConfigForFamily(config.FontFamily, config.FontSize ?? _fontConfig.FontSize);

          if (savedFontConfig != null)
          {
            // Console.WriteLine($"TerminalUiFonts: Constructor - Successfully created font config");
            // Console.WriteLine($"TerminalUiFonts: Constructor - Regular: {savedFontConfig.RegularFontName}");
            // Console.WriteLine($"TerminalUiFonts: Constructor - Bold: {savedFontConfig.BoldFontName}");
            // Console.WriteLine($"TerminalUiFonts: Constructor - Size: {savedFontConfig.FontSize}");

            var oldRegular = _fontConfig.RegularFontName;

            _fontConfig = savedFontConfig;
            _fontLoader = new FontLoader(_fontConfig);
            // Metrics calculator doesn't need to be recreated (it uses immutable config reference)
            _currentFontFamily = config.FontFamily;

            // Console.WriteLine($"TerminalUiFonts: Constructor - Font config updated from '{oldRegular}' to '{_fontConfig.RegularFontName}'");
            // Console.WriteLine($"TerminalUiFonts: Constructor - Current font family set to: '{_currentFontFamily}'");
          }
          else
          {
            // Console.WriteLine($"TerminalUiFonts: Constructor - Could not create font config for '{config.FontFamily}', keeping default");
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"TerminalUiFonts: Constructor - FAILED to load saved font family '{config.FontFamily}': {ex.Message}");

          // Keep current font configuration on error
        }
      }
      else
      {
        // Console.WriteLine("TerminalUiFonts: Constructor - No saved font family found in config");
      }

      // Apply saved font size if available
      if (config.FontSize.HasValue)
      {
        var fontSize = Math.Max(LayoutConstants.MIN_FONT_SIZE, Math.Min(LayoutConstants.MAX_FONT_SIZE, config.FontSize.Value));
        var oldSize = _fontConfig.FontSize;
        _fontConfig.FontSize = fontSize;
        // Console.WriteLine($"TerminalUiFonts: Constructor - Font size updated from {oldSize} to {fontSize}");
      }
      else
      {
        // Console.WriteLine("TerminalUiFonts: Constructor - No saved font size found in config");
      }

      // Console.WriteLine($"TerminalUiFonts: Constructor - Final font config: Regular='{_fontConfig.RegularFontName}', Size={_fontConfig.FontSize}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalUiFonts: Constructor - ERROR loading font settings: {ex.Message}");
    }
  }

  /// <summary>
  ///     Initializes the current font family from the font configuration.
  /// </summary>
  public void InitializeCurrentFontFamily()
  {
    try
    {
      // Font settings were already loaded in LoadFontSettingsInConstructor()
      // Just ensure _currentFontFamily is set correctly
      if (string.IsNullOrEmpty(_currentFontFamily))
      {
        // Determine current font family from configuration using CaTTYFontManager
        var detectedFamily = CaTTYFontManager.GetCurrentFontFamily(_fontConfig);
        _currentFontFamily = detectedFamily ?? "Hack"; // Default fallback
        // Console.WriteLine($"TerminalUiFonts: Detected font family from config: {_currentFontFamily}");
      }
      else
      {
        // Console.WriteLine($"TerminalUiFonts: Using font family from constructor loading: {_currentFontFamily}");
      }

      // Console.WriteLine($"TerminalUiFonts: Final initialization - Font family: {_currentFontFamily}, Regular font: {_fontConfig.RegularFontName}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalUiFonts: Error initializing current font family: {ex.Message}");
      _currentFontFamily = "Hack"; // Safe fallback
    }
  }

  /// <summary>
  ///     Loads font settings from persistent configuration.
  /// </summary>
  public void LoadFontSettings(Action onFontChanged)
  {
    try
    {
      // Load fresh configuration from disk to get latest saved values
      var config = ThemeConfiguration.Load();

      bool fontConfigChanged = false;

      // Apply saved font family if available
      if (!string.IsNullOrEmpty(config.FontFamily))
      {
        try
        {
          // Create font configuration for the saved family
          var savedFontConfig = CaTTYFontManager.CreateFontConfigForFamily(config.FontFamily, config.FontSize ?? _fontConfig.FontSize);

          // Log what we're trying to load vs what we got
          Console.WriteLine($"TerminalUiFonts: Attempting to load font family '{config.FontFamily}'");
          Console.WriteLine($"TerminalUiFonts: Created font config - Regular: {savedFontConfig.RegularFontName}");

          _fontConfig = savedFontConfig;
          _fontLoader = new FontLoader(_fontConfig);
          // Metrics calculator doesn't need to be recreated (it uses immutable config reference)
          _currentFontFamily = config.FontFamily;
          fontConfigChanged = true;
          Console.WriteLine($"TerminalUiFonts: Successfully loaded font family from settings: {config.FontFamily}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"TerminalUiFonts: Failed to load saved font family '{config.FontFamily}': {ex.Message}");
          // Keep current font configuration on error
        }
      }
      else
      {
        Console.WriteLine("TerminalUiFonts: No saved font family found, using default");
      }

      // Apply saved font size if available
      if (config.FontSize.HasValue)
      {
        var fontSize = Math.Max(LayoutConstants.MIN_FONT_SIZE, Math.Min(LayoutConstants.MAX_FONT_SIZE, config.FontSize.Value));
        if (Math.Abs(_fontConfig.FontSize - fontSize) > 0.1f)
        {
          _fontConfig.FontSize = fontSize;
          fontConfigChanged = true;
          Console.WriteLine($"TerminalUiFonts: Loaded font size from settings: {fontSize}");
        }
      }
      else
      {
        Console.WriteLine("TerminalUiFonts: No saved font size found, using default");
      }

      // If font configuration changed, reset font loading state to force reload
      if (fontConfigChanged)
      {
        _fontsLoaded = false;
        Console.WriteLine("TerminalUiFonts: Font configuration changed, fonts will be reloaded on next render");
        Console.WriteLine($"TerminalUiFonts: Final font config after loading - Regular: {_fontConfig.RegularFontName}, Size: {_fontConfig.FontSize}");

        // Notify caller that font changed
        onFontChanged?.Invoke();
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalUiFonts: Error loading font settings: {ex.Message}");
    }
  }

  /// <summary>
  ///     Saves current font settings to persistent configuration.
  /// </summary>
  public void SaveFontSettings()
  {
    try
    {
      var config = ThemeConfiguration.Load();

      // Update font settings
      config.FontFamily = _currentFontFamily;
      config.FontSize = _fontConfig.FontSize;

      // Save configuration
      config.Save();

      Console.WriteLine($"TerminalUiFonts: Saved font settings - Family: {_currentFontFamily}, Size: {_fontConfig.FontSize}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalUiFonts: Error saving font settings: {ex.Message}");
    }
  }
}
