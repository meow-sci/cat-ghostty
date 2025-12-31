using System;
using System.IO;
using System.Text.Json;
using caTTY.Display.Configuration;
using NUnit.Framework;

namespace caTTY.Display.Tests.Unit.Configuration;

/// <summary>
/// Unit tests for ThemeConfiguration class.
/// Tests specific error handling scenarios and edge cases.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ThemeConfigurationTests
{
    private string _tempConfigDirectory = null!;
    private string _originalAppData = null!;

    [SetUp]
    public void SetUp()
    {
        // Create a temporary directory for test configuration files
        _tempConfigDirectory = Path.Combine(Path.GetTempPath(), $"catty_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempConfigDirectory);

        // Store original AppData path and set temporary one for testing
        _originalAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        
        // We can't easily override Environment.GetFolderPath, so we'll test with actual paths
        // but clean up afterwards
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up temporary directory
        if (Directory.Exists(_tempConfigDirectory))
        {
            try
            {
                Directory.Delete(_tempConfigDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Clean up any test config files in actual AppData
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDirectory = Path.Combine(appDataPath, "caTTY");
            var configFile = Path.Combine(configDirectory, "theme-config.json");
            
            if (File.Exists(configFile))
            {
                var content = File.ReadAllText(configFile);
                // Only delete if it looks like a test file
                if (content.Contains("TestTheme") || content.Contains("0.123"))
                {
                    File.Delete(configFile);
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Test missing configuration file scenarios.
    /// Requirements: 6.2, 6.3
    /// </summary>
    [Test]
    public void Load_WhenConfigurationFileDoesNotExist_ShouldReturnDefaultConfiguration()
    {
        // Ensure no config file exists by using a non-existent directory structure
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDirectory = Path.Combine(appDataPath, "caTTY");
        var configFile = Path.Combine(configDirectory, "theme-config.json");

        // Delete config file if it exists
        if (File.Exists(configFile))
        {
            File.Delete(configFile);
        }

        // Load configuration
        var config = ThemeConfiguration.Load();

        // Should return default configuration
        Assert.That(config, Is.Not.Null);
        Assert.That(config.SelectedThemeName, Is.Null);
        Assert.That(config.GlobalOpacity, Is.EqualTo(1.0f));
    }

    /// <summary>
    /// Test invalid configuration file scenarios.
    /// Requirements: 6.2, 6.3
    /// </summary>
    [Test]
    public void Load_WhenConfigurationFileIsInvalid_ShouldReturnDefaultConfiguration()
    {
        // Create invalid JSON content
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDirectory = Path.Combine(appDataPath, "caTTY");
        var configFile = Path.Combine(configDirectory, "theme-config.json");

        // Ensure directory exists
        Directory.CreateDirectory(configDirectory);

        // Write invalid JSON
        var invalidJsonContent = "{ invalid json content }";
        File.WriteAllText(configFile, invalidJsonContent);

        try
        {
            // Load configuration
            var config = ThemeConfiguration.Load();

            // Should return default configuration despite invalid JSON
            Assert.That(config, Is.Not.Null);
            Assert.That(config.SelectedThemeName, Is.Null);
            Assert.That(config.GlobalOpacity, Is.EqualTo(1.0f));
        }
        finally
        {
            // Clean up
            if (File.Exists(configFile))
            {
                File.Delete(configFile);
            }
        }
    }

    /// <summary>
    /// Test configuration file with malformed JSON structure.
    /// Requirements: 6.2, 6.3
    /// </summary>
    [Test]
    public void Load_WhenConfigurationFileHasMalformedJson_ShouldReturnDefaultConfiguration()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDirectory = Path.Combine(appDataPath, "caTTY");
        var configFile = Path.Combine(configDirectory, "theme-config.json");

        Directory.CreateDirectory(configDirectory);

        // Test various malformed JSON scenarios
        var malformedJsonCases = new[]
        {
            "{ \"SelectedThemeName\": \"Test\", }", // Trailing comma
            "{ \"SelectedThemeName\": \"Test\" \"GlobalOpacity\": 0.5 }", // Missing comma
            "{ \"SelectedThemeName\": }", // Missing value
            "{ \"GlobalOpacity\": \"not_a_number\" }", // Wrong type
            "{ \"UnknownProperty\": \"value\" }", // Unknown property only
            "", // Empty file
            "null", // Null JSON
            "[]", // Array instead of object
        };

        foreach (var malformedJson in malformedJsonCases)
        {
            File.WriteAllText(configFile, malformedJson);

            try
            {
                var config = ThemeConfiguration.Load();

                // Should always return a valid default configuration
                Assert.That(config, Is.Not.Null, $"Failed for JSON: {malformedJson}");
                Assert.That(config.SelectedThemeName, Is.Null, $"Failed for JSON: {malformedJson}");
                Assert.That(config.GlobalOpacity, Is.EqualTo(1.0f), $"Failed for JSON: {malformedJson}");
            }
            finally
            {
                if (File.Exists(configFile))
                {
                    File.Delete(configFile);
                }
            }
        }
    }

    /// <summary>
    /// Test configuration save when directory doesn't exist.
    /// Requirements: 6.2, 6.3
    /// </summary>
    [Test]
    public void Save_WhenDirectoryDoesNotExist_ShouldCreateDirectoryAndSave()
    {
        var config = new ThemeConfiguration
        {
            SelectedThemeName = "TestTheme",
            GlobalOpacity = 0.75f
        };

        // Ensure directory doesn't exist initially
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDirectory = Path.Combine(appDataPath, "caTTY");
        var configFile = Path.Combine(configDirectory, "theme-config.json");

        if (Directory.Exists(configDirectory))
        {
            Directory.Delete(configDirectory, true);
        }

        try
        {
            // Save configuration
            config.Save();

            // Directory and file should be created
            Assert.That(Directory.Exists(configDirectory), Is.True);
            Assert.That(File.Exists(configFile), Is.True);

            // Content should be correct
            var loadedConfig = ThemeConfiguration.Load();
            Assert.That(loadedConfig.SelectedThemeName, Is.EqualTo("TestTheme"));
            Assert.That(loadedConfig.GlobalOpacity, Is.EqualTo(0.75f).Within(0.001f));
        }
        finally
        {
            // Clean up
            if (File.Exists(configFile))
            {
                File.Delete(configFile);
            }
            if (Directory.Exists(configDirectory))
            {
                Directory.Delete(configDirectory, true);
            }
        }
    }

    /// <summary>
    /// Test valid configuration save and load cycle.
    /// </summary>
    [Test]
    public void SaveAndLoad_WithValidConfiguration_ShouldPreserveAllValues()
    {
        var originalConfig = new ThemeConfiguration
        {
            SelectedThemeName = "Adventure",
            GlobalOpacity = 0.85f
        };

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDirectory = Path.Combine(appDataPath, "caTTY");
        var configFile = Path.Combine(configDirectory, "theme-config.json");

        try
        {
            // Save configuration
            originalConfig.Save();

            // Load configuration back
            var loadedConfig = ThemeConfiguration.Load();

            // All values should be preserved
            Assert.That(loadedConfig.SelectedThemeName, Is.EqualTo("Adventure"));
            Assert.That(loadedConfig.GlobalOpacity, Is.EqualTo(0.85f).Within(0.001f));
        }
        finally
        {
            // Clean up
            if (File.Exists(configFile))
            {
                File.Delete(configFile);
            }
        }
    }

    /// <summary>
    /// Test configuration with null theme name.
    /// </summary>
    [Test]
    public void SaveAndLoad_WithNullThemeName_ShouldHandleGracefully()
    {
        var config = new ThemeConfiguration
        {
            SelectedThemeName = null,
            GlobalOpacity = 0.5f
        };

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configFile = Path.Combine(appDataPath, "caTTY", "theme-config.json");

        try
        {
            // Save and load should work without exceptions
            config.Save();
            var loadedConfig = ThemeConfiguration.Load();

            // Null theme name should be preserved
            Assert.That(loadedConfig.SelectedThemeName, Is.Null);
            Assert.That(loadedConfig.GlobalOpacity, Is.EqualTo(0.5f).Within(0.001f));
        }
        finally
        {
            // Clean up
            if (File.Exists(configFile))
            {
                File.Delete(configFile);
            }
        }
    }

    /// <summary>
    /// Test configuration with edge case opacity values.
    /// </summary>
    [Test]
    public void SaveAndLoad_WithEdgeCaseOpacityValues_ShouldPreserveValues()
    {
        var edgeCases = new[] { 0.0f, 1.0f, 0.001f, 0.999f };

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configFile = Path.Combine(appDataPath, "caTTY", "theme-config.json");

        foreach (var opacity in edgeCases)
        {
            var config = new ThemeConfiguration
            {
                SelectedThemeName = $"TestTheme_{opacity}",
                GlobalOpacity = opacity
            };

            try
            {
                config.Save();
                var loadedConfig = ThemeConfiguration.Load();

                Assert.That(loadedConfig.GlobalOpacity, Is.EqualTo(opacity).Within(0.0001f), 
                    $"Failed to preserve opacity value: {opacity}");
                Assert.That(loadedConfig.SelectedThemeName, Is.EqualTo($"TestTheme_{opacity}"));
            }
            finally
            {
                if (File.Exists(configFile))
                {
                    File.Delete(configFile);
                }
            }
        }
    }
}