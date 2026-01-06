using System;
using Brutal.ImGuiApi;
using caTTY.Core.Terminal;
using caTTY.Core.Types;
using caTTY.Display.Rendering;
using caTTY.Display.Types;
using float2 = Brutal.Numerics.float2;
using float4 = Brutal.Numerics.float4;

namespace caTTY.Display.Controllers.TerminalUi;

/// <summary>
///     Handles rendering of terminal content including cells, cursor, and text decorations.
/// </summary>
internal class TerminalUiRender
{
  private readonly TerminalUiFonts _fonts;
  private readonly CursorRenderer _cursorRenderer;
  private readonly Performance.PerformanceStopwatch _perfWatch;
  private readonly ColorResolver _colorResolver;
  private readonly StyleManager _styleManager;

  public TerminalUiRender(TerminalUiFonts fonts, CursorRenderer cursorRenderer, Performance.PerformanceStopwatch perfWatch, ColorResolver colorResolver, StyleManager styleManager)
  {
    _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
    _cursorRenderer = cursorRenderer ?? throw new ArgumentNullException(nameof(cursorRenderer));
    _perfWatch = perfWatch ?? throw new ArgumentNullException(nameof(perfWatch));
    _colorResolver = colorResolver ?? throw new ArgumentNullException(nameof(colorResolver));
    _styleManager = styleManager ?? throw new ArgumentNullException(nameof(styleManager));
  }

  /// <summary>
  ///     Renders the complete terminal content including all cells and cursor.
  /// </summary>
  public void RenderTerminalContent(
    SessionManager sessionManager,
    float currentCharacterWidth,
    float currentLineHeight,
    TextSelection currentSelection,
    out float2 lastTerminalOrigin,
    out float2 lastTerminalSize,
    Action handleMouseInputForTerminal,
    Action handleMouseTrackingForApplications)
  {
    var activeSession = sessionManager.ActiveSession;
    if (activeSession == null)
    {
      // No active session - show placeholder
      ImGui.Text("No terminal sessions. Click + to create one.");
      lastTerminalOrigin = new float2(0, 0);
      lastTerminalSize = new float2(0, 0);
      return;
    }

    // Push terminal content font for this rendering section
    _perfWatch.Start("Font.Push");
    _fonts.PushTerminalContentFont(out bool terminalFontUsed);
    _perfWatch.Stop("Font.Push");

    try
    {
      ImDrawListPtr drawList = ImGui.GetWindowDrawList();
      float2 windowPos = ImGui.GetCursorScreenPos();

      // Calculate terminal area
      float terminalWidth = activeSession.Terminal.Width * currentCharacterWidth;
      float terminalHeight = activeSession.Terminal.Height * currentLineHeight;

      // Cache terminal rect for input encoding (mouse wheel / mouse reporting)
      lastTerminalOrigin = windowPos;
      lastTerminalSize = new float2(terminalWidth, terminalHeight);

      // CRITICAL: Create an invisible button that captures mouse input and prevents window dragging
      // This is the key to preventing ImGui window dragging when selecting text
      ImGui.InvisibleButton("terminal_content", new float2(terminalWidth, terminalHeight));
      bool terminalHovered = ImGui.IsItemHovered();
      bool terminalActive = ImGui.IsItemActive();

      // Get the draw position after the invisible button
      float2 terminalDrawPos = windowPos;

      // Note: Terminal background is now handled by ImGui window background color
      // No need to draw a separate terminal background rectangle

      // Get viewport content from ScrollbackManager instead of directly from screen buffer
      _perfWatch.Start("GetViewportRows");
      var screenBuffer = new ReadOnlyMemory<Cell>[activeSession.Terminal.Height];
      for (int i = 0; i < activeSession.Terminal.Height; i++)
      {
        var rowSpan = activeSession.Terminal.ScreenBuffer.GetRow(i);
        var rowArray = new Cell[rowSpan.Length];
        rowSpan.CopyTo(rowArray);
        screenBuffer[i] = rowArray.AsMemory();
      }

      // Get the viewport rows that should be displayed (combines scrollback + screen buffer)
      var isAlternateScreenActive = ((TerminalEmulator)activeSession.Terminal).State.IsAlternateScreenActive;
      var viewportRows = activeSession.Terminal.ScrollbackManager.GetViewportRows(
          screenBuffer,
          isAlternateScreenActive,
          activeSession.Terminal.Height
      );
      _perfWatch.Stop("GetViewportRows");

      // Render each cell from the viewport content
      _perfWatch.Start("CellRenderingLoop");
      for (int row = 0; row < Math.Min(viewportRows.Count, activeSession.Terminal.Height); row++)
      {
        var rowMemory = viewportRows[row];
        var rowSpan = rowMemory.Span;

        for (int col = 0; col < Math.Min(rowSpan.Length, activeSession.Terminal.Width); col++)
        {
          Cell cell = rowSpan[col];
          RenderCell(drawList, terminalDrawPos, row, col, cell, currentCharacterWidth, currentLineHeight, currentSelection);
        }
      }
      _perfWatch.Stop("CellRenderingLoop");

      // Render cursor
      _perfWatch.Start("RenderCursor");
      RenderCursor(drawList, terminalDrawPos, activeSession, currentCharacterWidth, currentLineHeight);
      _perfWatch.Stop("RenderCursor");

      // Handle mouse input only when the invisible button is hovered/active
      if (terminalHovered || terminalActive)
      {
        _perfWatch.Start("HandleMouseInput");
        handleMouseInputForTerminal();
        _perfWatch.Stop("HandleMouseInput");
      }

      // Also handle mouse tracking for applications (this works regardless of hover state)
      handleMouseTrackingForApplications();
    }
    finally
    {
      TerminalUiFonts.MaybePopFont(terminalFontUsed);
    }
  }

