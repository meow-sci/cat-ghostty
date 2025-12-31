using System;
using System.IO;
using System.Text.Json;

namespace caTTY.Display.Configuration;

/// <summary>
/// Configuration class for persisting theme and display settings.
/// Handles serialization to/from JSON configuration files.
/// </summary>
public class ThemeConfiguration
{
    /// <summary>
    /// Name of the currently selected theme.
    /// </summary>
    public string? SelectedThemeName { get; set; }

    /// <summary>
    /// Background opacity setting for terminal background colors (0.0 to 1.0).
    /// </summary>
    public float BackgroundOpacity { get; set; } = 1.0f;

    /// <summary>
    /// Foreground opacity setting for terminal text colors (0.0 to 1.0).
    /// </summary>
    public float ForegroundOpacity { get; set; } = 1.0f;


    public static string? OverrideAppDataDirectory { get; set; }

    /// <summary>
    /// Load configuration from the default configuration file.
    /// </summary>
    /// <returns>Loaded configuration or default configuration if file doesn't exist</returns>
    public static ThemeConfiguration Load()
    {
        try
        {
            var configPath = GetConfigFilePath();

            if (!File.Exists(configPath))
            {
                return new ThemeConfiguration();
            }

            var jsonContent = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<ThemeConfiguration>(jsonContent);

            return config ?? new ThemeConfiguration();
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
        {
            // Log error and return default configuration
            Console.WriteLine($"Error loading theme configuration: {ex.Message}");
            return new ThemeConfiguration();
        }
    }

    /// <summary>
    /// Save configuration to the default configuration file.
    /// </summary>
    public void Save()
    {
        try
        {
            var configPath = GetConfigFilePath();
            var configDirectory = Path.GetDirectoryName(configPath);

            // Ensure directory exists
            if (!string.IsNullOrEmpty(configDirectory) && !Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var jsonContent = JsonSerializer.Serialize(this, options);
            File.WriteAllText(configPath, jsonContent);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
        {
            Console.WriteLine($"Error saving theme configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the path to the configuration file.
    /// </summary>
    /// <returns>Full path to the configuration file</returns>
    private static string GetConfigFilePath()
    {
        var appDataPath = OverrideAppDataDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDirectory = Path.Combine(appDataPath, "caTTY");
        return Path.Combine(configDirectory, "theme-config.json");
    }
}
