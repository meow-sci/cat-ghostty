using caTTY.Core.Types;

namespace caTTY.Core.Managers;

/// <summary>
///     Interface for managing SGR attribute state and application to characters.
/// </summary>
public interface IAttributeManager
{
    /// <summary>
    ///     Gets the current SGR attributes for new characters.
    /// </summary>
    SgrAttributes CurrentAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the current character protection attribute.
    /// </summary>
    bool CurrentCharacterProtection { get; set; }

    /// <summary>
    ///     Applies an SGR message to update the current attributes.
    /// </summary>
    /// <param name="message">The SGR message to apply</param>
    void ApplySgrMessage(SgrMessage message);

    /// <summary>
    ///     Resets all attributes to their default values.
    /// </summary>
    void ResetAttributes();

    /// <summary>
    ///     Gets the default SGR attributes.
    /// </summary>
    /// <returns>Default SGR attributes</returns>
    SgrAttributes GetDefaultAttributes();

    /// <summary>
    ///     Sets the foreground color.
    /// </summary>
    /// <param name="color">The foreground color to set</param>
    void SetForegroundColor(Color color);

    /// <summary>
    ///     Sets the background color.
    /// </summary>
    /// <param name="color">The background color to set</param>
    void SetBackgroundColor(Color color);

    /// <summary>
    ///     Sets text style attributes.
    /// </summary>
    /// <param name="bold">Whether text should be bold</param>
    /// <param name="italic">Whether text should be italic</param>
    /// <param name="underline">Whether text should be underlined</param>
    void SetTextStyle(bool bold, bool italic, bool underline);

    /// <summary>
    ///     Sets the inverse video attribute.
    /// </summary>
    /// <param name="inverse">Whether inverse video should be enabled</param>
    void SetInverse(bool inverse);

    /// <summary>
    ///     Sets the dim attribute.
    /// </summary>
    /// <param name="dim">Whether dim should be enabled</param>
    void SetDim(bool dim);

    /// <summary>
    ///     Sets the strikethrough attribute.
    /// </summary>
    /// <param name="strikethrough">Whether strikethrough should be enabled</param>
    void SetStrikethrough(bool strikethrough);

    /// <summary>
    ///     Sets the blink attribute.
    /// </summary>
    /// <param name="blink">Whether blink should be enabled</param>
    void SetBlink(bool blink);
}