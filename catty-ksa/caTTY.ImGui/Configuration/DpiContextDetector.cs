using System;
using System.Linq;
using System.Reflection;
using Brutal.ImGuiApi;
using BrutalImGui = Brutal.ImGuiApi.ImGui;

namespace caTTY.ImGui.Configuration;

/// <summary>
/// Enumeration of execution contexts for DPI scaling detection.
/// </summary>
public enum ExecutionContext
{
    /// <summary>
    /// Running in the standalone TestApp context with proper DPI awareness.
    /// </summary>
    TestApp,

    /// <summary>
    /// Running in the GameMod context which inherits the game's DPI context.
    /// </summary>
    GameMod,

    /// <summary>
    /// Unknown execution context - unable to determine the environment.
    /// </summary>
    Unknown
}

/// <summary>
/// Utility class for detecting DPI scaling context and creating appropriate terminal rendering configurations.
/// This class analyzes the execution environment to determine whether the application is running as a
/// standalone TestApp or as a GameMod within the KSA game engine.
/// </summary>
public static class DpiContextDetector
{
    /// <summary>
    /// Detects the execution context and creates an appropriate terminal rendering configuration.
    /// This method combines context detection, DPI scaling detection, and configuration creation
    /// into a single convenient method.
    /// </summary>
    /// <returns>A TerminalRenderingConfig optimized for the detected execution context.</returns>
    public static TerminalRenderingConfig DetectAndCreateConfig()
    {
        var context = DetectExecutionContext();
        var dpiScale = DetectDpiScaling();

        LogDetectionResults(context, dpiScale);

        return context switch
        {
            ExecutionContext.TestApp => TerminalRenderingConfig.CreateForTestApp(),
            ExecutionContext.GameMod => TerminalRenderingConfig.CreateForGameMod(dpiScale),
            ExecutionContext.Unknown => CreateFallbackConfig(dpiScale),
            _ => TerminalRenderingConfig.CreateDefault()
        };
    }

