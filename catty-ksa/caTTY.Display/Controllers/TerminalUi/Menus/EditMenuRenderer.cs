using System;
using Brutal.ImGuiApi;
using caTTY.Display.Controllers.TerminalUi;
using caTTY.Display.Utils;

namespace caTTY.Display.Controllers.TerminalUi.Menus;

/// <summary>
/// Handles rendering of the Edit menu with text operations.
/// Provides menu items for copy, paste, and select all operations.
/// </summary>
internal class EditMenuRenderer
{
  private readonly TerminalController _controller;
  private readonly TerminalUiSelection _selection;

  public EditMenuRenderer(
    TerminalController controller,
    TerminalUiSelection selection)
  {
    _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    _selection = selection ?? throw new ArgumentNullException(nameof(selection));
  }

  /// <summary>
  /// Renders the Edit menu with text operations.
  /// </summary>
  public void Render()
  {
    if (ImGui.BeginMenu("Edit"))
    {
      try
      {
        // Copy - enabled only when selection exists
        bool hasSelection = !_selection.GetCurrentSelection().IsEmpty;
        if (ImGui.MenuItem("Copy", "", false, hasSelection))
        {
          _selection.CopySelectionToClipboard();
        }

        // Paste - always enabled
        if (ImGui.MenuItem("Paste"))
        {
          PasteFromClipboard();
        }

        // Select All - always enabled
        if (ImGui.MenuItem("Select All"))
        {
          SelectAllText();
        }
      }
      finally
      {
        ImGui.EndMenu();
      }
    }
  }

  /// <summary>
  /// Pastes text from the clipboard to the terminal.
  /// </summary>
  private void PasteFromClipboard()
  {
    try
    {
      string? clipboardText = ClipboardManager.GetText();
      if (!string.IsNullOrEmpty(clipboardText))
      {
        _controller.SendToProcess(clipboardText);
        Console.WriteLine($"TerminalController: Pasted {clipboardText.Length} characters from clipboard");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error pasting from clipboard: {ex.Message}");
    }
  }

  /// <summary>
  /// Selects all text in the terminal.
  /// </summary>
  private void SelectAllText()
  {
    _selection.SelectAllVisibleContent();
  }
}
