using System;
using Brutal.GlfwApi;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Core;
using KSA;
using RenderCore;

public static class StandaloneImGui
{
  private static GlfwWindow? window;
  private static Renderer? renderer;
  private static RenderPassState rstate = default!;
  private static Action? OnDrawUi;

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
    window = Glfw.CreateWindow(new()
    {
      Title = "ImGui Test",
      Size = new int2(1500, 1500),
    });

    renderer = new Renderer(window, VkFormat.Undefined, VkPresentModeKHR.FifoKHR, VulkanHelpers.Api.VERSION_1_3);

    rstate = new RenderPassState
    {
      ClearValues = [new VkClearColorValue() { Float32 = Color.Black.AsFloat4 }]
    };


    ImGui.CreateContext();
    ImGuiBackend.Initialize(window, renderer);

    var io = ImGui.GetIO();
    io.IniSavingRate = 0f; // disable imgui.ini state saving

    // This requires the working directory to be set to the KSA install (or the cwd having a Content folder with at least one ttf)
    KSA.Program.ConsoleWindow = new(); // so fontmanager doesn't throw
    FontManager.Initialize(renderer.Device);
  }

  private static void OnFrame()
  {
    Glfw.PollEvents();
    ImGuiBackend.NewFrame();
    ImGui.NewFrame();

    // call custom ui code
    Console.WriteLine($"scale: {window!.ContentScale[0]}");
    OnDrawUi!();

    ImGui.Render();

    var (result, frame) = renderer!.TryAcquireNextFrame();
    if (result != FrameResult.Success)
    {
      renderer.Rebuild(VkPresentModeKHR.FifoKHR);
      renderer.Device.WaitIdle();
      return;
    }

    var (resources, commandBuffer) = frame;

    commandBuffer.Begin(VkCommandBufferUsageFlags.OneTimeSubmitBit);
    commandBuffer.BeginRenderPass(new VkRenderPassBeginInfo()
    {
      RenderPass = renderer.MainRenderPass,
      Framebuffer = resources.Framebuffer,
      RenderArea = new(renderer.Extent),
      ClearValues = rstate.ClearValues.Ptr,
      ClearValueCount = 1,
    }, VkSubpassContents.Inline);

    ImGuiBackend.Vulkan.RenderDrawData(commandBuffer);

    commandBuffer.EndRenderPass();
    commandBuffer.End();

    renderer.TrySubmitFrame();
  }
}