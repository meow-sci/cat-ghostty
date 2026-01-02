using Brutal.ImGuiApi;
using KSA;
using StarMap.API;

namespace KsaExampleMod;

[StarMapMod]
public class SimpleMod
{
    public bool ImmediateUnload => false;

    [StarMapAfterGui]
    public void OnAfterUi(double dt)
    {
        // The original flags assignement - just to show some options
        // ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.MenuBar;

        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground |
                    ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.MenuBar;

        var vehicle = Program.ControlledVehicle;

        if (vehicle != null)
        {
            ImGui.Begin("MyWindow", flags);

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("Modone - Stats"))
                {
                    if (ImGui.MenuItem("Show Message 2")) Console.WriteLine("Hello from modone!");
                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }
 
            ImGui.Text($"Current Propellant Mass: {vehicle.FlightComputer.PropellantMass:F3}");
            // ImGui.Text($"Total Mass: {vehicle.FlightComputer.TotalMass}");
            ImGui.Text($"Total Acceleration: {vehicle.FlightComputer.TotalAcceleration}");
            ImGui.Text($"Current simulation Speed: {Universe.SimulationSpeed.ToString()}");
            // ImGui.Text($"Time: {Universe.GetElapsedSimTime().Days} days");
            if (ImGui.Button("Go go go!"))
            {
                Console.WriteLine("Going fast!");
                
                var rocket = Program.ControlledVehicle;

                rocket?.SetEnum(VehicleEngine.MainIgnite);
            // Access core systems
                // var fc = rocket.FlightComputer;
                // var plan = rocket.FlightPlan;
                

            }
            
            if (ImGui.Button("Whoa there!"))
            {
                
                Console.WriteLine("Slowing down");
                var rocket = KSA.Program.ControlledVehicle;

                rocket?.SetEnum(VehicleEngine.MainShutdown);
            }
            
            

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