using System.Runtime.InteropServices;
using Brutal.ImGuiApi;
using Brutal.Numerics;


namespace caTTY.Playground.Rendering;

/// <summary>
/// Screen scaling utilities for ImGui applications on Windows.
/// Fixes mouse location issues when Windows display scaling is enabled.
/// </summary>
public static class ImGuiScreenScaling
{
  
  [DllImport("user32.dll")]
  private static extern IntPtr GetForegroundWindow();

  [DllImport("user32.dll")]
  private static extern uint GetDpiForWindow(IntPtr hwnd);

  [DllImport("user32.dll")]
  private static extern bool GetCursorPos(out POINT lpPoint);

  [DllImport("user32.dll")]
  private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

  [StructLayout(LayoutKind.Sequential)]
  private struct POINT
  {
    public int X;
    public int Y;
  }

  public static void FixMouseLocationForScreenScaling()
  {
    // Fix: compute mouse position in logical (DPI-scaled) client coordinates and feed to ImGui
    try
    {
      var io = ImGui.GetIO();
      if (GetCursorPos(out POINT pt))
      {
        var hwnd = GetForegroundWindow();
        if (hwnd != IntPtr.Zero)
        {
          try { ScreenToClient(hwnd, ref pt); } catch { }
          uint dpi = 96;
          try { dpi = GetDpiForWindow(hwnd); } catch { }
          float scale = dpi > 0 ? dpi / 96f : 1f;
          io.MousePos = new float2(pt.X / scale, pt.Y / scale);
        }
      }
    }
    catch { }
  }

}