  /// <summary>
  ///     Renders a single terminal cell.
  /// </summary>
  public void RenderCell(ImDrawListPtr drawList, float2 windowPos, int row, int col, Cell cell, float currentCharacterWidth, float currentLineHeight, TextSelection currentSelection)
  {
    _perfWatch.Start("RenderCell");
    try
    {
      _perfWatch.Start("RenderCell.Setup");
      float x = windowPos.X + (col * currentCharacterWidth);
      float y = windowPos.Y + (row * currentLineHeight);
      var pos = new float2(x, y);

      // Check if this cell is selected
      bool isSelected = !currentSelection.IsEmpty && currentSelection.Contains(row, col);
      _perfWatch.Stop("RenderCell.Setup");

      // Resolve colors using the new color resolution system
      _perfWatch.Start("RenderCell.ResolveColors");
      float4 baseForeground = _colorResolver.Resolve(cell.Attributes.ForegroundColor, false);
      float4 baseBackground = _colorResolver.Resolve(cell.Attributes.BackgroundColor, true);
      _perfWatch.Stop("RenderCell.ResolveColors");

      // Apply SGR attributes to colors
      var (fgColor, bgColor) = _styleManager.ApplyAttributes(cell.Attributes, baseForeground, baseBackground);

    // Apply foreground opacity to foreground colors and cell background opacity to background colors
    _perfWatch.Start("RenderCell.ApplyOpacity");
    fgColor = OpacityManager.ApplyForegroundOpacity(fgColor);
    bgColor = OpacityManager.ApplyCellBackgroundOpacity(bgColor);
    _perfWatch.Stop("RenderCell.ApplyOpacity");

    // Apply selection highlighting or draw background only when needed
    if (isSelected)
    {
      _perfWatch.Start("RenderCell.DrawSelection");
      // Use selection colors - invert foreground and background for selected text
      var selectionBg = new float4(0.3f, 0.5f, 0.8f, 0.7f); // Semi-transparent blue
      var selectionFg = new float4(1.0f, 1.0f, 1.0f, 1.0f); // White text

      // Apply foreground opacity to selection foreground and cell background opacity to selection background
      bgColor = OpacityManager.ApplyCellBackgroundOpacity(selectionBg);
      fgColor = OpacityManager.ApplyForegroundOpacity(selectionFg);

      // Always draw background for selected cells
      var bgRect = new float2(x + currentCharacterWidth, y + currentLineHeight);
      drawList.AddRectFilled(pos, bgRect, ImGui.ColorConvertFloat4ToU32(bgColor));
      _perfWatch.Stop("RenderCell.DrawSelection");
    }
    else if (cell.Attributes.BackgroundColor.HasValue)
    {
      _perfWatch.Start("RenderCell.DrawBackground");
      // Only draw background when SGR sequences have set a specific background color
      // This allows the theme background to show through for cells without explicit background colors
      var bgRect = new float2(x + currentCharacterWidth, y + currentLineHeight);
      drawList.AddRectFilled(pos, bgRect, ImGui.ColorConvertFloat4ToU32(bgColor));
      _perfWatch.Stop("RenderCell.DrawBackground");
    }
    // Note: When no SGR background color is set and cell is not selected,
    // the ImGui window background (theme background) will show through

      // Draw character if not space or null
      if (cell.Character != ' ' && cell.Character != '\0')
      {
        // Select appropriate font based on SGR attributes
        _perfWatch.Start("Font.SelectAndRender");
        _perfWatch.Start("Font.SelectAndRender.SelectFont");
        var font = _fonts.SelectFont(cell.Attributes);
        _perfWatch.Stop("Font.SelectAndRender.SelectFont");

        // Draw the character with selected font using proper PushFont/PopFont pattern
        _perfWatch.Start("Font.SelectAndRender.PushFont");
        ImGui.PushFont(font, _fonts.CurrentFontConfig.FontSize);
        _perfWatch.Stop("Font.SelectAndRender.PushFont");
        try
        {
          _perfWatch.Start("Font.SelectAndRender.AddText");
          drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(fgColor), cell.Character.ToString());
          _perfWatch.Stop("Font.SelectAndRender.AddText");
        }
        finally
        {
          _perfWatch.Start("Font.SelectAndRender.PopFont");
          ImGui.PopFont();
          _perfWatch.Stop("Font.SelectAndRender.PopFont");
        }
        _perfWatch.Stop("Font.SelectAndRender");

        // Draw underline if needed (but not for selected text to avoid visual clutter)
        if (!isSelected && _styleManager.ShouldRenderUnderline(cell.Attributes))
        {
          _perfWatch.Start("RenderDecorations");
          RenderUnderline(drawList, pos, cell.Attributes, fgColor, currentCharacterWidth, currentLineHeight);
          _perfWatch.Stop("RenderDecorations");
        }

        // Draw strikethrough if needed (but not for selected text to avoid visual clutter)
        if (!isSelected && _styleManager.ShouldRenderStrikethrough(cell.Attributes))
        {
          _perfWatch.Start("RenderDecorations");
          RenderStrikethrough(drawList, pos, fgColor, currentCharacterWidth, currentLineHeight);
          _perfWatch.Stop("RenderDecorations");
        }
      }
    }
    finally
    {
      _perfWatch.Stop("RenderCell");
    }
  }

  /// <summary>
  ///     Renders the terminal cursor using the new cursor rendering system.
  /// </summary>
  public void RenderCursor(ImDrawListPtr drawList, float2 windowPos, TerminalSession activeSession, float currentCharacterWidth, float currentLineHeight)
  {
    if (activeSession == null) return;

    var terminalState = ((TerminalEmulator)activeSession.Terminal).State;
    ICursor cursor = activeSession.Terminal.Cursor;

    // Ensure cursor position is within bounds
    int cursorCol = Math.Max(0, Math.Min(cursor.Col, activeSession.Terminal.Width - 1));
    int cursorRow = Math.Max(0, Math.Min(cursor.Row, activeSession.Terminal.Height - 1));

    float x = windowPos.X + (cursorCol * currentCharacterWidth);
    float y = windowPos.Y + (cursorRow * currentLineHeight);
    var cursorPos = new float2(x, y);

    // Get cursor color from theme
    float4 cursorColor = ThemeManager.GetCursorColor();

    // Check if terminal is at bottom (not scrolled back)
    var scrollbackManager = activeSession.Terminal.ScrollbackManager;
    bool isAtBottom = scrollbackManager?.IsAtBottom ?? true;

    // Render cursor using the new cursor rendering system
    _cursorRenderer.RenderCursor(
        drawList,
        cursorPos,
        currentCharacterWidth,
        currentLineHeight,
        terminalState.CursorStyle,
        terminalState.CursorVisible,
        cursorColor,
        isAtBottom
    );
  }

  /// <summary>
  ///     Renders underline decoration for a cell.
  /// </summary>
  public void RenderUnderline(ImDrawListPtr drawList, float2 pos, SgrAttributes attributes, float4 foregroundColor, float currentCharacterWidth, float currentLineHeight)
  {
    _perfWatch.Start("RenderUnderline");
    try
    {
      float4 underlineColor = _styleManager.GetUnderlineColor(attributes, foregroundColor);
      underlineColor = OpacityManager.ApplyForegroundOpacity(underlineColor);
      float thickness = _styleManager.GetUnderlineThickness(attributes.UnderlineStyle);

      float underlineY = pos.Y + currentLineHeight - 2;
      var underlineStart = new float2(pos.X, underlineY);
      var underlineEnd = new float2(pos.X + currentCharacterWidth, underlineY);

      switch (attributes.UnderlineStyle)
      {
        case UnderlineStyle.Single:
          uint singleColor = ImGui.ColorConvertFloat4ToU32(underlineColor);
          float singleThickness = Math.Max(3.0f, thickness);
          drawList.AddLine(underlineStart, underlineEnd, singleColor, singleThickness);
          break;

        case UnderlineStyle.Double:
          // Draw two lines for double underline with proper spacing
          uint doubleColor = ImGui.ColorConvertFloat4ToU32(underlineColor);
          float doubleThickness = Math.Max(3.0f, thickness);

          // First line (bottom) - same position as single underline
          drawList.AddLine(underlineStart, underlineEnd, doubleColor, doubleThickness);

          // Second line (top) - spaced 4 pixels above the first for better visibility
          var doubleStart = new float2(pos.X, underlineY - 4);
          var doubleEnd = new float2(pos.X + currentCharacterWidth, underlineY - 4);
          drawList.AddLine(doubleStart, doubleEnd, doubleColor, doubleThickness);
          break;

        case UnderlineStyle.Curly:
          // Draw wavy line using bezier curves for a smooth curly effect
          RenderCurlyUnderline(drawList, pos, underlineColor, thickness, currentCharacterWidth, currentLineHeight);
          break;

        case UnderlineStyle.Dotted:
          // Draw dotted line using small segments with spacing
          RenderDottedUnderline(drawList, pos, underlineColor, thickness, currentCharacterWidth, currentLineHeight);
          break;

        case UnderlineStyle.Dashed:
          // Draw dashed line using longer segments with spacing
          RenderDashedUnderline(drawList, pos, underlineColor, thickness, currentCharacterWidth, currentLineHeight);
          break;
      }
    }
    finally
    {
      _perfWatch.Stop("RenderUnderline");
    }
  }

  /// <summary>
  ///     Renders strikethrough for a cell.
  /// </summary>
  public void RenderStrikethrough(ImDrawListPtr drawList, float2 pos, float4 foregroundColor, float currentCharacterWidth, float currentLineHeight)
  {
    _perfWatch.Start("RenderStrikethrough");
    try
    {
      // Apply foreground opacity to strikethrough color
      foregroundColor = OpacityManager.ApplyForegroundOpacity(foregroundColor);

      float strikeY = pos.Y + (currentLineHeight / 2);
      var strikeStart = new float2(pos.X, strikeY);
      var strikeEnd = new float2(pos.X + currentCharacterWidth, strikeY);
      drawList.AddLine(strikeStart, strikeEnd, ImGui.ColorConvertFloat4ToU32(foregroundColor));
    }
    finally
    {
      _perfWatch.Stop("RenderStrikethrough");
    }
  }

  /// <summary>
  ///     Renders a curly underline using bezier curves for smooth wavy effect.
  /// </summary>
  public void RenderCurlyUnderline(ImDrawListPtr drawList, float2 pos, float4 underlineColor, float thickness, float currentCharacterWidth, float currentLineHeight)
  {
    float underlineY = pos.Y + currentLineHeight - 2;
    uint color = ImGui.ColorConvertFloat4ToU32(underlineColor);
    float curlyThickness = Math.Max(3.0f, thickness);

    // Create a wavy line using multiple bezier curve segments with much higher amplitude
    float waveHeight = 4.0f; // Much bigger amplitude for very visible waves
    float segmentWidth = currentCharacterWidth / 2.0f; // 2 wave segments per character for smoother curves

    for (int i = 0; i < 2; i++)
    {
      float startX = pos.X + (i * segmentWidth);
      float endX = pos.X + ((i + 1) * segmentWidth);

      // Alternate wave direction for each segment to create continuous wave
      float controlOffset = (i % 2 == 0) ? -waveHeight : waveHeight;

      var p1 = new float2(startX, underlineY);
      var p2 = new float2(startX + segmentWidth * 0.3f, underlineY + controlOffset);
      var p3 = new float2(startX + segmentWidth * 0.7f, underlineY - controlOffset);
      var p4 = new float2(endX, underlineY);

      drawList.AddBezierCubic(p1, p2, p3, p4, color, curlyThickness);
    }
  }

  /// <summary>
  ///     Renders a dotted underline using small line segments with spacing.
  /// </summary>
  public void RenderDottedUnderline(ImDrawListPtr drawList, float2 pos, float4 underlineColor, float thickness, float currentCharacterWidth, float currentLineHeight)
  {
    _perfWatch.Start("RenderDottedUnderline");
    try
    {
      float underlineY = pos.Y + currentLineHeight - 2;
      uint color = ImGui.ColorConvertFloat4ToU32(underlineColor);
      float dottedThickness = Math.Max(3.0f, thickness);

      float dotSize = 3.0f; // Increased dot size for better visibility
      float spacing = 3.0f; // Increased spacing for clearer separation
      float totalStep = dotSize + spacing;

      for (float x = pos.X; x < pos.X + currentCharacterWidth - dotSize; x += totalStep)
      {
        float dotEnd = Math.Min(x + dotSize, pos.X + currentCharacterWidth);
        var dotStart = new float2(x, underlineY);
        var dotEndPos = new float2(dotEnd, underlineY);
        drawList.AddLine(dotStart, dotEndPos, color, dottedThickness);
      }
    }
    finally
    {
      _perfWatch.Stop("RenderDottedUnderline");
    }
  }

  /// <summary>
  ///     Renders a dashed underline using longer line segments with spacing.
  /// </summary>
  public void RenderDashedUnderline(ImDrawListPtr drawList, float2 pos, float4 underlineColor, float thickness, float currentCharacterWidth, float currentLineHeight)
  {
    _perfWatch.Start("RenderDashedUnderline");
    try
    {
      float underlineY = pos.Y + currentLineHeight - 2;
      uint color = ImGui.ColorConvertFloat4ToU32(underlineColor);
      float dashedThickness = Math.Max(3.0f, thickness);

      float dashSize = 6.0f; // Increased dash length for better visibility
      float spacing = 4.0f; // Increased spacing for clearer separation
      float totalStep = dashSize + spacing;

      for (float x = pos.X; x < pos.X + currentCharacterWidth - dashSize; x += totalStep)
      {
        float dashEnd = Math.Min(x + dashSize, pos.X + currentCharacterWidth);
        var dashStart = new float2(x, underlineY);
        var dashEndPos = new float2(dashEnd, underlineY);
        drawList.AddLine(dashStart, dashEndPos, color, dashedThickness);
      }
    }
    finally
    {
      _perfWatch.Stop("RenderDashedUnderline");
    }
  }
}
