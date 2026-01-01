using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using caTTY.Core.Terminal;

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

    /// <summary>
    /// Default shell type for new terminal sessions.
    /// </summary>
    [JsonIgnore]
    public ShellType DefaultShellType { get; set; } = ShellType.PowerShell;

    /// <summary>
    /// String representation of DefaultShellType for JSON serialization.
    /// This ensures stability across enum changes.
    /// </summary>
    [JsonPropertyName("DefaultShellType")]
    [JsonConverter(typeof(ShellTypeJsonConverter))]
    public string DefaultShellTypeString
    {
        get => DefaultShellType.ToString();
        set
        {
            if (Enum.TryParse<ShellType>(value, true, out var shellType))
            {
                DefaultShellType = shellType;
            }
            else
            {
                // Fallback to WSL if parsing fails (e.g., unknown shell type from future version)
                DefaultShellType = ShellType.Wsl;
            }
        }
    }

    /// <summary>
    /// Custom shell path when DefaultShellType is Custom.
    /// </summary>
    public string? CustomShellPath { get; set; }

    /// <summary>
    /// Additional arguments for the default shell.
    /// </summary>
    public List<string> DefaultShellArguments { get; set; } = new();

    /// <summary>
    /// WSL distribution name when DefaultShellType is Wsl.
    /// </summary>
    public string? WslDistribution { get; set; }

    /// <summary>
    /// Creates ProcessLaunchOptions based on the current configuration.
    /// </summary>
    /// <returns>ProcessLaunchOptions configured according to current settings</returns>
    public ProcessLaunchOptions CreateLaunchOptions()
    {
        return DefaultShellType switch
        {
            ShellType.Wsl => string.IsNullOrEmpty(WslDistribution) 
                ? ProcessLaunchOptions.CreateWsl() 
                : ProcessLaunchOptions.CreateWsl(WslDistribution),
            ShellType.PowerShell => ProcessLaunchOptions.CreatePowerShell(),
            ShellType.PowerShellCore => ProcessLaunchOptions.CreatePowerShellCore(),
            ShellType.Cmd => ProcessLaunchOptions.CreateCmd(),
            ShellType.Custom when !string.IsNullOrEmpty(CustomShellPath) => 
                ProcessLaunchOptions.CreateCustom(CustomShellPath, DefaultShellArguments.ToArray()),
            _ => ProcessLaunchOptions.CreateDefault()
        };
    }

    /// <summary>
    /// Gets a display name for the current shell configuration.
    /// </summary>
    /// <returns>Human-readable shell configuration name</returns>
    public string GetShellDisplayName()
    {
        return DefaultShellType switch
        {
            ShellType.Wsl => string.IsNullOrEmpty(WslDistribution) 
                ? "WSL2 (Default)" 
                : $"WSL2 ({WslDistribution})",
            ShellType.PowerShell => "Windows PowerShell",
            ShellType.PowerShellCore => "PowerShell Core",
            ShellType.Cmd => "Command Prompt",
            ShellType.Custom when !string.IsNullOrEmpty(CustomShellPath) => 
                $"Custom ({Path.GetFileName(CustomShellPath)})",
            _ => "Auto-detect"
        };
    }


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

/// <summary>
/// Custom JSON converter for ShellType that handles both string and numeric values.
/// This provides backward compatibility with old numeric enum serialization.
/// </summary>
public class ShellTypeJsonConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                // New string format - return as-is
                return reader.GetString() ?? "Wsl";
                
            case JsonTokenType.Number:
                // Old numeric format - convert to string
                var numericValue = reader.GetInt32();
                if (Enum.IsDefined(typeof(ShellType), numericValue))
                {
                    return ((ShellType)numericValue).ToString();
                }
                // Unknown numeric value - fallback to WSL
                return "Wsl";
                
            default:
                // Invalid format - fallback to WSL
                return "Wsl";
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        // Always write as string
        writer.WriteStringValue(value);
    }
}
