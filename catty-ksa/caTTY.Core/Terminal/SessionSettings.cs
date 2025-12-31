namespace caTTY.Core.Terminal;

/// <summary>
///     Settings specific to a terminal session.
///     This is separate from display-related terminal settings.
/// </summary>
public class SessionSettings
{
    /// <summary>Terminal title for session identification</summary>
    public string Title { get; set; } = "Terminal 1";

    /// <summary>Whether this session is currently active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Validates the session settings for consistency and reasonable values.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when settings contain invalid values</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new ArgumentException("Title cannot be null or empty");
        }
    }

    /// <summary>
    /// Creates a copy of the current settings.
    /// </summary>
    /// <returns>A new SessionSettings instance with the same values</returns>
    public SessionSettings Clone()
    {
        return new SessionSettings
        {
            Title = Title,
            IsActive = IsActive
        };
    }
}