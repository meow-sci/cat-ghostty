using System.Diagnostics;
using System.Reflection;
using System.Text;
using Brutal.ImGuiApi;
using caTTY.Core.Terminal;
using caTTY.Core.Types;
using caTTY.Display.Configuration;
using KSA;
using float2 = Brutal.Numerics.float2;
using float4 = Brutal.Numerics.float4;

namespace caTTY.Display.Controllers;

/// <summary>
///     ImGui terminal controller that handles display and input for the terminal emulator.
///     This is the shared controller implementation that is used by both the TestApp and GameMod.
/// </summary>
public class TerminalController : ITerminalController
{
    private readonly TerminalRenderingConfig _config;
    private TerminalFontConfig _fontConfig;

    // Input handling
    private readonly StringBuilder _inputBuffer = new();
    private readonly IProcessManager _processManager;
    private readonly ITerminalEmulator _terminal;
    private bool _disposed;

    // Font pointers for different styles
    private ImFontPtr _regularFont;
    private ImFontPtr _boldFont;
    private ImFontPtr _italicFont;
    private ImFontPtr _boldItalicFont;

    // Font and rendering settings (now config-based)
    private bool _isVisible = true;

    /// <summary>
    ///     Creates a new terminal controller with default configuration.
    ///     This constructor maintains backward compatibility.
    /// </summary>
    /// <param name="terminal">The terminal emulator instance</param>
    /// <param name="processManager">The process manager instance</param>
    public TerminalController(ITerminalEmulator terminal, IProcessManager processManager)
        : this(terminal, processManager, DpiContextDetector.DetectAndCreateConfig(), FontContextDetector.DetectAndCreateConfig())
    {
    }

    /// <summary>
    ///     Creates a new terminal controller with the specified rendering configuration.
    ///     Uses automatic font detection for font configuration.
    /// </summary>
    /// <param name="terminal">The terminal emulator instance</param>
    /// <param name="processManager">The process manager instance</param>
    /// <param name="config">The rendering configuration to use</param>
    public TerminalController(ITerminalEmulator terminal, IProcessManager processManager,
        TerminalRenderingConfig config)
        : this(terminal, processManager, config, FontContextDetector.DetectAndCreateConfig())
    {
    }

    /// <summary>
    ///     Creates a new terminal controller with the specified font configuration.
    ///     Uses automatic DPI detection for rendering configuration.
    /// </summary>
    /// <param name="terminal">The terminal emulator instance</param>
    /// <param name="processManager">The process manager instance</param>
    /// <param name="fontConfig">The font configuration to use</param>
    public TerminalController(ITerminalEmulator terminal, IProcessManager processManager,
        TerminalFontConfig fontConfig)
        : this(terminal, processManager, DpiContextDetector.DetectAndCreateConfig(), fontConfig)
    {
    }

    /// <summary>
    ///     Creates a new terminal controller with the specified configurations.
    /// </summary>
    /// <param name="terminal">The terminal emulator instance</param>
    /// <param name="processManager">The process manager instance</param>
    /// <param name="config">The rendering configuration to use</param>
    /// <param name="fontConfig">The font configuration to use</param>
    public TerminalController(ITerminalEmulator terminal, IProcessManager processManager,
        TerminalRenderingConfig config, TerminalFontConfig fontConfig)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _fontConfig = fontConfig ?? throw new ArgumentNullException(nameof(fontConfig));

        // Validate configurations
        _config.Validate();
        _fontConfig.Validate();

        // Load fonts from ImGui font system
        LoadFonts();

        // Calculate character metrics from loaded fonts
        CalculateCharacterMetrics();

        // Apply configuration to rendering metrics
        CurrentFontSize = _fontConfig.FontSize;

        // Log configuration for debugging
        // LogConfiguration();
        // LogFontConfiguration();

