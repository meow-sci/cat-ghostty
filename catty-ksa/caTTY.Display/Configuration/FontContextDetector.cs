using System;
using System.Linq;
using System.Reflection;

namespace caTTY.Display.Configuration;

/// <summary>
/// Utility class for detecting execution context and creating appropriate font configurations.
/// Uses assembly inspection to determine whether the application is running in TestApp or GameMod context.
/// </summary>
public static class FontContextDetector
{
    /// <summary>
    /// Detects the current execution context and creates an appropriate font configuration.
    /// Combines context detection with configuration creation for convenience.
    /// </summary>
    /// <returns>A TerminalFontConfig instance optimized for the detected execution context.</returns>
    public static TerminalFontConfig DetectAndCreateConfig()
    {
        var context = DetectExecutionContext();
        
        LogContextDetection(context);
        
        return context switch
        {
            ExecutionContext.TestApp => TerminalFontConfig.CreateForTestApp(),
            ExecutionContext.GameMod => TerminalFontConfig.CreateForGameMod(),
            _ => TerminalFontConfig.CreateForTestApp() // Safe default
        };
    }
    
    /// <summary>
    /// Detects the current execution context by inspecting loaded assemblies.
    /// Looks for KSA-related assemblies to determine if running in game mod context.
    /// </summary>
    /// <returns>The detected execution context.</returns>
    public static ExecutionContext DetectExecutionContext()
    {
        try
        {
            // Get all currently loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            // Check for KSA game assemblies
            var hasKsaAssemblies = assemblies.Any(assembly => 
                IsKsaRelatedAssembly(assembly));
            
            // Check for TestApp-specific assemblies
            var hasTestAppAssemblies = assemblies.Any(assembly => 
                IsTestAppRelatedAssembly(assembly));
            
            LogAssemblyInspection(assemblies, hasKsaAssemblies, hasTestAppAssemblies);
            
            if (hasKsaAssemblies)
            {
                return ExecutionContext.GameMod;
            }
            
            if (hasTestAppAssemblies)
            {
                return ExecutionContext.TestApp;
            }
            
            // Fallback: check entry assembly name
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var entryName = entryAssembly.GetName().Name;
                LogEntryAssemblyCheck(entryName);
                
                if (entryName?.Contains("TestApp", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return ExecutionContext.TestApp;
                }
                
                if (entryName?.Contains("GameMod", StringComparison.OrdinalIgnoreCase) == true ||
                    entryName?.Contains("KSA", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return ExecutionContext.GameMod;
                }
            }
            
            return ExecutionContext.Unknown;
        }
        catch (Exception ex)
        {
            LogDetectionError(ex);
            return ExecutionContext.Unknown;
        }
    }
    
    /// <summary>
    /// Determines if an assembly is related to the KSA game environment.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>True if the assembly appears to be KSA-related, false otherwise.</returns>
    private static bool IsKsaRelatedAssembly(Assembly assembly)
    {
        try
        {
            var assemblyName = assembly.GetName().Name;
            if (string.IsNullOrEmpty(assemblyName))
                return false;
            
            // Check for KSA-specific assembly names
            return assemblyName.Contains("KSA", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.Contains("Kitten", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.Contains("Space", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.Contains("Agency", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.Contains("BRUTAL", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.Contains("StarMap", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // If we can't inspect the assembly, assume it's not KSA-related
            return false;
        }
    }
    
    /// <summary>
    /// Determines if an assembly is related to the TestApp environment.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>True if the assembly appears to be TestApp-related, false otherwise.</returns>
    private static bool IsTestAppRelatedAssembly(Assembly assembly)
    {
        try
        {
            var assemblyName = assembly.GetName().Name;
            if (string.IsNullOrEmpty(assemblyName))
                return false;
            
            // Check for TestApp-specific assembly names
            return assemblyName.Contains("TestApp", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.Contains("Playground", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // If we can't inspect the assembly, assume it's not TestApp-related
            return false;
        }
    }
    
    /// <summary>
    /// Logs the detected context for debugging purposes.
    /// </summary>
    /// <param name="context">The detected execution context.</param>
    private static void LogContextDetection(ExecutionContext context)
    {
        try
        {
            Console.WriteLine($"FontContextDetector: Detected execution context: {context}");
        }
        catch
        {
            // Ignore logging errors to prevent crashes
        }
    }
    
    /// <summary>
    /// Logs assembly inspection results for debugging purposes.
    /// </summary>
    /// <param name="assemblies">All loaded assemblies.</param>
    /// <param name="hasKsaAssemblies">Whether KSA assemblies were found.</param>
    /// <param name="hasTestAppAssemblies">Whether TestApp assemblies were found.</param>
    private static void LogAssemblyInspection(Assembly[] assemblies, bool hasKsaAssemblies, bool hasTestAppAssemblies)
    {
        try
        {
            Console.WriteLine($"FontContextDetector: Inspected {assemblies.Length} assemblies");
            Console.WriteLine($"FontContextDetector: Found KSA assemblies: {hasKsaAssemblies}");
            Console.WriteLine($"FontContextDetector: Found TestApp assemblies: {hasTestAppAssemblies}");
            
            // Log specific KSA and TestApp assemblies found
            var ksaAssemblies = assemblies.Where(IsKsaRelatedAssembly).Select(a => a.GetName().Name).ToArray();
            var testAppAssemblies = assemblies.Where(IsTestAppRelatedAssembly).Select(a => a.GetName().Name).ToArray();
            
            if (ksaAssemblies.Length > 0)
            {
                Console.WriteLine($"FontContextDetector: KSA assemblies: {string.Join(", ", ksaAssemblies)}");
            }
            
            if (testAppAssemblies.Length > 0)
            {
                Console.WriteLine($"FontContextDetector: TestApp assemblies: {string.Join(", ", testAppAssemblies)}");
            }
        }
        catch
        {
            // Ignore logging errors to prevent crashes
        }
    }
    
    /// <summary>
    /// Logs entry assembly inspection for debugging purposes.
    /// </summary>
    /// <param name="entryName">The name of the entry assembly.</param>
    private static void LogEntryAssemblyCheck(string? entryName)
    {
        try
        {
            Console.WriteLine($"FontContextDetector: Entry assembly name: {entryName ?? "null"}");
        }
        catch
        {
            // Ignore logging errors to prevent crashes
        }
    }
    
    /// <summary>
    /// Logs detection errors for debugging purposes.
    /// </summary>
    /// <param name="ex">The exception that occurred during detection.</param>
    private static void LogDetectionError(Exception ex)
    {
        try
        {
            Console.WriteLine($"FontContextDetector: Error during context detection: {ex.Message}");
        }
        catch
        {
            // Ignore logging errors to prevent crashes
        }
    }
}