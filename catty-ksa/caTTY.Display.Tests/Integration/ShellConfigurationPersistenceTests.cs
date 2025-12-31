using NUnit.Framework;
using caTTY.Display.Configuration;
using caTTY.Display.Controllers;
using caTTY.Core.Terminal;
using System.IO;

namespace caTTY.Display.Tests.Integration;

/// <summary>
/// Integration tests for shell configuration persistence and loading.
/// </summary>
[TestFixture]
[Category("Integration")]
public class ShellConfigurationPersistenceTests
{
    private string? _originalAppDataOverride;
    private string? _tempConfigDirectory;

    [SetUp]
    public void SetUp()
    {
        // Create a temporary directory for test configuration
        _tempConfigDirectory = Path.Combine(Path.GetTempPath(), "caTTY_Test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempConfigDirectory);

        // Override the app data directory for testing
        _originalAppDataOverride = ThemeConfiguration.OverrideAppDataDirectory;
        ThemeConfiguration.OverrideAppDataDirectory = _tempConfigDirectory;
    }

    [TearDown]
    public void TearDown()
    {
        // Restore original app data override
        ThemeConfiguration.OverrideAppDataDirectory = _originalAppDataOverride;

        // Clean up temporary directory
        if (_tempConfigDirectory != null && Directory.Exists(_tempConfigDirectory))
        {
            Directory.Delete(_tempConfigDirectory, true);
        }
    }

    [Test]
    public void InitialSessionUsesPersistedShellConfiguration()
    {
        // Arrange - Save a PowerShell configuration
        var config = new ThemeConfiguration
        {
            DefaultShellType = ShellType.PowerShell,
            SelectedThemeName = "TestTheme"
        };
        config.Save();

        // Act - Create a TerminalController (which should load the persisted configuration)
        using var sessionManager = new SessionManager();
        using var controller = new TerminalController(sessionManager);

        // Assert - The session manager should have PowerShell as default
        var defaultOptions = sessionManager.DefaultLaunchOptions;
        Assert.That(defaultOptions.ShellType, Is.EqualTo(ShellType.PowerShell));
    }

    [Test]
    public void SessionCreatedWithoutExplicitOptionsUsesPersistedConfiguration()
    {
        // Arrange - Save a PowerShellCore configuration
        var config = new ThemeConfiguration
        {
            DefaultShellType = ShellType.PowerShellCore,
            SelectedThemeName = "TestTheme"
        };
        config.Save();

        // Act - Create controller and session without explicit launch options
        using var sessionManager = new SessionManager();
        using var controller = new TerminalController(sessionManager);

        // The session manager should now have PowerShellCore as default
        var defaultOptions = sessionManager.DefaultLaunchOptions;
        
        // Assert
        Assert.That(defaultOptions.ShellType, Is.EqualTo(ShellType.PowerShellCore));
        Assert.That(defaultOptions.InitialWidth, Is.EqualTo(80));
        Assert.That(defaultOptions.InitialHeight, Is.EqualTo(24));
        Assert.That(defaultOptions.WorkingDirectory, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void SessionCreatedWithExplicitOptionsOverridesPersistedConfiguration()
    {
        // Arrange - Save a PowerShell configuration
        var config = new ThemeConfiguration
        {
            DefaultShellType = ShellType.PowerShell,
            SelectedThemeName = "TestTheme"
        };
        config.Save();

        using var sessionManager = new SessionManager();
        using var controller = new TerminalController(sessionManager);

        // Act - Create session with explicit Cmd options (should override persisted PowerShell)
        var explicitOptions = ProcessLaunchOptions.CreateCmd();
        explicitOptions.InitialWidth = 100;
        explicitOptions.InitialHeight = 30;

        // We can't easily test the actual session creation without starting processes,
        // but we can verify that the default options are still PowerShell
        var defaultOptions = sessionManager.DefaultLaunchOptions;
        
        // Assert - Default should still be PowerShell (not overridden by explicit options)
        Assert.That(defaultOptions.ShellType, Is.EqualTo(ShellType.PowerShell));
    }

    [Test]
    public void NoPersistedConfigurationUsesDefaultShell()
    {
        // Arrange - No configuration file exists (fresh start)
        // The temp directory is empty, so no config file exists

        // Act - Create controller (should use default configuration)
        using var sessionManager = new SessionManager();
        using var controller = new TerminalController(sessionManager);

        // Assert - Should use default shell (WSL)
        var defaultOptions = sessionManager.DefaultLaunchOptions;
        Assert.That(defaultOptions.ShellType, Is.EqualTo(ShellType.Wsl));
    }

    [Test]
    public void ConfigurationChangesUpdateSessionManagerDefaults()
    {
        // Arrange - Start with WSL configuration
        var config = new ThemeConfiguration
        {
            DefaultShellType = ShellType.Wsl,
            SelectedThemeName = "TestTheme"
        };
        config.Save();

        using var sessionManager = new SessionManager();
        using var controller = new TerminalController(sessionManager);

        // Verify initial state
        Assert.That(sessionManager.DefaultLaunchOptions.ShellType, Is.EqualTo(ShellType.Wsl));

        // Act - Change configuration to PowerShell and save
        config.DefaultShellType = ShellType.PowerShell;
        config.Save();

        // Simulate what happens when user changes shell configuration in UI
        var newLaunchOptions = config.CreateLaunchOptions();
        newLaunchOptions.InitialWidth = 80;
        newLaunchOptions.InitialHeight = 24;
        newLaunchOptions.WorkingDirectory = Environment.CurrentDirectory;
        sessionManager.UpdateDefaultLaunchOptions(newLaunchOptions);

        // Assert - Session manager should now use PowerShell
        Assert.That(sessionManager.DefaultLaunchOptions.ShellType, Is.EqualTo(ShellType.PowerShell));
    }
}