using System;
using System.Collections.Generic;
using System.Numerics;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using KSA;
using ImGui = Brutal.ImGuiApi.ImGui;
using float2 = Brutal.Numerics.float2;
using float4 = Brutal.Numerics.float4;

namespace caTTY.Playground.Experiments;

/// <summary>
/// Text styling experiments for testing different ImGui text attribute approaches.
/// This class implements the core experiments for task 1.6.
/// </summary>
public static class TextStylingExperiments
{
    // Experiment state
    private static int _selectedExperiment = 0;
    private static readonly string[] _experimentNames = [
        "Text Attributes Test",
        "Font Style Variations",
        "Cursor Display Techniques",
        "Interactive Styling Controls",
        "Styling Limitations Analysis"
    ];

    // Text styling state
    private static bool _boldEnabled = false;
    private static bool _italicEnabled = false;
    private static bool _underlineEnabled = false;
    private static bool _strikethroughEnabled = false;
    private static bool _inverseEnabled = false;
    private static bool _dimEnabled = false;
    private static bool _blinkEnabled = false;

    // Cursor state
    private static int _cursorType = 0; // 0=block, 1=underline, 2=beam
    private static bool _cursorVisible = true;
    private static bool _cursorBlinking = false;
    private static DateTime _lastBlinkTime = DateTime.Now;
    private static bool _blinkState = true;

    // Color state
    private static int _foregroundColorIndex = 0;
    private static int _backgroundColorIndex = 7; // Transparent by default
    private static readonly float4[] _colorPalette = [
        new(1.0f, 1.0f, 1.0f, 1.0f), // 0: White
        new(1.0f, 0.0f, 0.0f, 1.0f), // 1: Red
        new(0.0f, 1.0f, 0.0f, 1.0f), // 2: Green
        new(0.0f, 0.0f, 1.0f, 1.0f), // 3: Blue
        new(1.0f, 1.0f, 0.0f, 1.0f), // 4: Yellow
        new(1.0f, 0.0f, 1.0f, 1.0f), // 5: Magenta
        new(0.0f, 1.0f, 1.0f, 1.0f), // 6: Cyan
        new(0.0f, 0.0f, 0.0f, 0.0f), // 7: Transparent
        new(0.5f, 0.5f, 0.5f, 1.0f), // 8: Gray
        new(0.2f, 0.2f, 0.2f, 1.0f), // 9: Dark Gray
    ];

    // Font metrics
    private static float _fontSize = 32.0f;
    private static float _charWidth = 0.0f;
    private static float _lineHeight = 0.0f;

    // Test content
    private static readonly string[] _testLines = [
        "The quick brown fox jumps over the lazy dog",
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
        "abcdefghijklmnopqrstuvwxyz",
        "0123456789!@#$%^&*()_+-=[]{}|;:,.<>?",
        "Bold text should appear heavier and darker",
        "Italic text should appear slanted or oblique",
        "Underlined text should have a line beneath",
        "Strikethrough text should have a line through",
        "Inverse text should swap foreground/background",
        "Dim text should appear lighter or faded"
    ];

    /// <summary>
    /// Runs the text styling experiments.
    /// </summary>
    public static void Run()
    {
        try
        {
            Console.WriteLine("Starting Text Styling Experiments...");
            
            // Update cursor blinking
            UpdateCursorBlink();
            
            DrawExperiments();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in text styling experiments: {ex.Message}");
        }
    }

    private static void UpdateCursorBlink()
    {
        if (_cursorBlinking)
        {
            var currentTime = DateTime.Now;
            if ((currentTime - _lastBlinkTime).TotalMilliseconds > 500) // 500ms blink interval
            {
                _blinkState = !_blinkState;
                _lastBlinkTime = currentTime;
            }
        }
        else
        {
            _blinkState = true; // Always visible when not blinking
        }
    }

