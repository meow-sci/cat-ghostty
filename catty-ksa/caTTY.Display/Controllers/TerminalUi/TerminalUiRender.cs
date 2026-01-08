using System;
using System.Buffers;
using System.Collections.Generic;
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
  private readonly CachedColorResolver _colorResolver;
  private readonly StyleManager _styleManager;
  private readonly TerminalViewportRenderCache? _renderCache;
  
  // Reusable buffers to avoid per-frame allocations
  private ReadOnlyMemory<Cell>[] _screenBufferCache = [];
  private readonly List<ReadOnlyMemory<Cell>> _viewportRowsCache = new(256);

  public TerminalUiRender(TerminalUiFonts fonts, CursorRenderer cursorRenderer, Performance.PerformanceStopwatch perfWatch, CachedColorResolver colorResolver, StyleManager styleManager, TerminalViewportRenderCache? renderCache = null)
  {
    _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
    _cursorRenderer = cursorRenderer ?? throw new ArgumentNullException(nameof(cursorRenderer));
    _perfWatch = perfWatch ?? throw new ArgumentNullException(nameof(perfWatch));
    _colorResolver = colorResolver ?? throw new ArgumentNullException(nameof(colorResolver));
    _styleManager = styleManager ?? throw new ArgumentNullException(nameof(styleManager));
    _renderCache = renderCache;
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
    Action handleMouseTrackingForApplications,
    Action renderContextMenu,
    bool drawBackground)
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
//    _perfWatch.Start("Font.Push");
    _fonts.PushTerminalContentFont(out bool terminalFontUsed);
//    _perfWatch.Stop("Font.Push");

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

      // Open context menu popup on right-click of the invisible button
      // MUST be called immediately after InvisibleButton while it's still the "last item"
      if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
      {
        ImGui.OpenPopup("terminal_context_menu");
      }

      // Get the draw position after the invisible button
      float2 terminalDrawPos = windowPos;

      // Note: Terminal background is now handled manually since window bg is transparent
      // Draw a separate terminal background rectangle for the content area
      if (drawBackground)
      {
        float4 themeBg = ThemeManager.GetDefaultBackground();
        themeBg = OpacityManager.ApplyBackgroundOpacity(themeBg);
        uint themeBgU32 = ImGui.ColorConvertFloat4ToU32(themeBg);
        
        drawList.AddRectFilled(
            terminalDrawPos,
            terminalDrawPos + new float2(terminalWidth, terminalHeight),
            themeBgU32
        );
      }
      
      // Compute render key for caching
      var renderKey = new TerminalRenderKey(
          activeSession.Terminal.ScreenBuffer.Revision,
          activeSession.Terminal.ViewportOffset,
          ThemeManager.Version,
          _fonts.CurrentFontSize,
          currentCharacterWidth,
          currentLineHeight,
          activeSession.Terminal.Width,
          activeSession.Terminal.Height,
          0);

      // Check for cache hit
      bool cacheHit = _renderCache != null && _renderCache.IsValid(renderKey);
      bool isCaching = false;
      
      // We need to ensure the viewport rows are populated if we are going to render selection overlay
      // or if we are rendering grid content.
      // If Cache Hit, we didn't call RenderGridContent, so we must populate manually if we have selection.
      bool hasSelection = !currentSelection.IsEmpty && currentSelection.Start != currentSelection.End;

      if (cacheHit)
      {
          // Draw from cache
          _renderCache!.Draw(drawList, terminalDrawPos);
          
          // Draw selection overlay if needed
          if (hasSelection)
          {
              PopulateViewportCache(activeSession);
              RenderSelectionOverlay(activeSession, drawList, terminalDrawPos, currentCharacterWidth, currentLineHeight, currentSelection);
          }
      }
      else
      {
          // If cache is available but invalid, try to capture
          if (_renderCache != null)
          {
              isCaching = _renderCache.BeginCapture(renderKey);
          }
          
          // If caching, we render WITHOUT selection (clean state)
          // If not caching (slow path), we render WITH selection (inline)
          var selectionForGrid = isCaching ? default(TextSelection) : currentSelection;
          
          if (isCaching)
          {
               // Capture Mode
               if (_renderCache!.GetBackingStore() is CommandBufferBackingStore cmdStore)
               {
                   var target = cmdStore.GetTarget();
                   if (target != null)
                   {
                       RenderGridContent(activeSession, target, terminalDrawPos, currentCharacterWidth, currentLineHeight, selectionForGrid);
                   }
               }
               
               _renderCache!.EndCapture();
               // Draw the captured texture
               _renderCache.Draw(drawList, terminalDrawPos);
               
               // Draw selection overlay on top
               if (hasSelection)
               {
                    // Viewport cache is already populated by RenderGridContent
                    RenderSelectionOverlay(activeSession, drawList, terminalDrawPos, currentCharacterWidth, currentLineHeight, currentSelection);
               }
          }
          else
          {
               // Direct Mode
               var target = new ImGuiDirectDrawTarget(drawList);
               RenderGridContent(activeSession, target, terminalDrawPos, currentCharacterWidth, currentLineHeight, selectionForGrid);
          }
      }

      // Render cursor