    /// <summary>
    /// Detects the current execution context by analyzing loaded assemblies and execution environment.
    /// Uses assembly inspection to determine if the application is running within the KSA game engine
    /// or as a standalone application.
    /// </summary>
    /// <returns>The detected execution context.</returns>
    public static ExecutionContext DetectExecutionContext()
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            // Check for KSA-specific assemblies that indicate GameMod context
            var hasKsaAssemblies = assemblies.Any(assembly =>
            {
                var name = assembly.FullName ?? string.Empty;
                return name.Contains("KSA", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("StarMap", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Planet.Core", StringComparison.OrdinalIgnoreCase);
            });

            if (hasKsaAssemblies)
            {
                Console.WriteLine("DpiContextDetector: Detected GameMod context (KSA assemblies found)");
                return ExecutionContext.GameMod;
            }

            // Check for TestApp-specific indicators
            var hasTestAppAssemblies = assemblies.Any(assembly =>
            {
                var name = assembly.FullName ?? string.Empty;
                return name.Contains("caTTY.TestApp", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("caTTY.ImGui.Playground", StringComparison.OrdinalIgnoreCase);
            });

            if (hasTestAppAssemblies)
            {
                Console.WriteLine("DpiContextDetector: Detected TestApp context");
                return ExecutionContext.TestApp;
            }

            // Additional heuristics: check entry assembly
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var entryName = entryAssembly.FullName ?? string.Empty;
                if (entryName.Contains("caTTY.TestApp", StringComparison.OrdinalIgnoreCase) ||
                    entryName.Contains("caTTY.ImGui.Playground", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("DpiContextDetector: Detected TestApp context from entry assembly");
                    return ExecutionContext.TestApp;
                }
            }

            Console.WriteLine("DpiContextDetector: Unable to determine execution context");
            return ExecutionContext.Unknown;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DpiContextDetector: Error detecting execution context: {ex.Message}");
            return ExecutionContext.Unknown;
        }
    }

    /// <summary>
    /// Detects the current DPI scaling factor using ImGui context and system fallbacks.
    /// Attempts to query the ImGui display framebuffer scale, falling back to common
    /// scaling factors if ImGui context is unavailable.
    /// </summary>
    /// <returns>The detected DPI scaling factor (typically 1.0, 1.25, 1.5, 2.0, etc.).</returns>
    public static float DetectDpiScaling()
    {
        try
        {
            // Use reflection to safely call BrutalImGui.GetIO() without causing assembly loading issues
            return DetectDpiScalingViaReflection();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DpiContextDetector: ImGui context unavailable ({ex.Message}), using fallback detection");
            return DetectSystemDpiScaling();
        }
    }

    /// <summary>
    /// Uses reflection to safely detect DPI scaling without causing assembly loading exceptions.
    /// </summary>
    /// <returns>The detected DPI scaling factor.</returns>
    private static float DetectDpiScalingViaReflection()
    {
        try
        {
            // Try to load the Brutal.ImGui assembly
            var brutalImGuiAssembly = Assembly.LoadFrom("Brutal.ImGui.dll");
            var imGuiType = brutalImGuiAssembly.GetType("Brutal.ImGuiApi.ImGui");
            
            if (imGuiType == null)
            {
                Console.WriteLine("DpiContextDetector: Could not find ImGui type, using fallback");
                return DetectSystemDpiScaling();
            }

            // Get the GetIO method
            var getIOMethod = imGuiType.GetMethod("GetIO", BindingFlags.Public | BindingFlags.Static);
            if (getIOMethod == null)
            {
                Console.WriteLine("DpiContextDetector: Could not find GetIO method, using fallback");
                return DetectSystemDpiScaling();
            }

            // Call GetIO()
            var io = getIOMethod.Invoke(null, null);
            if (io == null)
            {
                Console.WriteLine("DpiContextDetector: GetIO returned null, using fallback");
                return DetectSystemDpiScaling();
            }

            // Get the DisplayFramebufferScale property
            var displayFramebufferScaleProperty = io.GetType().GetProperty("DisplayFramebufferScale");
            if (displayFramebufferScaleProperty == null)
            {
                Console.WriteLine("DpiContextDetector: Could not find DisplayFramebufferScale property, using fallback");
                return DetectSystemDpiScaling();
            }

            var scale = displayFramebufferScaleProperty.GetValue(io);
            if (scale == null)
            {
                Console.WriteLine("DpiContextDetector: DisplayFramebufferScale is null, using fallback");
                return DetectSystemDpiScaling();
            }

            // Get the X component (assuming it's a Vector2-like structure)
            var xProperty = scale.GetType().GetProperty("X");
            if (xProperty == null)
            {
                Console.WriteLine("DpiContextDetector: Could not find X property on DisplayFramebufferScale, using fallback");
                return DetectSystemDpiScaling();
            }

            var xValue = xProperty.GetValue(scale);
            if (xValue is float scaleX && scaleX > 1.0f)
            {
                Console.WriteLine($"DpiContextDetector: Detected DPI scaling from ImGui: {scaleX:F2}x");
                return scaleX;
            }

            Console.WriteLine("DpiContextDetector: ImGui reports no DPI scaling (1.0x)");
            return 1.0f;
        }
        catch (System.IO.FileNotFoundException)
        {
            Console.WriteLine("DpiContextDetector: BRUTAL ImGui assembly not found (test environment), using fallback detection");
            return DetectSystemDpiScaling();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DpiContextDetector: Reflection-based detection failed ({ex.Message}), using fallback");
            return DetectSystemDpiScaling();
        }
    }

    /// <summary>
    /// Detects DPI scaling using system-level APIs as a fallback when ImGui context is unavailable.
    /// This method provides reasonable defaults based on common DPI scaling scenarios.
    /// </summary>
    /// <returns>The estimated DPI scaling factor.</returns>
    private static float DetectSystemDpiScaling()
    {
        try
        {
            // On Windows, we could use GetDpiForWindow or similar APIs
            // For now, we'll use a reasonable default for GameMod context
            // since this is typically called when ImGui context isn't available
            
            // Common DPI scaling factors: 1.0 (100%), 1.25 (125%), 1.5 (150%), 2.0 (200%)
            // Default to 2.0x for GameMod scenarios where ImGui context detection fails
            const float fallbackScale = 2.0f;
            
            Console.WriteLine($"DpiContextDetector: Using fallback DPI scaling: {fallbackScale:F1}x");
            return fallbackScale;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DpiContextDetector: System DPI detection failed ({ex.Message}), using default 2.0x");
            return 2.0f;
        }
    }

    /// <summary>
    /// Creates a fallback configuration when the execution context cannot be determined.
    /// Uses conservative settings that should work reasonably well in most scenarios.
    /// </summary>
    /// <param name="dpiScale">The detected DPI scaling factor.</param>
    /// <returns>A fallback TerminalRenderingConfig.</returns>
    private static TerminalRenderingConfig CreateFallbackConfig(float dpiScale)
    {
        Console.WriteLine($"DpiContextDetector: Creating fallback configuration with DPI scale {dpiScale:F1}x");
        
        // If DPI scaling is detected, assume we're in a GameMod-like context
        if (dpiScale > 1.1f)
        {
            return TerminalRenderingConfig.CreateForGameMod(dpiScale);
        }
        
        // Otherwise, use TestApp configuration
        return TerminalRenderingConfig.CreateForTestApp();
    }

    /// <summary>
    /// Logs the detection results for debugging purposes.
    /// Provides comprehensive information about the detected context and DPI scaling.
    /// </summary>
    /// <param name="context">The detected execution context.</param>
    /// <param name="dpiScale">The detected DPI scaling factor.</param>
    private static void LogDetectionResults(ExecutionContext context, float dpiScale)
    {
        Console.WriteLine("=== DPI Context Detection Results ===");
        Console.WriteLine($"Execution Context: {context}");
        Console.WriteLine($"DPI Scaling Factor: {dpiScale:F2}x");
        
        var expectedConfig = context switch
        {
            ExecutionContext.TestApp => "Standard metrics (16.0f font, 9.6f width, 18.0f height)",
            ExecutionContext.GameMod => $"Compensated metrics ({16.0f / dpiScale:F1}f font, {9.6f / dpiScale:F1}f width, {18.0f / dpiScale:F1}f height)",
            ExecutionContext.Unknown => dpiScale > 1.1f ? "GameMod-style compensated metrics" : "TestApp-style standard metrics",
            _ => "Default configuration"
        };
        
        Console.WriteLine($"Configuration: {expectedConfig}");
        Console.WriteLine("=====================================");
    }

    /// <summary>
    /// Gets diagnostic information about the current execution environment.
    /// This method provides detailed information for troubleshooting DPI detection issues.
    /// </summary>
    /// <returns>A string containing diagnostic information.</returns>
    public static string GetDiagnosticInfo()
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var entryAssembly = Assembly.GetEntryAssembly();
            
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== DPI Context Diagnostic Information ===");
            info.AppendLine($"Entry Assembly: {entryAssembly?.FullName ?? "Unknown"}");
            info.AppendLine($"Total Loaded Assemblies: {assemblies.Length}");
            
            info.AppendLine("\nKSA-related Assemblies:");
            var ksaAssemblies = assemblies.Where(a => 
                a.FullName?.Contains("KSA", StringComparison.OrdinalIgnoreCase) == true ||
                a.FullName?.Contains("StarMap", StringComparison.OrdinalIgnoreCase) == true ||
                a.FullName?.Contains("Planet", StringComparison.OrdinalIgnoreCase) == true).ToList();
            
            if (ksaAssemblies.Any())
            {
                foreach (var assembly in ksaAssemblies)
                {
                    info.AppendLine($"  - {assembly.GetName().Name}");
                }
            }
            else
            {
                info.AppendLine("  - None found");
            }
            
            info.AppendLine("\ncaTTY-related Assemblies:");
            var cattyAssemblies = assemblies.Where(a => 
                a.FullName?.Contains("caTTY", StringComparison.OrdinalIgnoreCase) == true).ToList();
            
            foreach (var assembly in cattyAssemblies)
            {
                info.AppendLine($"  - {assembly.GetName().Name}");
            }
            
            // Check if BRUTAL ImGui is available using reflection
            try
            {
                var brutalImGuiAssembly = Assembly.LoadFrom("Brutal.ImGui.dll");
                var imGuiType = brutalImGuiAssembly.GetType("Brutal.ImGuiApi.ImGui");
                var getIOMethod = imGuiType?.GetMethod("GetIO", BindingFlags.Public | BindingFlags.Static);
                
                if (getIOMethod != null)
                {
                    var io = getIOMethod.Invoke(null, null);
                    var displayFramebufferScaleProperty = io?.GetType().GetProperty("DisplayFramebufferScale");
                    var scale = displayFramebufferScaleProperty?.GetValue(io);
                    var xProperty = scale?.GetType().GetProperty("X");
                    var yProperty = scale?.GetType().GetProperty("Y");
                    
                    if (xProperty != null && yProperty != null)
                    {
                        var xValue = xProperty.GetValue(scale);
                        var yValue = yProperty.GetValue(scale);
                        info.AppendLine($"\nImGui Display Scale: {xValue:F2}x, {yValue:F2}x");
                    }
                    else
                    {
                        info.AppendLine("\nImGui Context: Available but scale properties not accessible");
                    }
                }
                else
                {
                    info.AppendLine("\nImGui Context: Assembly found but GetIO method not accessible");
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                info.AppendLine("\nImGui Context: Unavailable (BRUTAL ImGui assembly not found - test environment)");
            }
            catch (Exception ex)
            {
                info.AppendLine($"\nImGui Context: Unavailable ({ex.Message})");
            }
            
            info.AppendLine("==========================================");
            return info.ToString();
        }
        catch (Exception ex)
        {
            return $"Error generating diagnostic info: {ex.Message}";
        }
    }
}