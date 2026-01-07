using System;
using System.Threading.Tasks;
using Brutal.ImGuiApi;
using caTTY.Core.Terminal;
using caTTY.Display.Controllers;

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