        // Subscribe to terminal events
        _terminal.ScreenUpdated += OnScreenUpdated;
        _terminal.ResponseEmitted += OnResponseEmitted;
    }

    /// <summary>
    ///     Gets the current font size for debugging purposes.
    /// </summary>
    public float CurrentFontSize { get; private set; }

    /// <summary>
    ///     Gets the current character width for debugging purposes.
    /// </summary>
    public float CurrentCharacterWidth { get; private set; }

    /// <summary>
    ///     Gets the current line height for debugging purposes.
    /// </summary>
    public float CurrentLineHeight { get; private set; }

    /// <summary>
    ///     Gets the current DPI scaling factor for debugging purposes.
    /// </summary>
    public float CurrentDpiScalingFactor => _config.DpiScalingFactor;

    /// <summary>
    ///     Gets the current font configuration for debugging purposes.
    /// </summary>
    public TerminalFontConfig CurrentFontConfig => _fontConfig;

    /// <summary>
    ///     Gets the current regular font name for debugging purposes.
    /// </summary>
    public string CurrentRegularFontName => _fontConfig.RegularFontName;

    /// <summary>
    ///     Gets the current bold font name for debugging purposes.
    /// </summary>
    public string CurrentBoldFontName => _fontConfig.BoldFontName;

    /// <summary>
    ///     Gets the current italic font name for debugging purposes.
    /// </summary>
    public string CurrentItalicFontName => _fontConfig.ItalicFontName;

    /// <summary>
    ///     Gets the current bold+italic font name for debugging purposes.
    /// </summary>
    public string CurrentBoldItalicFontName => _fontConfig.BoldItalicFontName;

    /// <summary>
    ///     Gets or sets whether the terminal window is visible.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }

    /// <summary>
    ///     Gets whether the terminal window currently has focus.
    /// </summary>
    public bool HasFocus { get; private set; }

    /// <summary>
    ///     Event raised when user input should be sent to the process.
    /// </summary>
    public event EventHandler<DataInputEventArgs>? DataInput;

    /// <summary>
    ///     Renders the terminal window using ImGui.
    /// </summary>
    public void Render()
    {
        if (!_isVisible)
        {
            return;
        }

        // Push monospace font if available
        PushMonospaceFont(out bool fontUsed);

        try
        {
            // Create terminal window
            ImGui.Begin("Terminal", ref _isVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

            // Track focus state
            HasFocus = ImGui.IsWindowFocused();

            // Display terminal info
            ImGui.Text($"Terminal: {_terminal.Width}x{_terminal.Height}");
            ImGui.SameLine();
            ImGui.Text($"Cursor: ({_terminal.Cursor.Row}, {_terminal.Cursor.Col})");
            ImGui.SameLine();
            ImGui.Text(
                $"Process: {(_processManager.IsRunning ? $"Running (PID: {_processManager.ProcessId})" : "Stopped")}");

            if (_processManager.ExitCode.HasValue)
            {
                ImGui.SameLine();
                ImGui.Text($"Exit Code: {_processManager.ExitCode}");
            }

            ImGui.Separator();

            // Render terminal content
            RenderTerminalContent();

            // Handle input if focused
            if (HasFocus)
            {
                HandleInput();
            }

            ImGui.End();
        }
        finally
        {
            MaybePopFont(fontUsed);
        }
    }

    /// <summary>
    ///     Updates the terminal controller state.
    ///     This method can be used for time-based updates if needed.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds</param>
    public void Update(float deltaTime)
    {
        // Currently no time-based updates needed
        // This method is available for future enhancements like cursor blinking
    }

    /// <summary>
    ///     Disposes the terminal controller and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_terminal != null)
            {
                _terminal.ScreenUpdated -= OnScreenUpdated;
                _terminal.ResponseEmitted -= OnResponseEmitted;
            }

            _disposed = true;
        }
    }

    /// <summary>
    ///     Updates the rendering configuration at runtime.
    /// </summary>
    /// <param name="newConfig">The new configuration to apply</param>
    /// <exception cref="ArgumentNullException">Thrown when newConfig is null</exception>
    /// <exception cref="ArgumentException">Thrown when newConfig contains invalid values</exception>
    public void UpdateRenderingConfig(TerminalRenderingConfig newConfig)
    {
        if (newConfig == null)
        {
            throw new ArgumentNullException(nameof(newConfig));
        }

        // Validate the new configuration
        newConfig.Validate();

        // Apply the new metrics
        CurrentFontSize = newConfig.FontSize;
        CurrentCharacterWidth = newConfig.CharacterWidth;
        CurrentLineHeight = newConfig.LineHeight;

        // Log the configuration change
        // Console.WriteLine("TerminalController: Runtime configuration updated");
        // LogConfiguration();
    }

    /// <summary>
    ///     Updates the font configuration at runtime.
    /// </summary>
    /// <param name="newFontConfig">The new font configuration to apply</param>
    /// <exception cref="ArgumentNullException">Thrown when newFontConfig is null</exception>
    /// <exception cref="ArgumentException">Thrown when newFontConfig contains invalid values</exception>
    public void UpdateFontConfig(TerminalFontConfig newFontConfig)
    {
        if (newFontConfig == null)
        {
            throw new ArgumentNullException(nameof(newFontConfig));
        }

        // Validate the new configuration
        newFontConfig.Validate();

        // Update font configuration
        _fontConfig = newFontConfig;

        // Reload fonts from ImGui font system
        LoadFonts();

        // Recalculate character metrics based on new fonts
        CalculateCharacterMetrics();

        // Update font size
        CurrentFontSize = _fontConfig.FontSize;

        // Log the configuration change
        Console.WriteLine("TerminalController: Runtime font configuration updated");
        LogFontConfiguration();
    }

    /// <summary>
    ///     Loads fonts from the ImGui font system by name.
    /// </summary>
    private void LoadFonts()
    {
        try
        {
            // Try to find fonts by name, fall back to default if not found
            var defaultFont = ImGui.GetFont();
            
            var regularFont = FindFont(_fontConfig.RegularFontName);
            _regularFont = regularFont.HasValue ? regularFont.Value : defaultFont;
                
            var boldFont = FindFont(_fontConfig.BoldFontName);
            _boldFont = boldFont.HasValue ? boldFont.Value : _regularFont;
                
            var italicFont = FindFont(_fontConfig.ItalicFontName);
            _italicFont = italicFont.HasValue ? italicFont.Value : _regularFont;
                
            var boldItalicFont = FindFont(_fontConfig.BoldItalicFontName);
            _boldItalicFont = boldItalicFont.HasValue ? boldItalicFont.Value : _regularFont;

            Console.WriteLine("TerminalController: Fonts loaded successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TerminalController: Error loading fonts: {ex.Message}");
            
            // Fallback to default font for all styles
            var defaultFont = ImGui.GetFont();
            _regularFont = defaultFont;
            _boldFont = defaultFont;
            _italicFont = defaultFont;
            _boldItalicFont = defaultFont;
        }
    }

    /// <summary>
    ///     Finds a font by name in the ImGui font atlas.
    /// </summary>
    /// <param name="fontName">The name of the font to find</param>
    /// <returns>The font pointer if found, null otherwise</returns>
    private ImFontPtr? FindFont(string fontName)
    {
        if (string.IsNullOrWhiteSpace(fontName))
        {
            return null;
        }

        try
        {
            // First try the standard FontManager (works in standalone apps)
            if (FontManager.Fonts.TryGetValue(fontName, out ImFontPtr fontPtr))
            {
                return fontPtr;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TerminalController: FontManager.Fonts not available for '{fontName}': {ex.Message}");
        }

        try
        {
            // Try the GameMod's font loading system (works in game mod context)
            var gameModType = Type.GetType("caTTY.GameMod.TerminalMod, caTTY");
            if (gameModType != null)
            {
                MethodInfo? getFontMethod = gameModType.GetMethod("GetFont", BindingFlags.Public | BindingFlags.Static);
                if (getFontMethod != null)
                {
                    object? result = getFontMethod.Invoke(null, new object[] { fontName });
                    if (result is ImFontPtr font)
                    {
                        return font;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TerminalController: GameMod font loading failed for '{fontName}': {ex.Message}");
        }

        // Try to iterate through ImGui font atlas (fallback method)
        try
        {
            var io = ImGui.GetIO();
            var fonts = io.Fonts;
            
            // This is a simplified approach - in a real implementation,
            // we would need to iterate through the font atlas and match names
            // For now, return null to indicate font not found
            Console.WriteLine($"TerminalController: Font '{fontName}' not found in ImGui font atlas");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TerminalController: Error searching ImGui font atlas for '{fontName}': {ex.Message}");
        }

        return null;
    }

    /// <summary>
    ///     Calculates character metrics from the loaded fonts.
    /// </summary>
    private void CalculateCharacterMetrics()
    {
        try
        {
            // Use the regular font for metric calculations
            ImGui.PushFont(_regularFont, _fontConfig.FontSize);
            
            try
            {
                // Calculate character width using multiple test characters to ensure accuracy
                var testChars = new[] { 'M', 'W', '@', '#' }; // Wide characters for accurate measurement
                float maxWidth = 0.0f;
                
                foreach (char testChar in testChars)
                {
                    var textSize = ImGui.CalcTextSize(testChar.ToString());
                    maxWidth = Math.Max(maxWidth, textSize.X);
                }
                
                // Use the maximum width found to ensure all characters fit properly
                CurrentCharacterWidth = maxWidth;
                
                // Calculate line height using a standard character
                var lineSize = ImGui.CalcTextSize("M");
                CurrentLineHeight = lineSize.Y * 1.2f; // Add 20% line spacing for readability
                
                Console.WriteLine($"TerminalController: Calculated metrics from font - CharWidth: {CurrentCharacterWidth:F1}, LineHeight: {CurrentLineHeight:F1}");
            }
            finally
            {
                ImGui.PopFont();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TerminalController: Error calculating character metrics: {ex.Message}");
            
            // Fallback to DPI-based metrics from config
            CurrentCharacterWidth = _config.CharacterWidth;
            CurrentLineHeight = _config.LineHeight;
            
            Console.WriteLine($"TerminalController: Using fallback metrics from config - CharWidth: {CurrentCharacterWidth:F1}, LineHeight: {CurrentLineHeight:F1}");
        }
    }

    /// <summary>
    ///     Selects the appropriate font based on SGR attributes.
    /// </summary>
    /// <param name="attributes">The SGR attributes of the character</param>
    /// <returns>The appropriate font pointer for the attributes</returns>
    private ImFontPtr SelectFont(SgrAttributes attributes)
    {
        if (attributes.Bold && attributes.Italic)
            return _boldItalicFont;
        else if (attributes.Bold)
            return _boldFont;
        else if (attributes.Italic)
            return _italicFont;
        else
            return _regularFont;
    }

    /// <summary>
    ///     Logs the current configuration for debugging purposes.
    /// </summary>
    private void LogConfiguration()
    {
        try
        {
            Console.WriteLine(
                $"TerminalController: FontSize={CurrentFontSize:F1}, CharWidth={CurrentCharacterWidth:F1}, LineHeight={CurrentLineHeight:F1}, DpiScale={_config.DpiScalingFactor:F1}");
        }
        catch (Exception ex)
        {
            // Ignore logging failures to prevent crashes
            Debug.WriteLine($"Failed to log configuration: {ex.Message}");
        }
    }

    /// <summary>
    ///     Logs the current font configuration for debugging purposes.
    /// </summary>
    private void LogFontConfiguration()
    {
        try
        {
            Console.WriteLine($"TerminalController Font Config:");
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
    ///     Renders the terminal screen content.
    /// </summary>
    private void RenderTerminalContent()
    {
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        float2 windowPos = ImGui.GetCursorScreenPos();

        // Calculate terminal area
        float terminalWidth = _terminal.Width * CurrentCharacterWidth;
        float terminalHeight = _terminal.Height * CurrentLineHeight;

        // Draw terminal background
        uint bgColor = ImGui.ColorConvertFloat4ToU32(new float4(0.0f, 0.0f, 0.0f, 1.0f));
        var terminalRect = new float2(windowPos.X + terminalWidth, windowPos.Y + terminalHeight);
        drawList.AddRectFilled(windowPos, terminalRect, bgColor);

        // Render each cell
        for (int row = 0; row < _terminal.Height; row++)
        {
            for (int col = 0; col < _terminal.Width; col++)
            {
                Cell cell = _terminal.ScreenBuffer.GetCell(row, col);
                RenderCell(drawList, windowPos, row, col, cell);
            }
        }

        // Render cursor
        RenderCursor(drawList, windowPos);

        // Reserve space for the terminal
        ImGui.Dummy(new float2(terminalWidth, terminalHeight));
    }

    /// <summary>
    ///     Renders a single terminal cell.
    /// </summary>
    private void RenderCell(ImDrawListPtr drawList, float2 windowPos, int row, int col, Cell cell)
    {
        float x = windowPos.X + (col * CurrentCharacterWidth);
        float y = windowPos.Y + (row * CurrentLineHeight);
        var pos = new float2(x, y);

        // Get colors with proper defaults
        float4 bgColor = ConvertColor(cell.Attributes.BackgroundColor, true);
        float4 fgColor = ConvertColor(cell.Attributes.ForegroundColor);

        // Always draw background (black by default)
        var bgRect = new float2(x + CurrentCharacterWidth, y + CurrentLineHeight);
        drawList.AddRectFilled(pos, bgRect, ImGui.ColorConvertFloat4ToU32(bgColor));

        // Draw character if not space or null
        if (cell.Character != ' ' && cell.Character != '\0')
        {
            // Select appropriate font based on SGR attributes
            var font = SelectFont(cell.Attributes);
            
            // Apply text styling
            if (cell.Attributes.Bold)
            {
                // Make color brighter for bold
                fgColor = new float4(
                    Math.Min(1.0f, fgColor.X * 1.3f),
                    Math.Min(1.0f, fgColor.Y * 1.3f),
                    Math.Min(1.0f, fgColor.Z * 1.3f),
                    fgColor.W
                );
            }

            if (cell.Attributes.Faint)
            {
                // Make color dimmer
                fgColor = new float4(fgColor.X * 0.7f, fgColor.Y * 0.7f, fgColor.Z * 0.7f, fgColor.W);
            }

            if (cell.Attributes.Inverse)
            {
                // Swap foreground and background
                float4 temp = fgColor;
                fgColor = bgColor;
                bgColor = temp;

                // Redraw background with swapped color
                drawList.AddRectFilled(pos, bgRect, ImGui.ColorConvertFloat4ToU32(bgColor));
            }

            // Draw the character with selected font using proper PushFont/PopFont pattern
            ImGui.PushFont(font, _fontConfig.FontSize);
            try
            {
                drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(fgColor), cell.Character.ToString());
            }
            finally
            {
                ImGui.PopFont();
            }

            // Draw underline if needed
            if (cell.Attributes.Underline)
            {
                float underlineY = y + CurrentLineHeight - 2;
                var underlineStart = new float2(x, underlineY);
                var underlineEnd = new float2(x + CurrentCharacterWidth, underlineY);
                drawList.AddLine(underlineStart, underlineEnd, ImGui.ColorConvertFloat4ToU32(fgColor));
            }

            // Draw strikethrough if needed
            if (cell.Attributes.Strikethrough)
            {
                float strikeY = y + (CurrentLineHeight / 2);
                var strikeStart = new float2(x, strikeY);
                var strikeEnd = new float2(x + CurrentCharacterWidth, strikeY);
                drawList.AddLine(strikeStart, strikeEnd, ImGui.ColorConvertFloat4ToU32(fgColor));
            }
        }
    }

    /// <summary>
    ///     Renders the terminal cursor.
    /// </summary>
    private void RenderCursor(ImDrawListPtr drawList, float2 windowPos)
    {
        if (!((TerminalEmulator)_terminal).State.CursorVisible)
        {
            return;
        }

        ICursor cursor = _terminal.Cursor;

        // Ensure cursor position is within bounds
        int cursorCol = Math.Max(0, Math.Min(cursor.Col, _terminal.Width - 1));
        int cursorRow = Math.Max(0, Math.Min(cursor.Row, _terminal.Height - 1));

        float x = windowPos.X + (cursorCol * CurrentCharacterWidth);
        float y = windowPos.Y + (cursorRow * CurrentLineHeight);

        // Draw cursor as a filled rectangle (white with some transparency)
        uint cursorColor = ImGui.ColorConvertFloat4ToU32(new float4(1.0f, 1.0f, 1.0f, 0.8f));
        var cursorPos = new float2(x, y);
        var cursorRect = new float2(x + CurrentCharacterWidth, y + CurrentLineHeight);

        drawList.AddRectFilled(cursorPos, cursorRect, cursorColor);
    }

    /// <summary>
    ///     Handles keyboard input when the terminal has focus.
    /// </summary>
    private void HandleInput()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        // Handle text input
        if (io.InputQueueCharacters.Count > 0)
        {
            for (int i = 0; i < io.InputQueueCharacters.Count; i++)
            {
                char ch = (char)io.InputQueueCharacters[i];
                if (ch >= 32 && ch < 127) // Printable ASCII
                {
                    SendToProcess(ch.ToString());
                }
            }
        }

        // Handle special keys
        if (ImGui.IsKeyPressed(ImGuiKey.Enter))
        {
            SendToProcess("\r\n");
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.Backspace))
        {
            SendToProcess("\b");
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.Tab))
        {
            SendToProcess("\t");
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
        {
            SendToProcess("\x1b[A");
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
        {
            SendToProcess("\x1b[B");
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.RightArrow))
        {
            SendToProcess("\x1b[C");
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow))
        {
            SendToProcess("\x1b[D");
        }

        // Handle Ctrl combinations
        if (io.KeyCtrl)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.C))
            {
                SendToProcess("\x03"); // Ctrl+C
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.D))
            {
                SendToProcess("\x04"); // Ctrl+D
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.Z))
            {
                SendToProcess("\x1a"); // Ctrl+Z
            }
        }
    }

    /// <summary>
    ///     Sends text to the shell process.
    /// </summary>
    private void SendToProcess(string text)
    {
        if (_processManager.IsRunning)
        {
            try
            {
                _processManager.Write(text);

                // Also raise the DataInput event for external subscribers
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                DataInput?.Invoke(this, new DataInputEventArgs(text, bytes));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send input to process: {ex.Message}");
            }
        }
    }

    /// <summary>
    ///     Converts a terminal color to ImGui float4 color.
    /// </summary>
    private static float4 ConvertColor(Color? color, bool isBackground = false)
    {
        if (!color.HasValue)
        {
            // Use proper terminal defaults: white text on black background
            return isBackground
                ? new float4(0.0f, 0.0f, 0.0f, 1.0f) // Black background
                : new float4(1.0f, 1.0f, 1.0f, 1.0f); // White foreground
        }

        return color.Value.Type switch
        {
            ColorType.Named => ConvertNamedColor(color.Value.NamedColor),
            ColorType.Indexed => ConvertIndexedColor(color.Value.Index),
            ColorType.Rgb => new float4(color.Value.Red / 255.0f, color.Value.Green / 255.0f, color.Value.Blue / 255.0f,
                1.0f),
            _ => isBackground
                ? new float4(0.0f, 0.0f, 0.0f, 1.0f) // Black background
                : new float4(1.0f, 1.0f, 1.0f, 1.0f) // White foreground
        };
    }

    /// <summary>
    ///     Converts a named color to ImGui float4 color.
    /// </summary>
    private static float4 ConvertNamedColor(NamedColor namedColor)
    {
        return namedColor switch
        {
            NamedColor.Black => new float4(0.0f, 0.0f, 0.0f, 1.0f),
            NamedColor.Red => new float4(0.8f, 0.0f, 0.0f, 1.0f),
            NamedColor.Green => new float4(0.0f, 0.8f, 0.0f, 1.0f),
            NamedColor.Yellow => new float4(0.8f, 0.8f, 0.0f, 1.0f),
            NamedColor.Blue => new float4(0.0f, 0.0f, 0.8f, 1.0f),
            NamedColor.Magenta => new float4(0.8f, 0.0f, 0.8f, 1.0f),
            NamedColor.Cyan => new float4(0.0f, 0.8f, 0.8f, 1.0f),
            NamedColor.White => new float4(0.8f, 0.8f, 0.8f, 1.0f),
            NamedColor.BrightBlack => new float4(0.4f, 0.4f, 0.4f, 1.0f),
            NamedColor.BrightRed => new float4(1.0f, 0.4f, 0.4f, 1.0f),
            NamedColor.BrightGreen => new float4(0.4f, 1.0f, 0.4f, 1.0f),
            NamedColor.BrightYellow => new float4(1.0f, 1.0f, 0.4f, 1.0f),
            NamedColor.BrightBlue => new float4(0.4f, 0.4f, 1.0f, 1.0f),
            NamedColor.BrightMagenta => new float4(1.0f, 0.4f, 1.0f, 1.0f),
            NamedColor.BrightCyan => new float4(0.4f, 1.0f, 1.0f, 1.0f),
            NamedColor.BrightWhite => new float4(1.0f, 1.0f, 1.0f, 1.0f),
            _ => new float4(0.8f, 0.8f, 0.8f, 1.0f)
        };
    }

    /// <summary>
    ///     Converts an indexed color to ImGui float4 color.
    /// </summary>
    private static float4 ConvertIndexedColor(byte index)
    {
        // Standard 16-color palette
        return index switch
        {
            0 => new float4(0.0f, 0.0f, 0.0f, 1.0f), // Black
            1 => new float4(0.8f, 0.0f, 0.0f, 1.0f), // Red
            2 => new float4(0.0f, 0.8f, 0.0f, 1.0f), // Green
            3 => new float4(0.8f, 0.8f, 0.0f, 1.0f), // Yellow
            4 => new float4(0.0f, 0.0f, 0.8f, 1.0f), // Blue
            5 => new float4(0.8f, 0.0f, 0.8f, 1.0f), // Magenta
            6 => new float4(0.0f, 0.8f, 0.8f, 1.0f), // Cyan
            7 => new float4(0.8f, 0.8f, 0.8f, 1.0f), // White
            8 => new float4(0.4f, 0.4f, 0.4f, 1.0f), // Bright Black
            9 => new float4(1.0f, 0.4f, 0.4f, 1.0f), // Bright Red
            10 => new float4(0.4f, 1.0f, 0.4f, 1.0f), // Bright Green
            11 => new float4(1.0f, 1.0f, 0.4f, 1.0f), // Bright Yellow
            12 => new float4(0.4f, 0.4f, 1.0f, 1.0f), // Bright Blue
            13 => new float4(1.0f, 0.4f, 1.0f, 1.0f), // Bright Magenta
            14 => new float4(0.4f, 1.0f, 1.0f, 1.0f), // Bright Cyan
            15 => new float4(1.0f, 1.0f, 1.0f, 1.0f), // Bright White
            _ => new float4(0.8f, 0.8f, 0.8f, 1.0f) // Default
        };
    }

    /// <summary>
    ///     Pushes a monospace font if available.
    /// </summary>
    private void PushMonospaceFont(out bool fontUsed)
    {
        try
        {
            // Use the regular font from our font configuration
            ImGui.PushFont(_regularFont, _fontConfig.FontSize);
            fontUsed = true;
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TerminalController: Error pushing configured font: {ex.Message}");
        }

        // Fallback: First try the standard FontManager (works in standalone apps)
        try
        {
            if (FontManager.Fonts.TryGetValue(_fontConfig.RegularFontName, out ImFontPtr fontPtr))
            {
                ImGui.PushFont(fontPtr, _fontConfig.FontSize);
                fontUsed = true;
                return;
            }
        }
        catch (Exception ex)
        {
            // FontManager.Fonts may not be available in game mod context
            Console.WriteLine($"FontManager.Fonts not available: {ex.Message}");
        }

        // Try the GameMod's font loading system (works in game mod context)
        try
        {
            // Use reflection to call the GameMod's GetFont method
            var gameModType = Type.GetType("caTTY.GameMod.TerminalMod, caTTY");
            if (gameModType != null)
            {
                MethodInfo? getFontMethod = gameModType.GetMethod("GetFont", BindingFlags.Public | BindingFlags.Static);
                if (getFontMethod != null)
                {
                    object? result = getFontMethod.Invoke(null, new object[] { _fontConfig.RegularFontName });
                    if (result is ImFontPtr font)
                    {
                        ImGui.PushFont(font, _fontConfig.FontSize);
                        fontUsed = true;
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // GameMod font loading not available or failed
            Console.WriteLine($"GameMod font loading failed: {ex.Message}");
        }

        fontUsed = false;
    }

    /// <summary>
    ///     Pops the font if it was pushed.
    /// </summary>
    private static void MaybePopFont(bool wasUsed)
    {
        if (wasUsed)
        {
            ImGui.PopFont();
        }
    }

    /// <summary>
    ///     Handles screen updated events from the terminal.
    /// </summary>
    private void OnScreenUpdated(object? sender, ScreenUpdatedEventArgs e)
    {
        // Screen will be redrawn on next frame
    }

    /// <summary>
    ///     Handles response emitted events from the terminal.
    /// </summary>
    private void OnResponseEmitted(object? sender, ResponseEmittedEventArgs e)
    {
        // Send terminal responses back to the process
        if (_processManager.IsRunning)
        {
            try
            {
                _processManager.Write(e.ResponseData.Span);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send terminal response to process: {ex.Message}");
            }
        }
    }
}
