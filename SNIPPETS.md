# example of imgui rendering some text

from tom_is_unlucky (https://discord.com/channels/1439383096813158702/1439669506464157796/1441838286678261854)

```csharp
    public static bool DrawUi(Vehicle vehicle, Viewport inViewport, Astronomical.UiContext uiContext)
    {
      if (vehicle != KSA.Program.ControlledVehicle)
        return false;
      if (!ImGui.Begin($"KSASM##KSASM-{vehicle.Id}", WINDOW_FLAGS))
      {
        ImGui.End();
        return false;
      }
      if (ImGui.BeginCombo("##Library", "Load Library Script"))
      {
        for (var i = 0; i < Library.Index.Count; i++)
        {
          var name = Library.Index[i];
          ImGui.PushID(i);
          if (ImGui.Selectable(name))
            LoadLibrary(name);
          ImGui.PopID();
        }
        ImGui.EndCombo();
      }
      ImGui.InputTextMultiline(
        "###source",
        buffer.AsSpan(),
        new float2(600, 400),
        ImGuiInputTextFlags.None
      );
      isTyping = ImGui.IsItemActive();
      Step(vehicle);
      if (ImGui.Button("Assemble##asm"))
        Assemble();
      ImGui.SameLine();
      if (ImGui.Button("Run##run"))
        Restart();
      ImGui.SameLine();
      doStep = ImGui.Button("Step##step");
      ImGui.SameLine();
      if (ImGui.Button("Stop##stop"))
        Stop();
      ImGui.SameLine(); ImGui.Text(stats);
      foreach (var line in output)
        ImGui.Text(line);
      if ((output.Count > 0 || stats.Length > 0) && ImGui.Button("Clear##clear"))
      {
        stats = "";
        output.Clear();
      }
      ImGui.End();
      return false;
    }
```