using caTTY.Core.Terminal;

namespace caTTY.Display.Configuration;

/// <summary>
/// Factory for creating SessionManager instances with proper configuration loading.
/// Ensures persisted shell settings are applied during initialization.
/// </summary>
public static class SessionManagerFactory
{
    /// <summary>
    /// Creates a SessionManager with persisted shell configuration loaded.
    /// This ensures that the user's saved shell preferences are used from startup.
    /// </summary>
    /// <param name="maxSessions">Maximum number of concurrent sessions (default: 20)</param>
    /// <returns>A SessionManager configured with persisted shell settings</returns>
    public static SessionManager CreateWithPersistedConfiguration(int maxSessions = 20)
    {
        // Load persisted configuration to determine initial shell type
        var themeConfig = ThemeConfiguration.Load();
        
        // Create launch options from persisted configuration
        var defaultLaunchOptions = themeConfig.CreateLaunchOptions();
        
        // Set default terminal dimensions and working directory
        defaultLaunchOptions.InitialWidth = 80;
        defaultLaunchOptions.InitialHeight = 24;
        defaultLaunchOptions.WorkingDirectory = Environment.CurrentDirectory;
        
        // Create session manager with persisted shell configuration
        return new SessionManager(maxSessions, defaultLaunchOptions);
    }
    
    /// <summary>
    /// Creates a SessionManager with default configuration (PowerShell on Windows).
    /// This is primarily for testing scenarios where persisted configuration should be ignored.
    /// </summary>
    /// <param name="maxSessions">Maximum number of concurrent sessions (default: 20)</param>
    /// <returns>A SessionManager with default shell configuration</returns>
    public static SessionManager CreateWithDefaultConfiguration(int maxSessions = 20)
    {
        return new SessionManager(maxSessions);
    }
}