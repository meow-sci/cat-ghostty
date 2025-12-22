using Brutal.ImGuiApi;
using Brutal.Numerics;

public static class Program2
{
  public static void Main()
  {
    Console.WriteLine("Starting Program2...");
    StandaloneImGui.Run(OnDrawUi);
  }


  private static void OnDrawUi()
  {

    ImGui.SetNextWindowPos(new float2(0, 0), ImGuiCond.FirstUseEver);
    ImGui.Begin("Debug", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);
    ImGui.Text("Hello from KsaImGuiTestWin!");
    ImGui.End();

    // not quite right, hard-coding a value that does
    // float titleBarHeight = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2;
    float titleBarHeight = 22;


    ImGui.SetNextWindowPos(new float2(0, 80 + titleBarHeight), ImGuiCond.FirstUseEver);
    ImGui.SetNextWindowSize(new float2(100, titleBarHeight), ImGuiCond.FirstUseEver);
    ImGui.Begin("Target", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground);


    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
    float2 winPos = ImGui.GetWindowPos();

    drawList.AddRectFilled(
      winPos + new float2(0, 0),
      winPos + new float2(100, titleBarHeight),
      0xFF0000FF // RGBA: Red, fully opaque (ImGui uses ABGR)
    );
    ImGui.End();


    ImGui.SetNextWindowPos(new float2(0, 200), ImGuiCond.FirstUseEver);
    ImGui.SetNextWindowSize(new float2(200, 200), ImGuiCond.FirstUseEver);
    ImGui.Begin("TestWin");
    ImGui.Text("Testing 123");
    ImGui.End();
  }
}