    private static void PushHackFont(out bool fontUsed, float? size = null, bool bold = false, bool italic = false)
    {
        // Determine font name based on styling
        string fontName = "HackNerdFontMono-Regular";
        if (bold && italic)
        {
            fontName = "HackNerdFontMono-BoldItalic";
        }
        else if (bold)
        {
            fontName = "HackNerdFontMono-Bold";
        }
        else if (italic)
        {
            fontName = "HackNerdFontMono-Italic";
        }

        if (FontManager.Fonts.TryGetValue(fontName, out ImFontPtr fontPtr))
        {
            ImGui.PushFont(fontPtr, size ?? _fontSize);
            fontUsed = true;
            
            // Calculate font metrics
            _charWidth = _fontSize * 0.6f; // Monospace approximation
            _lineHeight = _fontSize + 2.0f; // Good vertical spacing
            return;
        }

        // Fallback to regular font if styled variant not available
        if (FontManager.Fonts.TryGetValue("HackNerdFontMono-Regular", out fontPtr))
        {
            ImGui.PushFont(fontPtr, size ?? _fontSize);
            fontUsed = true;
            
            // Calculate font metrics
            _charWidth = _fontSize * 0.6f; // Monospace approximation
            _lineHeight = _fontSize + 2.0f; // Good vertical spacing
            return;
        }

        fontUsed = false;
        _charWidth = _fontSize * 0.6f;
        _lineHeight = _fontSize + 2.0f;
    }

    private static void MaybePopFont(bool wasUsed)
    {
        if (wasUsed) 
        {
            ImGui.PopFont();
        }
    }

    /// <summary>
    /// Draws the text styling experiments UI.
    /// </summary>
    public static void DrawExperiments()
    {
        // Update cursor blinking state
        UpdateCursorBlink();
        
        PushHackFont(out bool fontUsed);

        // Main experiment window
        ImGui.Begin("Text Styling Experiments");

        ImGui.Text("Text Styling and Cursor Experiments - Task 1.6");
        ImGui.Separator();

        // Experiment selector
        ImGui.Combo("Experiment", ref _selectedExperiment, _experimentNames, _experimentNames.Length);
        ImGui.Separator();

        // Draw the selected experiment
        switch (_selectedExperiment)
        {
            case 0:
                DrawTextAttributesTest();
                break;
            case 1:
                DrawFontStyleVariations();
                break;
            case 2:
                DrawCursorDisplayTechniques();
                break;
            case 3:
                DrawInteractiveStylingControls();
                break;
            case 4:
                DrawStylingLimitationsAnalysis();
                break;
        }

        MaybePopFont(fontUsed);

        ImGui.End();
    }

    private static void DrawTextAttributesTest()
    {
        ImGui.Text("Text Attributes Testing");
        ImGui.Text("Testing different approaches to text styling in ImGui");
        ImGui.Separator();

        var drawList = ImGui.GetWindowDrawList();
        var windowPos = ImGui.GetCursorScreenPos();

        // Test different text attributes
        var attributes = new[]
        {
            ("Normal Text", false, false, false, false, false),
            ("Bold Text (True Font)", true, false, false, false, false),
            ("Italic Text (True Font)", false, true, false, false, false),
            ("Bold Italic Text", true, true, false, false, false),
            ("Underlined Text", false, false, true, false, false),
            ("Strikethrough Text", false, false, false, true, false),
            ("Inverse Text", false, false, false, false, true),
        };

        for (int i = 0; i < attributes.Length; i++)
        {
            var (text, bold, italic, underline, strikethrough, inverse) = attributes[i];
            var y = windowPos.Y + i * _lineHeight;
            
            DrawStyledText(drawList, new float2(windowPos.X, y), text, 
                bold, italic, underline, strikethrough, inverse, false);
        }

        ImGui.Dummy(new float2(400, attributes.Length * _lineHeight));

        ImGui.Separator();
        ImGui.Text("Styling Approach Analysis:");
        ImGui.BulletText("Bold: True bold font (HackNerdFontMono-Bold) with fallback simulation");
        ImGui.BulletText("Italic: True italic font (HackNerdFontMono-Italic) support");
        ImGui.BulletText("Bold+Italic: Combined font variant (HackNerdFontMono-BoldItalic)");
        ImGui.BulletText("Underline: Custom drawing using DrawList.AddLine()");
        ImGui.BulletText("Strikethrough: Custom drawing using DrawList.AddLine()");
        ImGui.BulletText("Inverse: Swap foreground/background colors with visibility fixes");
        ImGui.BulletText("Dim: Reduce alpha or use darker color variant");
    }

