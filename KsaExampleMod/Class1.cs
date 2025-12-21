using Brutal.ImGuiApi;
using StarMap.API;
using KSA;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using System.Reflection;
using Microsoft.VisualBasic;

namespace KsaExampleMod
{
    [StarMapMod]
    public class SimpleMod
    {
        public bool ImmediateUnload => false;

        [StarMapAfterGui]
        public void OnAfterUi(double dt)
        {
            // The original flags assignement - just to show some options
            // ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.MenuBar;

            ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.MenuBar;

            Vehicle? vehicle = Program.ControlledVehicle;

            if (vehicle != null)
                {
                ImGui.Begin("MyWindow", flags);

                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("Modone - Stats"))
                    {
                        if (ImGui.MenuItem("Show Message 2"))
                        {
                            Console.WriteLine("Hello from modone!");
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.EndMenuBar();
                }
                ImGui.Text($"Current Propellant Mass: {vehicle.FlightComputer.PropellantMass:F3}");
                // ImGui.Text($"Total Mass: {vehicle.FlightComputer.TotalMass}");
                ImGui.Text($"Total Acceleration: {vehicle.FlightComputer.TotalAcceleration}");
                ImGui.Text($"Current simulation Speed: {Universe.SimulationSpeed.ToString()}");
                ImGui.Text($"Time: {Universe.GetElapsedSimTime().Days} days");

                ImGui.End();
            }
        }

        [StarMapBeforeGui]
        public void OnBeforeUi(double dt)
        {
            
        }

        [StarMapAllModsLoaded]
        public void OnFullyLoaded()
        {
            Patcher.Patch();
        }

        [StarMapImmediateLoad]
        public void OnImmediatLoad()
        {
        }

        [StarMapUnload]
        public void Unload()
        {
            Patcher.Unload();
        }
    }
}
