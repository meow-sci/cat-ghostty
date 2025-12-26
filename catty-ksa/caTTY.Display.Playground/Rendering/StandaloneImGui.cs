using Brutal.GlfwApi;
using Brutal.ImGuiApi;
using Brutal.ImGuiApi.Abstractions;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Core;
using KSA;
using RenderCore;
using ImGui = Brutal.ImGuiApi.ImGui;

namespace caTTY.Playground.Rendering;

/// <summary>
///     Standalone ImGui context for terminal rendering experiments.
///     Based on the KSA ImGui test application pattern.
/// </summary>
public static class StandaloneImGui
{
    private static GlfwWindow? window;
    private static Renderer? renderer;
    private static RenderPassState? rstate;
    private static Action? OnDrawUi;

    /// <summary>
    ///     Run the ImGui application loop
    /// </summary>
    public static void Run(Action onDrawUi)
    {
        OnDrawUi = onDrawUi;
        Init();

        while (!window!.ShouldClose)
        {
            OnFrame();
        }
    }

    private static void Init()
    {
        Glfw.Init();

        Glfw.WindowHint(GlfwWindowHint.ClientApi, 0);
        Glfw.WindowHint(GlfwWindowHint.AutoIconify, 0);
        Glfw.WindowHint(GlfwWindowHint.FocusOnShow, 1);
        window = Glfw.CreateWindow(new GlfwWindow.CreateInfo { Title = "ImGui Test", Size = new int2(2000, 1200) });

        renderer = new Renderer(window, VkFormat.D32SFloat, VkPresentModeKHR.FifoKHR, VulkanHelpers.Api.VERSION_1_3);

        rstate = new RenderPassState
        {
            Pass = renderer.MainRenderPass,
            SampleCount = VkSampleCountFlags._1Bit,
            ClearValues =
            [
                new VkClearColorValue { Float32 = Color.Black.AsFloat4 },
                new VkClearDepthStencilValue { Depth = 0 }
            ]
        };

        ImGui.CreateContext();

        ImGuiIOPtr io = ImGui.GetIO();
        io.ConfigDpiScaleFonts = true;
        io.ConfigDpiScaleViewports = true;
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        io.IniFilename = null;
        io.IniSavingRate = 0;

        ImGuiBackend.Initialize(window, renderer);

        // This requires the working directory to be set to the KSA install (or the cwd having a Content folder with at least one ttf)
        KSA.Program.ConsoleWindow = new ConsoleWindow(); // required so fontmanager doesn't throw
        FontManager.Initialize(renderer.Device);
        Console.WriteLine("after fonts");
    }

    private static void OnFrame()
    {
        Glfw.PollEvents();
        ImGuiBackend.NewFrame();
        ImGui.NewFrame();
        ImGuiHelper.StartFrame();

        OnDrawUi!();

        ImGui.Render();
        (FrameResult result, AcquiredFrame frame) = renderer!.TryAcquireNextFrame();
        if (result != FrameResult.Success)
        {
            RebuildRenderer();
            (result, frame) = renderer!.TryAcquireNextFrame();
        }

        if (result != FrameResult.Success)
        {
            throw new InvalidOperationException($"{result}");
        }

        (FrameResources resources, CommandBuffer commandBuffer) = frame;
        var begin = new VkRenderPassBeginInfo
        {
            RenderPass = renderer!.MainRenderPass,
            Framebuffer = resources.Framebuffer,
            RenderArea = new VkRect2D(renderer.Extent),
            ClearValues = rstate!.ClearValues.Ptr,
            ClearValueCount = 2
        };

        commandBuffer.Reset();
        commandBuffer.Begin(VkCommandBufferUsageFlags.OneTimeSubmitBit);
        commandBuffer.BeginRenderPass(in begin, VkSubpassContents.Inline);
        ImGuiBackend.Vulkan.RenderDrawData(commandBuffer);
        commandBuffer.EndRenderPass();
        commandBuffer.End();

        FrameResult frameResult = renderer.TrySubmitFrame();
        ImGui.UpdatePlatformWindows();
        ImGui.RenderPlatformWindowsDefault();
        if (frameResult != FrameResult.Success)
        {
            RebuildRenderer();
        }
    }

    private static void RebuildRenderer()
    {
        renderer!.Rebuild(VkPresentModeKHR.FifoKHR);
        renderer!.Device.WaitIdle();
        rstate!.Pass = renderer!.MainRenderPass;
    }
}