    private static void DrawFontStyleVariations()
    {
        ImGui.Text("Font Style Variations");
        ImGui.Text("Comparing different font rendering techniques");
        ImGui.Separator();

        var drawList = ImGui.GetWindowDrawList();
        var windowPos = ImGui.GetCursorScreenPos();

        // Font size variations
        ImGui.Text("Font Size Variations:");
        var sizes = new float[] { 16.0f, 24.0f, 32.0f, 48.0f };
        var currentY = windowPos.Y + _lineHeight;

        for (int i = 0; i < sizes.Length; i++)
        {
            PushHackFont(out bool sizedFontUsed, sizes[i]);
            
            var pos = new float2(windowPos.X, currentY);
            var text = $"Size {sizes[i]:F0}: The quick brown fox";
            drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(_colorPalette[0]), text);
            
            MaybePopFont(sizedFontUsed);
            
            currentY += sizes[i] + 4.0f; // Add some spacing
        }

        ImGui.Dummy(new float2(400, currentY - windowPos.Y));

        ImGui.Separator();
        ImGui.Text("Font Variant Testing:");
        
        var fontY = ImGui.GetCursorScreenPos().Y;
        var fontTests = new[]
        {
            ("Regular Font", false, false),
            ("Bold Font", true, false),
            ("Italic Font", false, true),
            ("Bold Italic Font", true, true)
        };
        
