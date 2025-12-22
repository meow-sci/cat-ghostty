using Brutal.ImGuiApi;

public static class Program2
{
  public static void Main() => StandaloneImGui.Run(OnDrawUi);

  private static void OnDrawUi()
  {
    ImGui.Begin("Test");
    ImGui.Text("TESTING 123");
    ImGui.End();
  }
}