using System;
using Brutal.ImGuiApi;
using caTTY.Core.Terminal;

namespace caTTY.Display.Controllers.TerminalUi.Menus;

/// <summary>
/// Handles rendering of the Sessions menu with session management operations.
/// Provides menu items for viewing, switching, and managing terminal sessions.
/// </summary>
internal class SessionsMenuRenderer
{
  private readonly TerminalController _controller;
  private readonly SessionManager _sessionManager;

  public SessionsMenuRenderer(TerminalController controller, SessionManager sessionManager)
  {
    _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
  }

  /// <summary>
  /// Renders the Sessions menu with a list of all terminal sessions.
  /// Shows a checkmark for the currently active session and allows clicking to switch sessions.
  /// </summary>
  /// <returns>True if the menu is currently open, false otherwise.</returns>
  public bool Render()
  {
    bool isOpen = ImGui.BeginMenu("Sessions");
    if (isOpen)
    {
      try
      {
        var sessions = _sessionManager.Sessions;
        var activeSession = _sessionManager.ActiveSession;

        if (sessions.Count == 0)
        {
          ImGui.Text("No sessions available");
        }
        else
        {
          foreach (var session in sessions)
          {
            bool isActive = session == activeSession;
            string sessionLabel = session.Title;

            // Add process exit code to label if process has exited
            if (session.ProcessManager.ExitCode.HasValue)
            {
              sessionLabel += $" (Exit: {session.ProcessManager.ExitCode})";
            }

            // Create unique ImGui ID using session GUID to avoid conflicts
            string menuItemId = $"{sessionLabel}##session_menu_item_{session.Id}";

            if (ImGui.MenuItem(menuItemId, "", isActive))
            {
              if (!isActive)
              {
                _controller.SwitchToSessionAndFocus(session.Id);
              }
            }

            // Show tooltip with session information
            if (ImGui.IsItemHovered())
            {
              var tooltip = $"Session: {session.Title}\nCreated: {session.CreatedAt:HH:mm:ss}";
              if (session.LastActiveAt.HasValue)
              {
                tooltip += $"\nLast Active: {session.LastActiveAt.Value:HH:mm:ss}";
              }
              tooltip += $"\nState: {session.State}";
              if (session.ProcessManager.IsRunning)
              {
                tooltip += "\nProcess: Running";
              }
              else if (session.ProcessManager.ExitCode.HasValue)
              {
                tooltip += $"\nProcess: Exited ({session.ProcessManager.ExitCode})";
              }
              ImGui.SetTooltip(tooltip);
            }
          }
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
