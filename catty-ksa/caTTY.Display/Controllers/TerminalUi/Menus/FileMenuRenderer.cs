using System;
using System.Threading.Tasks;
using Brutal.ImGuiApi;
using caTTY.Core.Terminal;
using caTTY.Display.Controllers;
using caTTY.Display.Utils;

namespace caTTY.Display.Controllers.TerminalUi.Menus;

/// <summary>
/// Handles rendering of the File menu with terminal management options.
/// Provides menu items for creating, closing, navigating, and exiting terminal sessions.
/// </summary>
internal class FileMenuRenderer
{
  private readonly TerminalController _controller;
  private readonly SessionManager _sessionManager;

  public FileMenuRenderer(
    TerminalController controller,
    SessionManager sessionManager)
  {
    _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
  }

  /// <summary>
  /// Renders the File menu with terminal management options.
  /// </summary>
  /// <returns>True if the menu is currently open, false otherwise.</returns>
  public bool Render()
  {
    bool isOpen = ImGui.BeginMenu("File");
    if (isOpen)
    {
      try
      {
        // New Terminal - now enabled for multi-session support
        if (ImGui.MenuItem("New Terminal"))
        {
          _ = Task.Run(async () => await _sessionManager.CreateSessionAsync());
        }

        // Shell selection menu items
        ImGui.Separator();
        ImGui.Text("New Terminal with Shell:");

        var shellOptions = ShellSelectionHelper.GetAvailableShellOptions();

        if (shellOptions.Count == 0)
        {
          ImGui.TextDisabled("No shells available");
        }
        else
        {
          foreach (var option in shellOptions)
          {
            if (ImGui.MenuItem(option.DisplayName))
            {
              _ = Task.Run(async () =>
                await ShellSelectionHelper.CreateSessionWithShell(_sessionManager, option));
            }

            if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(option.Tooltip))
            {
              ImGui.SetTooltip(option.Tooltip);
            }
          }
        }

        ImGui.Separator();

        // Close Terminal - enabled when more than one session exists
        bool canCloseTerminal = _sessionManager.SessionCount > 1;
        if (ImGui.MenuItem("Close Terminal", "", false, canCloseTerminal))
        {
          var activeSession = _sessionManager.ActiveSession;
          if (activeSession != null)
          {
            _ = Task.Run(async () => await _sessionManager.CloseSessionAsync(activeSession.Id));
          }
        }

        ImGui.Separator();

        // Next Terminal - enabled when more than one session exists
        bool canNavigateSessions = _sessionManager.SessionCount > 1;
        if (ImGui.MenuItem("Next Terminal", "", false, canNavigateSessions))
        {
          _controller.SwitchToNextSessionAndFocus();
        }

        // Previous Terminal - enabled when more than one session exists
        if (ImGui.MenuItem("Previous Terminal", "", false, canNavigateSessions))
        {
          _controller.SwitchToPreviousSessionAndFocus();
        }

        ImGui.Separator();

        // Exit - closes the terminal window
        if (ImGui.MenuItem("Exit"))
        {
          _controller.IsVisible = false;
        }
      }
      finally
      {
        ImGui.EndMenu();
      }
    }
    return isOpen;
  }
}