        for (int i = 0; i < fontTests.Length; i++)
        {
            var (label, bold, italic) = fontTests[i];
            var pos = new float2(windowPos.X, fontY + i * _lineHeight);
            
            PushHackFont(out bool variantFontUsed, _fontSize, bold, italic);
            drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(_colorPalette[0]), label);
            MaybePopFont(variantFontUsed);
        }

        ImGui.Dummy(new float2(400, fontTests.Length * _lineHeight));

        ImGui.Separator();
        ImGui.Text("Bold Simulation Techniques (fallback when no bold font):");
        
        var boldY = ImGui.GetCursorScreenPos().Y;
        var boldText = "Bold Text Simulation";
        
        // Technique 1: Multiple draws with offsets
        ImGui.Text("1. Multiple draws with pixel offsets:");
        DrawBoldText(drawList, new float2(windowPos.X + 20, boldY + _lineHeight), boldText, BoldTechnique.MultipleDraws);
        
        // Technique 2: Outline effect
        ImGui.Text("2. Outline effect:");
        DrawBoldText(drawList, new float2(windowPos.X + 20, boldY + _lineHeight * 3), boldText, BoldTechnique.Outline);
        
        // Technique 3: Color intensity
        ImGui.Text("3. Color intensity variation:");
        DrawBoldText(drawList, new float2(windowPos.X + 20, boldY + _lineHeight * 5), boldText, BoldTechnique.ColorIntensity);

        ImGui.Dummy(new float2(400, _lineHeight * 6));
    }

    private enum BoldTechnique
    {
        MultipleDraws,
        Outline,
        ColorIntensity
    }

    private static void DrawBoldText(ImDrawListPtr drawList, float2 pos, string text, BoldTechnique technique)
    {
        var color = ImGui.ColorConvertFloat4ToU32(_colorPalette[0]);
        
        switch (technique)
        {
            case BoldTechnique.MultipleDraws:
                // Draw text multiple times with slight offsets
                drawList.AddText(pos, color, text);
                drawList.AddText(new float2(pos.X + 0.5f, pos.Y), color, text);
                drawList.AddText(new float2(pos.X, pos.Y + 0.5f), color, text);
                drawList.AddText(new float2(pos.X + 0.5f, pos.Y + 0.5f), color, text);
                break;
                
            case BoldTechnique.Outline:
                // Draw outline first, then main text
                var outlineColor = ImGui.ColorConvertFloat4ToU32(new float4(0.3f, 0.3f, 0.3f, 1.0f));
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx != 0 || dy != 0)
                        {
                            drawList.AddText(new float2(pos.X + dx, pos.Y + dy), outlineColor, text);
                        }
                    }
                }
                drawList.AddText(pos, color, text);
                break;
                
            case BoldTechnique.ColorIntensity:
                // Use brighter/more intense color
                var brightColor = ImGui.ColorConvertFloat4ToU32(new float4(1.2f, 1.2f, 1.2f, 1.0f));
                drawList.AddText(pos, brightColor, text);
                break;
        }
    }

    private static void DrawCursorDisplayTechniques()
    {
        ImGui.Text("Cursor Display Techniques");
        ImGui.Text("Different cursor styles and blinking behavior");
        ImGui.Separator();

        // Cursor controls
        ImGui.Text("Cursor Controls:");
        var cursorTypes = new[] { "Block Cursor", "Underline Cursor", "Beam Cursor" };
        ImGui.Combo("Cursor Type", ref _cursorType, cursorTypes, cursorTypes.Length);
        ImGui.Checkbox("Cursor Visible", ref _cursorVisible);
        ImGui.Checkbox("Cursor Blinking", ref _cursorBlinking);
        
        ImGui.Separator();

        var drawList = ImGui.GetWindowDrawList();
        var windowPos = ImGui.GetCursorScreenPos();

        // Demo text with cursor
        var demoText = "Sample text with cursor here: ";
        var cursorPos = new float2(windowPos.X + demoText.Length * _charWidth, windowPos.Y);
        
        // Draw the demo text
        drawList.AddText(windowPos, ImGui.ColorConvertFloat4ToU32(_colorPalette[0]), demoText);
        
        // Draw cursor if visible and not blinking or blink state is on
        if (_cursorVisible && (!_cursorBlinking || _blinkState))
        {
            DrawCursor(drawList, cursorPos, _cursorType);
        }

        ImGui.Dummy(new float2(400, _lineHeight * 2));

        ImGui.Separator();
        ImGui.Text("Cursor Style Demonstrations:");

        // Show all cursor types
        var cursorDemoY = ImGui.GetCursorScreenPos().Y;
        var cursorNames = new[] { "Block", "Underline", "Beam" };
        
        for (int i = 0; i < 3; i++)
        {
            var pos = new float2(windowPos.X, cursorDemoY + i * _lineHeight);
            var textPos = new float2(pos.X + _charWidth * 2, pos.Y);
            
            // Draw cursor
            DrawCursor(drawList, pos, i);
            
            // Draw label
            drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(_colorPalette[0]), 
                $"{cursorNames[i]} cursor");
        }

        ImGui.Dummy(new float2(400, _lineHeight * 4));

        ImGui.Separator();
        ImGui.Text("Cursor Implementation Notes:");
        ImGui.BulletText("Block: Filled rectangle covering entire character cell");
        ImGui.BulletText("Underline: Horizontal line at bottom of character cell");
        ImGui.BulletText("Beam: Vertical line at left edge of character cell");
        ImGui.BulletText("Blinking: Toggle visibility every 500ms when enabled");
        ImGui.BulletText("All cursors use ImGui DrawList for custom rendering");
    }

    private static void DrawCursor(ImDrawListPtr drawList, float2 pos, int cursorType)
    {
        var cursorColor = ImGui.ColorConvertFloat4ToU32(_colorPalette[0]); // White cursor
        
        switch (cursorType)
        {
            case 0: // Block cursor
                var blockEnd = new float2(pos.X + _charWidth, pos.Y + _lineHeight);
                drawList.AddRectFilled(pos, blockEnd, cursorColor);
                break;
                
            case 1: // Underline cursor
                var underlineStart = new float2(pos.X, pos.Y + _lineHeight - 2);
                var underlineEnd = new float2(pos.X + _charWidth, pos.Y + _lineHeight - 2);
                drawList.AddLine(underlineStart, underlineEnd, cursorColor, 2.0f);
                break;
                
            case 2: // Beam cursor
                var beamStart = new float2(pos.X, pos.Y);
                var beamEnd = new float2(pos.X, pos.Y + _lineHeight);
                drawList.AddLine(beamStart, beamEnd, cursorColor, 2.0f);
                break;
        }
    }

    private static void DrawInteractiveStylingControls()
    {
        ImGui.Text("Interactive Styling Controls");
        ImGui.Text("Real-time text styling with interactive controls");
        ImGui.Separator();

        // Styling controls
        ImGui.Text("Text Attributes:");
        ImGui.Checkbox("Bold", ref _boldEnabled);
        ImGui.SameLine();
        ImGui.Checkbox("Italic", ref _italicEnabled);
        ImGui.SameLine();
        ImGui.Checkbox("Underline", ref _underlineEnabled);
        
        ImGui.Checkbox("Strikethrough", ref _strikethroughEnabled);
        ImGui.SameLine();
        ImGui.Checkbox("Inverse", ref _inverseEnabled);
        ImGui.SameLine();
        ImGui.Checkbox("Dim", ref _dimEnabled);
        
        ImGui.Checkbox("Blink", ref _blinkEnabled);

        ImGui.Separator();

        // Color controls
        ImGui.Text("Colors:");
        var colorNames = new[] { "White", "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan", "Transparent", "Gray", "Dark Gray" };
        ImGui.Combo("Foreground", ref _foregroundColorIndex, colorNames, colorNames.Length);
        ImGui.Combo("Background", ref _backgroundColorIndex, colorNames, colorNames.Length);

        // Debug color information
        if (_inverseEnabled)
        {
            ImGui.Separator();
            ImGui.Text("Debug - Color Values:");
            var fg = _colorPalette[_foregroundColorIndex];
            var bg = _colorPalette[_backgroundColorIndex];
            ImGui.Text($"Original FG: R={fg.X:F2} G={fg.Y:F2} B={fg.Z:F2} A={fg.W:F2}");
            ImGui.Text($"Original BG: R={bg.X:F2} G={bg.Y:F2} B={bg.Z:F2} A={bg.W:F2}");
            ImGui.Text($"After swap: FG=BG, BG=FG");
        }

        ImGui.Separator();

        // Live preview
        ImGui.Text("Live Preview:");
        var drawList = ImGui.GetWindowDrawList();
        var windowPos = ImGui.GetCursorScreenPos();

        // Draw sample text with current styling
        var sampleText = "Sample text with current styling applied";
        DrawStyledText(drawList, windowPos, sampleText,
            _boldEnabled, _italicEnabled, _underlineEnabled, _strikethroughEnabled,
            _inverseEnabled, _dimEnabled, _blinkEnabled);

        ImGui.Dummy(new float2(400, _lineHeight * 2));

        ImGui.Separator();

        // Multiple lines with different combinations
        ImGui.Text("Style Combination Examples:");
        var exampleY = ImGui.GetCursorScreenPos().Y;
        
        var examples = new[]
        {
            ("Normal text", false, false, false, false, false, false),
            ("Bold + Underline", true, false, true, false, false, false),
            ("Italic + Strikethrough", false, true, false, true, false, false),
            ("Inverse + Bold", true, false, false, false, true, false),
            ("All attributes", true, true, true, true, false, true),
        };

        for (int i = 0; i < examples.Length; i++)
        {
            var (text, bold, italic, underline, strikethrough, inverse, dim) = examples[i];
            var pos = new float2(windowPos.X, exampleY + i * _lineHeight);
            
            DrawStyledText(drawList, pos, text, bold, italic, underline, strikethrough, inverse, dim);
        }

        ImGui.Dummy(new float2(400, examples.Length * _lineHeight));
    }

    private static void DrawStyledText(ImDrawListPtr drawList, float2 pos, string text,
        bool bold, bool italic, bool underline, bool strikethrough, bool inverse, bool dim, bool blink = false)
    {
        // Handle blinking
        if (blink && _blinkEnabled && !_blinkState)
        {
            return; // Don't draw if blinking and in off state
        }

        // Determine colors
        var fgColor = _colorPalette[_foregroundColorIndex];
        var bgColor = _colorPalette[_backgroundColorIndex];
        
        if (inverse)
        {
            // Simple inverse: swap colors and ensure visibility
            (fgColor, bgColor) = (bgColor, fgColor);
            
            // If background is now transparent (was foreground), make it black
            if (bgColor.W < 0.1f)
            {
                bgColor = new float4(0.0f, 0.0f, 0.0f, 1.0f); // Black background
            }
            
            // If foreground is now transparent (was background), make it white
            if (fgColor.W < 0.1f)
            {
                fgColor = new float4(1.0f, 1.0f, 1.0f, 1.0f); // White foreground
            }
            
            // Simple contrast check: if both colors are very similar, force contrast
            var fgSum = fgColor.X + fgColor.Y + fgColor.Z;
            var bgSum = bgColor.X + bgColor.Y + bgColor.Z;
            
            if (Math.Abs(fgSum - bgSum) < 0.5f) // Colors too similar
            {
                if (bgSum < 1.5f) // Dark background
                {
                    fgColor = new float4(1.0f, 1.0f, 1.0f, 1.0f); // White text
                }
                else // Light background
                {
                    fgColor = new float4(0.0f, 0.0f, 0.0f, 1.0f); // Black text
                }
            }
        }
        
        if (dim)
        {
            fgColor = new float4(fgColor.X * 0.6f, fgColor.Y * 0.6f, fgColor.Z * 0.6f, fgColor.W);
        }

        // Draw background if not transparent
        if (bgColor.W > 0)
        {
            var bgEnd = new float2(pos.X + text.Length * _charWidth, pos.Y + _lineHeight);
            drawList.AddRectFilled(pos, bgEnd, ImGui.ColorConvertFloat4ToU32(bgColor));
        }

        // Use appropriate font for bold/italic
        PushHackFont(out bool styledFontUsed, _fontSize, bold, italic);

        // Draw text
        var textColor = ImGui.ColorConvertFloat4ToU32(fgColor);
        if (bold && !styledFontUsed)
        {
            // Fallback to bold simulation if no bold font available
            DrawBoldText(drawList, pos, text, BoldTechnique.MultipleDraws);
        }
        else
        {
            drawList.AddText(pos, textColor, text);
        }

        MaybePopFont(styledFontUsed);

        // Draw underline if enabled
        if (underline)
        {
            var underlineY = pos.Y + _lineHeight - 2;
            var underlineStart = new float2(pos.X, underlineY);
            var underlineEnd = new float2(pos.X + text.Length * _charWidth, underlineY);
            drawList.AddLine(underlineStart, underlineEnd, textColor, 1.0f);
        }

        // Draw strikethrough if enabled
        if (strikethrough)
        {
            var strikeY = pos.Y + _lineHeight * 0.5f;
            var strikeStart = new float2(pos.X, strikeY);
            var strikeEnd = new float2(pos.X + text.Length * _charWidth, strikeY);
            drawList.AddLine(strikeStart, strikeEnd, textColor, 1.0f);
        }
    }

    private static void DrawStylingLimitationsAnalysis()
    {
        ImGui.Text("Styling Capabilities and Limitations Analysis");
        ImGui.Text("Comprehensive analysis of ImGui text styling capabilities");
        ImGui.Separator();

        ImGui.Text("✅ Supported Features:");
        ImGui.BulletText("Bold text (true font + simulation fallback)");
        ImGui.BulletText("Italic text (true font support)");
        ImGui.BulletText("Bold+Italic combination (true font support)");
        ImGui.BulletText("Underline (custom line drawing)");
        ImGui.BulletText("Strikethrough (custom line drawing)");
        ImGui.BulletText("Color variations (foreground/background)");
        ImGui.BulletText("Inverse video (color swapping with visibility fixes)");
        ImGui.BulletText("Dim text (alpha/color reduction)");
        ImGui.BulletText("Cursor variations (block, underline, beam)");
        ImGui.BulletText("Cursor blinking (visibility toggling)");
        ImGui.BulletText("Custom drawing via DrawList");

        ImGui.Separator();
        ImGui.Text("❌ Limitations:");
        ImGui.BulletText("Font weight variations beyond bold (no semi-bold, extra-bold)");
        ImGui.BulletText("Advanced underline styles (double, wavy, dotted)");
        ImGui.BulletText("Complex text shaping (ligatures, kerning)");
        ImGui.BulletText("Proportional font support (monospace assumption)");

        ImGui.Separator();
        ImGui.Text("🔧 Workarounds and Solutions:");
        ImGui.BulletText("Bold: True font variants with simulation fallback");
        ImGui.BulletText("Italic: True font variants (HackNerdFontMono-Italic)");
        ImGui.BulletText("Underline styles: Custom line patterns with DrawList");
        ImGui.BulletText("Blinking: Timer-based visibility toggling");
        ImGui.BulletText("Inverse: Color swapping with visibility validation");
        ImGui.BulletText("Advanced effects: Combine multiple DrawList operations");

        ImGui.Separator();
        ImGui.Text("📊 Performance Considerations:");
        ImGui.BulletText("Bold simulation adds 4x draw calls per character");
        ImGui.BulletText("Custom decorations require additional DrawList calls");
        ImGui.BulletText("Blinking requires continuous frame updates");
        ImGui.BulletText("Background colors add rectangle draw calls");
        ImGui.BulletText("Consider batching for large terminal grids");

        ImGui.Separator();
        ImGui.Text("🎯 Recommendations for Terminal Implementation:");
        ImGui.BulletText("Use DrawList for all custom styling effects");
        ImGui.BulletText("Implement bold as configurable (quality vs performance)");
        ImGui.BulletText("Cache styled text measurements for positioning");
        ImGui.BulletText("Batch similar styling operations together");
        ImGui.BulletText("Consider LOD for distant or small text");
        ImGui.BulletText("Use texture atlases for repeated decorative elements");

        ImGui.Separator();
        ImGui.Text("📋 Implementation Priority for Terminal:");
        ImGui.Text("1. High Priority: Colors, bold simulation, underline, cursor");
        ImGui.Text("2. Medium Priority: Strikethrough, inverse, dim, blinking");
        ImGui.Text("3. Low Priority: Advanced underline styles, italic alternatives");
    }
}