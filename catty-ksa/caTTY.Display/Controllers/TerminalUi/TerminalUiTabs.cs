using System;
using System.Threading.Tasks;
using Brutal.ImGuiApi;
using caTTY.Core.Terminal;
using caTTY.Display.Controllers;
using float2 = Brutal.Numerics.float2;

namespace caTTY.Display.Controllers.TerminalUi;

/// <summary>
///     Handles tab area rendering and tab-related operations for terminal sessions.
///     Provides ImGui tab bar implementation with session management integration.
/// </summary>
internal class TerminalUiTabs
{
  private readonly TerminalController _controller;
  private readonly SessionManager _sessionManager;
  private Guid? _lastActiveSessionId;

  /// <summary>
  ///     Creates a new tabs subsystem instance.
  /// </summary>
  /// <param name="controller">The parent terminal controller</param>
  /// <param name="sessionManager">The session manager instance</param>
  public TerminalUiTabs(TerminalController controller, SessionManager sessionManager)
  {
    _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
  }

  /// <summary>
  /// Calculates the current tab area height based on the number of terminal instances.
  /// Uses constrained sizing to prevent excessive height growth.
  /// </summary>
  /// <param name="tabCount">Number of terminal tabs (defaults to 1 for current single terminal)</param>
  /// <returns>Tab area height in pixels</returns>
  public static float CalculateTabAreaHeight(int tabCount = 1) => TerminalUiResize.CalculateTabAreaHeight(tabCount);

  /// <summary>
  /// Renders the tab area using real ImGui tabs for session management.
  /// Includes add button and context menus for tab operations.
  /// </summary>
  public void RenderTabArea()
  {
    try
    {
      var sessions = _sessionManager.Sessions;
      var activeSession = _sessionManager.ActiveSession;

      // Detect if active session changed (for tab synchronization)
      bool activeSessionChanged = activeSession?.Id != _lastActiveSessionId;
      if (activeSession != null)
      {
        _lastActiveSessionId = activeSession.Id;
      }

      // Get available width for tab area
      float availableWidth = ImGui.GetContentRegionAvail().X;
      float tabHeight = LayoutConstants.TAB_AREA_HEIGHT;

      // Create a child region for the tab area to maintain consistent height
      bool childBegun = ImGui.BeginChild("TabArea", new float2(availableWidth, tabHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar);

      try
      {
        if (childBegun)
        {
          // Add button on the left with fixed width
          float addButtonWidth = LayoutConstants.ADD_BUTTON_WIDTH;
          if (ImGui.Button("+##add_terminal", new float2(addButtonWidth, tabHeight - 5.0f)))
          {
            _ = Task.Run(async () => await _sessionManager.CreateSessionAsync());
            _controller.ForceFocus();
          }

          if (ImGui.IsItemHovered())
          {
            ImGui.SetTooltip("Add new terminal session");
          }

          // Only show tabs if we have sessions
          if (sessions.Count > 0)
          {
            ImGui.SameLine();

            // Calculate remaining width for tab bar
            float remainingWidth = availableWidth - addButtonWidth - LayoutConstants.ELEMENT_SPACING;

            // Begin tab bar with remaining width
            if (ImGui.BeginTabBar("SessionTabs", ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.FittingPolicyScroll))
            {
              try
              {
                // Render each session as a tab
                foreach (var session in sessions)
                {
                  bool isActive = session == activeSession;

                  // Create tab label with session title and optional exit code
                  string tabLabel = session.Title;
                  if (session.ProcessManager.ExitCode.HasValue)
                  {
                    tabLabel += $" (Exit: {session.ProcessManager.ExitCode})";
                  }

                  // Use unique ID for each tab
                  string tabId = $"{tabLabel}##tab_{session.Id}";

                  // Use SetSelected flag only when active session just changed (to sync ImGui with SessionManager)
                  // This ensures programmatic session switches (File menu, Sessions menu) update the tab UI
                  // Without causing infinite loops from using SetSelected every frame
                  ImGuiTabItemFlags tabFlags = (isActive && activeSessionChanged)
                    ? ImGuiTabItemFlags.SetSelected
                    : ImGuiTabItemFlags.None;

                  bool tabOpen = true;
                  if (ImGui.BeginTabItem(tabId, ref tabOpen, tabFlags))
                  {
                    try
                    {
                      // If this tab is being rendered and it's not the current active session, switch to it
                      // This happens when user clicks the tab directly
                      // BUT: Don't switch if we just did a programmatic switch (activeSessionChanged),
                      // because ImGui's tab selection lags one frame behind our SessionManager state
                      if (!isActive && !activeSessionChanged)
                      {
                        _controller.SwitchToSessionAndFocus(session.Id);
                      }

                      // Tab content is handled by the terminal canvas, so we don't render content here
                      // The tab item just needs to exist to show the tab
                    }
                    finally
                    {
                      ImGui.EndTabItem();
                    }
                  }

                  // Handle tab close button (when tabOpen becomes false)
                  if (!tabOpen && sessions.Count > 1)
                  {
                    _ = Task.Run(async () => await _sessionManager.CloseSessionAsync(session.Id));
                  }

                  // Context menu for tab (right-click)
                  if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                  {
                    ImGui.OpenPopup($"tab_context_{session.Id}");
                  }

                  if (ImGui.BeginPopup($"tab_context_{session.Id}"))
                  {
                    if (ImGui.MenuItem("Close Tab") && sessions.Count > 1)
                    {
                      _ = Task.Run(async () => await _sessionManager.CloseSessionAsync(session.Id));
                    }

                    // Add restart option for terminated sessions
                    if (!session.ProcessManager.IsRunning && session.ProcessManager.ExitCode.HasValue)
                    {
                      if (ImGui.MenuItem("Restart Session"))
                      {
                        _ = Task.Run(async () =>
                        {
                          try
                          {
                            await _sessionManager.RestartSessionAsync(session.Id);
                          }
                          catch (Exception ex)
                          {
                            Console.WriteLine($"TerminalController: Failed to restart session {session.Id}: {ex.Message}");
                          }
                        });
                      }
                    }

                    if (ImGui.MenuItem("Rename Tab"))
                    {
                      // TODO: Implement tab renaming in future
                      ShowNotImplementedMessage("Tab renaming");
                    }
                    ImGui.EndPopup();
                  }
                }
              }
              finally
              {
                ImGui.EndTabBar();
              }
            }
          }
        }
      }
      finally
      {
        if (childBegun)
        {
          ImGui.EndChild();
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error rendering tab area: {ex.Message}");

      // Fallback: render a simple text indicator if tab rendering fails
      ImGui.Text("No sessions");
      ImGui.SameLine();
      if (ImGui.Button("+##fallback_add"))
      {
        _ = Task.Run(async () => await _sessionManager.CreateSessionAsync());
      }
    }
  }

  /// <summary>
  /// Shows a message for not-yet-implemented features.
  /// </summary>
  /// <param name="feature">The feature name to display</param>
  private void ShowNotImplementedMessage(string feature)
  {
    Console.WriteLine($"TerminalController: {feature} not implemented in this phase");
    // Future: Could show ImGui popup
  }
}