//      _perfWatch.Start("RenderCursor");
      RenderCursor(drawList, terminalDrawPos, activeSession, currentCharacterWidth, currentLineHeight);
//      _perfWatch.Stop("RenderCursor");

      // Handle mouse input only when the invisible button is hovered/active
      if (terminalHovered || terminalActive)
      {
//        _perfWatch.Start("HandleMouseInput");
        handleMouseInputForTerminal();
//        _perfWatch.Stop("HandleMouseInput");
      }

      // ALWAYS render the context menu popup (even when not hovering)
      // This is necessary because when the popup is open, we're no longer hovering the terminal
      renderContextMenu();

      // Also handle mouse tracking for applications (this works regardless of hover state)
      handleMouseTrackingForApplications();
    }
    finally
    {
      TerminalUiFonts.MaybePopFont(terminalFontUsed);
    }
  }

  private void PopulateViewportCache(TerminalSession activeSession)
  {
      // Get viewport content from ScrollbackManager instead of directly from screen buffer
//      _perfWatch.Start("GetViewportRows");
      
      // Ensure screen buffer cache is the right size
      int terminalRowCount = activeSession.Terminal.Height;
      if (_screenBufferCache.Length != terminalRowCount)
      {
        _screenBufferCache = new ReadOnlyMemory<Cell>[terminalRowCount];
      }
      
      // Get row memory references directly from ScreenBuffer - no allocation!
      for (int i = 0; i < terminalRowCount; i++)
      {
        _screenBufferCache[i] = activeSession.Terminal.ScreenBuffer.GetRowMemory(i);
      }

      // Get the viewport rows that should be displayed (combines scrollback + screen buffer)
      var isAlternateScreenActive = ((TerminalEmulator)activeSession.Terminal).State.IsAlternateScreenActive;
      activeSession.Terminal.ScrollbackManager.GetViewportRowsNonAlloc(
          _screenBufferCache,
          isAlternateScreenActive,
          terminalRowCount,
          _viewportRowsCache
      );
//      _perfWatch.Stop("GetViewportRows");
  }

  private void RenderGridContent(
      TerminalSession activeSession,
      ITerminalDrawTarget target,
      float2 terminalDrawPos,
      float currentCharacterWidth,
      float currentLineHeight,
      TextSelection currentSelection)
  {
      PopulateViewportCache(activeSession);
      var viewportRows = _viewportRowsCache;

      // Check if we're viewing the live screen buffer (not scrolled into scrollback history)
      // This enables dirty row tracking optimization
      // - In alternate screen mode: always viewing live buffer (no scrollback)
      // - In primary screen mode: only when auto-scroll is enabled (not scrolled up)
      var isAlternateScreenActive = ((TerminalEmulator)activeSession.Terminal).State.IsAlternateScreenActive;
      bool isViewingLiveScreen = isAlternateScreenActive || activeSession.Terminal.IsAutoScrollEnabled;
      bool canUseDirtyTracking = isViewingLiveScreen;

      // Render each cell from the viewport content
//      _perfWatch.Start("CellRenderingLoop");
      int terminalWidthCells = activeSession.Terminal.Width;
      char[] runChars = ArrayPool<char>.Shared.Rent(Math.Max(terminalWidthCells, 1));
      float4[] foregroundColors = ArrayPool<float4>.Shared.Rent(Math.Max(terminalWidthCells, 1));
      SgrAttributes[] cellAttributes = ArrayPool<SgrAttributes>.Shared.Rent(Math.Max(terminalWidthCells, 1));
      bool[] isSelectedByCol = ArrayPool<bool>.Shared.Rent(Math.Max(terminalWidthCells, 1));

      try
      {
        for (int row = 0; row < Math.Min(viewportRows.Count, activeSession.Terminal.Height); row++)
        {
          var rowMemory = viewportRows[row];
          var rowSpan = rowMemory.Span;
          int colsToRender = Math.Min(rowSpan.Length, terminalWidthCells);

          int runStartCol = 0;
          int runLength = 0;
          uint runColorU32 = 0;
          ImFontPtr runFont = default;

          // Background run tracking for batching contiguous same-color backgrounds
          int bgRunStartCol = -1;
          int bgRunLength = 0;
          uint bgRunColorU32 = 0;

          void FlushBackgroundRun()
          {
            if (bgRunLength <= 0)
              return;

            float bgX = terminalDrawPos.X + (bgRunStartCol * currentCharacterWidth);
            float bgY = terminalDrawPos.Y + (row * currentLineHeight);
            float bgWidth = bgRunLength * currentCharacterWidth;
            float bgHeight = currentLineHeight;


            target.AddRectFilled(
              new float2(bgX, bgY),
              new float2(bgX + bgWidth, bgY + bgHeight),
              bgRunColorU32
            );

            bgRunLength = 0;
            bgRunStartCol = -1;
          }

          void FlushRun()
          {
            if (runLength <= 0)
              return;

            // IMPORTANT: Flush backgrounds BEFORE text to ensure correct draw order
            FlushBackgroundRun();

//            _perfWatch.Start("RenderCell.FlushRun");

            float runY = terminalDrawPos.Y + (row * currentLineHeight);

//            _perfWatch.Start("Font.SelectAndRender");
//            _perfWatch.Start("Font.SelectAndRender.PushFont");
            // ImGui.PushFont(runFont, _fonts.CurrentFontConfig.FontSize); // HANDLED BY TARGET IN ADDTEXT or recorded
//            _perfWatch.Stop("Font.SelectAndRender.PushFont");
            try
            {
//              _perfWatch.Start("Font.SelectAndRender.AddText");
              // Render each character at its exact grid position to prevent drift
              // caused by font glyph advance widths not matching currentCharacterWidth.
              // This fixes character shifting when selection changes run boundaries.
              for (int i = 0; i < runLength; i++)
              {
                float charX = terminalDrawPos.X + ((runStartCol + i) * currentCharacterWidth);
                var charPos = new float2(charX, runY);
                target.AddText(charPos, runColorU32, runChars[i].ToString(), runFont, _fonts.CurrentFontConfig.FontSize);
              }
//              _perfWatch.Stop("Font.SelectAndRender.AddText");
            }
            finally
            {
//              _perfWatch.Start("Font.SelectAndRender.PopFont");
              // ImGui.PopFont(); // HANDLED BY TARGET or Implicit
//              _perfWatch.Stop("Font.SelectAndRender.PopFont");
//              _perfWatch.Stop("Font.SelectAndRender");
            }

            // Decorations must be drawn after text to preserve existing draw order.
//            _perfWatch.Start("RenderCell.FlushRun.DecorationsLoop");
            for (int i = 0; i < runLength; i++)
            {
              int col = runStartCol + i;
              if (col < 0 || col >= colsToRender)
                continue;

              if (isSelectedByCol[col])
                continue;

              var attrs = cellAttributes[col];
              var fgColor = foregroundColors[col];
              float x = terminalDrawPos.X + (col * currentCharacterWidth);
              float y = terminalDrawPos.Y + (row * currentLineHeight);
              var pos = new float2(x, y);

              if (_styleManager.ShouldRenderUnderline(attrs))
              {
//                _perfWatch.Start("RenderDecorations");
                RenderUnderline(target, pos, attrs, fgColor, currentCharacterWidth, currentLineHeight);
//                _perfWatch.Stop("RenderDecorations");
              }

              if (_styleManager.ShouldRenderStrikethrough(attrs))
              {
//                _perfWatch.Start("RenderDecorations");
                RenderStrikethrough(target, pos, fgColor, currentCharacterWidth, currentLineHeight);
//                _perfWatch.Stop("RenderDecorations");
              }
            }

//            _perfWatch.Stop("RenderCell.FlushRun.DecorationsLoop");

            runLength = 0;

//            _perfWatch.Stop("RenderCell.FlushRun");
          }

          // Pre-compute whether this row might have any selection overlap
          bool rowMightHaveSelection = currentSelection.RowMightBeSelected(row);

          // EARLY EXIT: Skip rows with no content and no selection
          if (!rowMightHaveSelection && !RowHasContent(rowSpan))
          {
            continue;
          }

          // DIRTY ROW OPTIMIZATION: Skip clean rows that have no content requiring rendering
          // This only applies when viewing the live screen buffer (not scrollback)
          // Clean rows with no content can be safely skipped because nothing needs to be drawn
          if (canUseDirtyTracking && !activeSession.Terminal.ScreenBuffer.IsRowDirty(row))
          {
            // Row hasn't changed since last render and has already been skipped
            // (if it had content, it wouldn't pass the RowHasContent check above on future frames)
            // For truly empty rows that haven't been modified, we can skip processing
            if (!RowHasContent(rowSpan) && !rowMightHaveSelection)
            {
              continue;
            }
          }

          for (int col = 0; col < colsToRender; col++)
          {
//            _perfWatch.Start("RenderCell");
            try
            {
              Cell cell = rowSpan[col];

              // EARLY EXIT: Skip completely default empty cells
              // This is the most common case in typical terminal output
              bool isEmptyChar = cell.Character == ' ' || cell.Character == '\0';
              if (isEmptyChar && cell.Attributes.IsDefault)
              {
                // Quick selection check - only do full Contains() if row overlaps selection
                if (!rowMightHaveSelection || !currentSelection.Contains(row, col))
                {
                  // Flush any pending text run before skipping
                  FlushRun();
                  continue;
                }
              }

//              _perfWatch.Start("RenderCell.Setup");
              float x = terminalDrawPos.X + (col * currentCharacterWidth);
              float y = terminalDrawPos.Y + (row * currentLineHeight);
              var pos = new float2(x, y);

              // Check if this cell is selected
//              _perfWatch.Start("RenderCell.SelectionCheck");
              bool isSelected;
              if (!rowMightHaveSelection)
              {
                isSelected = false;
              }
              else
              {
//                _perfWatch.Start("RenderCell.SelectionCheck.Contains");
                isSelected = currentSelection.Contains(row, col);
//                _perfWatch.Stop("RenderCell.SelectionCheck.Contains");
              }
//              _perfWatch.Stop("RenderCell.SelectionCheck");

              isSelectedByCol[col] = isSelected;
              cellAttributes[col] = cell.Attributes;
//              _perfWatch.Stop("RenderCell.Setup");

              // Resolve and process all colors in one fused call
//              _perfWatch.Start("RenderCell.ResolveColors");
              _colorResolver.ResolveCellColors(
                  cell.Attributes,
                  out uint fgColorU32,
                  out uint cellBgColorU32,
                  out bool needsBackground,
                  out float4 fgColor);
//              _perfWatch.Stop("RenderCell.ResolveColors");

              foregroundColors[col] = fgColor;

              // Handle selection - override colors if selected
              if (isSelected)
              {
//                _perfWatch.Start("RenderCell.DrawSelection");
                // Use selection colors - invert foreground and background for selected text
                var selectionBg = new float4(0.3f, 0.5f, 0.8f, 0.7f); // Semi-transparent blue
                var selectionFg = new float4(1.0f, 1.0f, 1.0f, 1.0f); // White text

                // Apply foreground opacity to selection foreground and cell background opacity to selection background
                var bgColor = OpacityManager.ApplyCellBackgroundOpacity(selectionBg);
                fgColor = OpacityManager.ApplyForegroundOpacity(selectionFg);
                foregroundColors[col] = fgColor;

                cellBgColorU32 = ImGui.ColorConvertFloat4ToU32(bgColor);
                needsBackground = true;
//                _perfWatch.Stop("RenderCell.DrawSelection");
              }

              // Batch background drawing
              if (needsBackground)
              {
                bool canExtendRun = bgRunLength > 0
                    && col == bgRunStartCol + bgRunLength
                    && cellBgColorU32 == bgRunColorU32;

                if (canExtendRun)
                {
                  bgRunLength++;
                }
                else
                {
                  FlushBackgroundRun();
                  bgRunStartCol = col;
                  bgRunLength = 1;
                  bgRunColorU32 = cellBgColorU32;
                }
              }
              else
              {
                FlushBackgroundRun();
              }

              // Draw character if not space or null (batched into runs)
              if (cell.Character != ' ' && cell.Character != '\0')
              {
//                _perfWatch.Start("RenderCell.RunBatching");
//                _perfWatch.Start("Font.SelectAndRender.SelectFont");
                var font = _fonts.SelectFont(cell.Attributes);
//                _perfWatch.Stop("Font.SelectAndRender.SelectFont");

//                _perfWatch.Start("RenderCell.ConvertFgToU32");
                uint fgU32 = ImGui.ColorConvertFloat4ToU32(foregroundColors[col]);
//                _perfWatch.Stop("RenderCell.ConvertFgToU32");

                if (runLength == 0)
                {
                  runStartCol = col;
                  runFont = font;
                  runColorU32 = fgU32;
                  runChars[0] = cell.Character;
                  runLength = 1;
                }
                else
                {
//                  _perfWatch.Start("RenderCell.RunBatching.MergeDecision");
                  bool isContiguous = col == runStartCol + runLength;
                  bool sameFont = runFont.Equals(font);
                  bool sameColor = runColorU32 == fgU32;

//                  _perfWatch.Stop("RenderCell.RunBatching.MergeDecision");

                  if (isContiguous && sameFont && sameColor)
                  {
                    runChars[runLength] = cell.Character;
                    runLength++;
                  }
                  else
                  {
                    FlushRun();
                    runStartCol = col;
                    runFont = font;
                    runColorU32 = fgU32;
                    runChars[0] = cell.Character;
                    runLength = 1;
                  }
                }

//                _perfWatch.Stop("RenderCell.RunBatching");
              }
              else
              {
                FlushRun();
              }
            }
            finally
            {
//              _perfWatch.Stop("RenderCell");
            }
          }

          // FlushRun internally calls FlushBackgroundRun first to ensure correct draw order
          FlushRun();
          // Final background flush in case there's a trailing background with no text
          FlushBackgroundRun();
        }
      }
      finally
      {
        ArrayPool<char>.Shared.Return(runChars);
        ArrayPool<float4>.Shared.Return(foregroundColors);
        ArrayPool<SgrAttributes>.Shared.Return(cellAttributes);
        ArrayPool<bool>.Shared.Return(isSelectedByCol);
      }
//      _perfWatch.Stop("CellRenderingLoop");

      // Clear dirty flags after rendering (only when viewing current screen, not scrollback)
      // This marks all rows as "clean" so the next frame can detect new changes
      if (canUseDirtyTracking)
      {
        // Only clear when viewing current screen buffer content (not scrolled into history)
        activeSession.Terminal.ScreenBuffer.ClearDirtyFlags();
      }
  }

  private void RenderSelectionOverlay(
      TerminalSession activeSession,
      ImDrawListPtr drawList,
      float2 terminalDrawPos,
      float currentCharacterWidth,
      float currentLineHeight,
      TextSelection currentSelection)
  {
      var viewportRows = _viewportRowsCache;
      int terminalWidthCells = activeSession.Terminal.Width;
      
      // Basic overlay implementation - loops through selection and redraws cells
      // We can optimize this by only looping through selected rows
      
      // Selection Colors
      var selectionBg = new float4(0.3f, 0.5f, 0.8f, 0.7f); // Semi-transparent blue
      var selectionFg = new float4(1.0f, 1.0f, 1.0f, 1.0f); // White text
      var finalBg = OpacityManager.ApplyCellBackgroundOpacity(selectionBg);
      var finalFg = OpacityManager.ApplyForegroundOpacity(selectionFg);
      uint bgColU32 = ImGui.ColorConvertFloat4ToU32(finalBg);
      uint fgColU32 = ImGui.ColorConvertFloat4ToU32(finalFg);
      
      var fontSize = _fonts.CurrentFontConfig.FontSize;

      for (int row = 0; row < Math.Min(viewportRows.Count, activeSession.Terminal.Height); row++)
      {
          if (!currentSelection.RowMightBeSelected(row))
              continue;
              
          var rowMemory = viewportRows[row];
          var rowSpan = rowMemory.Span;
          int colsToRender = Math.Min(rowSpan.Length, terminalWidthCells);

          for (int col = 0; col < colsToRender; col++)
          {
              if (!currentSelection.Contains(row, col))
                  continue;
                  
              Cell cell = rowSpan[col];
              
              float x = terminalDrawPos.X + (col * currentCharacterWidth);
              float y = terminalDrawPos.Y + (row * currentLineHeight);
              var pos = new float2(x, y);
              
              // Draw Background
              drawList.AddRectFilled(
                  pos,
                  new float2(x + currentCharacterWidth, y + currentLineHeight),
                  bgColU32
              );
              
              // Draw Text
              if (cell.Character != ' ' && cell.Character != '\0')
              {
                  var font = _fonts.SelectFont(cell.Attributes);
                  ImGui.PushFont(font, fontSize);
                  drawList.AddText(pos, fgColU32, cell.Character.ToString());
                  ImGui.PopFont();
              }
              
              // TODO: Draw Styles (Underline etc) if needed?
              // Usually selection hides underlining or it's drawn in white.
              // For now, simpler selection is acceptable.
          }
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
  /// <summary>
  ///     Renders underline decoration for a cell.
  /// </summary>
  public void RenderUnderline(ITerminalDrawTarget target, float2 pos, SgrAttributes attributes, float4 foregroundColor, float currentCharacterWidth, float currentLineHeight)
  {
//    _perfWatch.Start("RenderUnderline");
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
          target.DrawLine(underlineStart, underlineEnd, singleColor, singleThickness);
          break;

        case UnderlineStyle.Double:
          // Draw two lines for double underline with proper spacing
          uint doubleColor = ImGui.ColorConvertFloat4ToU32(underlineColor);
          float doubleThickness = Math.Max(3.0f, thickness);

          // First line (bottom) - same position as single underline
          target.DrawLine(underlineStart, underlineEnd, doubleColor, doubleThickness);

          // Second line (top) - spaced 4 pixels above the first for better visibility
          var doubleStart = new float2(pos.X, underlineY - 4);
          var doubleEnd = new float2(pos.X + currentCharacterWidth, underlineY - 4);
          target.DrawLine(doubleStart, doubleEnd, doubleColor, doubleThickness);
          break;

        case UnderlineStyle.Curly:
          target.DrawCurlyUnderline(pos, underlineColor, thickness, currentCharacterWidth, currentLineHeight);
          break;

        case UnderlineStyle.Dotted:
          target.DrawDottedUnderline(pos, underlineColor, thickness, currentCharacterWidth, currentLineHeight);
          break;

        case UnderlineStyle.Dashed:
          target.DrawDashedUnderline(pos, underlineColor, thickness, currentCharacterWidth, currentLineHeight);
          break;
      }
    }
    finally
    {
//      _perfWatch.Stop("RenderUnderline");
    }
  }

  /// <summary>
  ///     Renders strikethrough for a cell.
  /// </summary>
  public void RenderStrikethrough(ITerminalDrawTarget target, float2 pos, float4 foregroundColor, float currentCharacterWidth, float currentLineHeight)
  {
//    _perfWatch.Start("RenderStrikethrough");
    try
    {
      // Apply foreground opacity to strikethrough color
      foregroundColor = OpacityManager.ApplyForegroundOpacity(foregroundColor);

      float strikeY = pos.Y + (currentLineHeight / 2);
      var strikeStart = new float2(pos.X, strikeY);
      var strikeEnd = new float2(pos.X + currentCharacterWidth, strikeY);
      target.DrawLine(strikeStart, strikeEnd, ImGui.ColorConvertFloat4ToU32(foregroundColor), 1.0f);
    }
    finally
    {
//      _perfWatch.Stop("RenderStrikethrough");
    }
  }

  /// <summary>
  ///     Checks if a row has any content that requires rendering.
  ///     Returns true if any cell has a non-space character or non-default attributes.
  /// </summary>
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  private static bool RowHasContent(ReadOnlySpan<Cell> rowSpan)
  {
    for (int i = 0; i < rowSpan.Length; i++)
    {
      ref readonly var cell = ref rowSpan[i];

      // Non-empty character?
      if (cell.Character != ' ' && cell.Character != '\0')
        return true;

      // Has explicit background color? (needs to draw background)
      if (cell.Attributes.BackgroundColor.HasValue)
        return true;

      // Has attributes that affect empty cells? (inverse makes spaces visible)
      if (cell.Attributes.Inverse)
        return true;
    }
    return false;
  }
  /// <summary>
  ///     internal wrapper for direct imgui drawing
  /// </summary>
  internal struct ImGuiDirectDrawTarget : ITerminalDrawTarget
  {
      private readonly ImDrawListPtr _drawList;
      
      public ImGuiDirectDrawTarget(ImDrawListPtr drawList)
      {
          _drawList = drawList;
      }
      
      public void AddRectFilled(float2 pMin, float2 pMax, uint col)
      {
          _drawList.AddRectFilled(pMin, pMax, col);
      }

      public void AddText(float2 pos, uint col, string text, ImFontPtr font, float fontSize)
      {
          ImGui.PushFont(font, fontSize);
          _drawList.AddText(pos, col, text);
          ImGui.PopFont();
      }

      public void DrawLine(float2 p1, float2 p2, uint col, float thickness)
      {
          _drawList.AddLine(p1, p2, col, thickness);
      }
      
      public void DrawCurlyUnderline(float2 pos, float4 color, float thickness, float width, float height)
      {
          caTTY.Display.Rendering.TerminalDecorationRenderers.RenderCurlyUnderline(_drawList, pos, color, thickness, width, height);
      }

      public void DrawDottedUnderline(float2 pos, float4 color, float thickness, float width, float height)
      {
          caTTY.Display.Rendering.TerminalDecorationRenderers.RenderDottedUnderline(_drawList, pos, color, thickness, width, height);
      }

      public void DrawDashedUnderline(float2 pos, float4 color, float thickness, float width, float height)
      {
          caTTY.Display.Rendering.TerminalDecorationRenderers.RenderDashedUnderline(_drawList, pos, color, thickness, width, height);
      }
  }
}
