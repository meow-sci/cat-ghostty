using caTTY.Core.Types;

namespace caTTY.Core.Managers;

/// <summary>
///     Manages SGR attribute state and application to characters.
///     Handles foreground/background colors, text styles, and other character attributes.
/// </summary>
public class AttributeManager : IAttributeManager
{
    private SgrAttributes _currentAttributes;

    /// <summary>
    ///     Creates a new attribute manager with default attributes.
    /// </summary>
    public AttributeManager()
    {
        _currentAttributes = SgrAttributes.Default;
        CurrentCharacterProtection = false;
    }

    /// <summary>
    ///     Gets or sets the current SGR attributes for new characters.
    /// </summary>
    public SgrAttributes CurrentAttributes
    {
        get => _currentAttributes;
        set => _currentAttributes = value;
    }

    /// <summary>
    ///     Gets or sets the current character protection attribute.
    /// </summary>
    public bool CurrentCharacterProtection { get; set; }

    /// <summary>
    ///     Applies an SGR message to update the current attributes.
    /// </summary>
    /// <param name="message">The SGR message to apply</param>
    public void ApplySgrMessage(SgrMessage message)
    {
        if (message == null)
        {
            return;
        }

        // Handle SGR message based on type
        switch (message.Type.ToLowerInvariant())
        {
            case "sgr.reset":
                ResetAttributes();
                break;
                
            case "sgr.bold":
                _currentAttributes = new SgrAttributes(
                    bold: true,
                    faint: _currentAttributes.Faint,
                    italic: _currentAttributes.Italic,
                    underline: _currentAttributes.Underline,
                    underlineStyle: _currentAttributes.UnderlineStyle,
                    blink: _currentAttributes.Blink,
                    inverse: _currentAttributes.Inverse,
                    hidden: _currentAttributes.Hidden,
                    strikethrough: _currentAttributes.Strikethrough,
                    foregroundColor: _currentAttributes.ForegroundColor,
                    backgroundColor: _currentAttributes.BackgroundColor,
                    underlineColor: _currentAttributes.UnderlineColor,
                    font: _currentAttributes.Font);
                break;
                
            case "sgr.normalintensity":
                _currentAttributes = new SgrAttributes(
                    bold: false,
                    faint: false,
                    italic: _currentAttributes.Italic,
                    underline: _currentAttributes.Underline,
                    underlineStyle: _currentAttributes.UnderlineStyle,
                    blink: _currentAttributes.Blink,
                    inverse: _currentAttributes.Inverse,
                    hidden: _currentAttributes.Hidden,
                    strikethrough: _currentAttributes.Strikethrough,
                    foregroundColor: _currentAttributes.ForegroundColor,
                    backgroundColor: _currentAttributes.BackgroundColor,
                    underlineColor: _currentAttributes.UnderlineColor,
                    font: _currentAttributes.Font);
                break;
                
            case "sgr.italic":
                _currentAttributes = new SgrAttributes(
                    bold: _currentAttributes.Bold,
                    faint: _currentAttributes.Faint,
                    italic: true,
                    underline: _currentAttributes.Underline,
                    underlineStyle: _currentAttributes.UnderlineStyle,
                    blink: _currentAttributes.Blink,
                    inverse: _currentAttributes.Inverse,
                    hidden: _currentAttributes.Hidden,
                    strikethrough: _currentAttributes.Strikethrough,
                    foregroundColor: _currentAttributes.ForegroundColor,
                    backgroundColor: _currentAttributes.BackgroundColor,
                    underlineColor: _currentAttributes.UnderlineColor,
                    font: _currentAttributes.Font);
                break;
                
            case "sgr.notitalic":
                _currentAttributes = new SgrAttributes(
                    bold: _currentAttributes.Bold,
                    faint: _currentAttributes.Faint,
                    italic: false,
                    underline: _currentAttributes.Underline,
                    underlineStyle: _currentAttributes.UnderlineStyle,
                    blink: _currentAttributes.Blink,
                    inverse: _currentAttributes.Inverse,
                    hidden: _currentAttributes.Hidden,
                    strikethrough: _currentAttributes.Strikethrough,
                    foregroundColor: _currentAttributes.ForegroundColor,
                    backgroundColor: _currentAttributes.BackgroundColor,
                    underlineColor: _currentAttributes.UnderlineColor,
                    font: _currentAttributes.Font);
                break;
                
            case "sgr.underline":
                _currentAttributes = new SgrAttributes(
                    bold: _currentAttributes.Bold,
                    faint: _currentAttributes.Faint,
                    italic: _currentAttributes.Italic,
                    underline: true,
                    underlineStyle: _currentAttributes.UnderlineStyle,
                    blink: _currentAttributes.Blink,
                    inverse: _currentAttributes.Inverse,
                    hidden: _currentAttributes.Hidden,
                    strikethrough: _currentAttributes.Strikethrough,
                    foregroundColor: _currentAttributes.ForegroundColor,
                    backgroundColor: _currentAttributes.BackgroundColor,
                    underlineColor: _currentAttributes.UnderlineColor,
                    font: _currentAttributes.Font);
                break;
                
            case "sgr.notunderlined":
                _currentAttributes = new SgrAttributes(
                    bold: _currentAttributes.Bold,
                    faint: _currentAttributes.Faint,
                    italic: _currentAttributes.Italic,
                    underline: false,
                    underlineStyle: _currentAttributes.UnderlineStyle,
                    blink: _currentAttributes.Blink,
                    inverse: _currentAttributes.Inverse,
                    hidden: _currentAttributes.Hidden,
                    strikethrough: _currentAttributes.Strikethrough,
                    foregroundColor: _currentAttributes.ForegroundColor,
                    backgroundColor: _currentAttributes.BackgroundColor,
                    underlineColor: _currentAttributes.UnderlineColor,
                    font: _currentAttributes.Font);
                break;
                
            case "sgr.foregroundcolor":
            case "sgr.backgroundcolor":
                // Color handling
                if (message.Data is Color color)
                {
                    if (message.Type.ToLowerInvariant() == "sgr.foregroundcolor")
                    {
                        _currentAttributes = new SgrAttributes(
                            bold: _currentAttributes.Bold,
                            faint: _currentAttributes.Faint,
                            italic: _currentAttributes.Italic,
                            underline: _currentAttributes.Underline,
                            underlineStyle: _currentAttributes.UnderlineStyle,
                            blink: _currentAttributes.Blink,
                            inverse: _currentAttributes.Inverse,
                            hidden: _currentAttributes.Hidden,
                            strikethrough: _currentAttributes.Strikethrough,
                            foregroundColor: color,
                            backgroundColor: _currentAttributes.BackgroundColor,
                            underlineColor: _currentAttributes.UnderlineColor,
                            font: _currentAttributes.Font);
                    }
                    else
                    {
                        _currentAttributes = new SgrAttributes(
                            bold: _currentAttributes.Bold,
                            faint: _currentAttributes.Faint,
                            italic: _currentAttributes.Italic,
                            underline: _currentAttributes.Underline,
                            underlineStyle: _currentAttributes.UnderlineStyle,
                            blink: _currentAttributes.Blink,
                            inverse: _currentAttributes.Inverse,
                            hidden: _currentAttributes.Hidden,
                            strikethrough: _currentAttributes.Strikethrough,
                            foregroundColor: _currentAttributes.ForegroundColor,
                            backgroundColor: color,
                            underlineColor: _currentAttributes.UnderlineColor,
                            font: _currentAttributes.Font);
                    }
                }
                break;
        }
    }

    /// <summary>
    ///     Resets all attributes to their default values.
    /// </summary>
    public void ResetAttributes()
    {
        _currentAttributes = SgrAttributes.Default;
        CurrentCharacterProtection = false;
    }

    /// <summary>
    ///     Gets the default SGR attributes.
    /// </summary>
    /// <returns>Default SGR attributes</returns>
    public SgrAttributes GetDefaultAttributes()
    {
        return SgrAttributes.Default;
    }

    /// <summary>
    ///     Sets the foreground color.
    /// </summary>
    /// <param name="color">The foreground color to set</param>
    public void SetForegroundColor(Color color)
    {
        _currentAttributes = new SgrAttributes(
            bold: _currentAttributes.Bold,
            faint: _currentAttributes.Faint,
            italic: _currentAttributes.Italic,
            underline: _currentAttributes.Underline,
            underlineStyle: _currentAttributes.UnderlineStyle,
            blink: _currentAttributes.Blink,
            inverse: _currentAttributes.Inverse,
            hidden: _currentAttributes.Hidden,
            strikethrough: _currentAttributes.Strikethrough,
            foregroundColor: color,
            backgroundColor: _currentAttributes.BackgroundColor,
            underlineColor: _currentAttributes.UnderlineColor,
            font: _currentAttributes.Font);
    }

    /// <summary>
    ///     Sets the background color.
    /// </summary>
    /// <param name="color">The background color to set</param>
    public void SetBackgroundColor(Color color)
    {
        _currentAttributes = new SgrAttributes(
            bold: _currentAttributes.Bold,
            faint: _currentAttributes.Faint,
            italic: _currentAttributes.Italic,
            underline: _currentAttributes.Underline,
            underlineStyle: _currentAttributes.UnderlineStyle,
            blink: _currentAttributes.Blink,
            inverse: _currentAttributes.Inverse,
            hidden: _currentAttributes.Hidden,
            strikethrough: _currentAttributes.Strikethrough,
            foregroundColor: _currentAttributes.ForegroundColor,
            backgroundColor: color,
            underlineColor: _currentAttributes.UnderlineColor,
            font: _currentAttributes.Font);
    }

    /// <summary>
    ///     Sets text style attributes.
    /// </summary>
    /// <param name="bold">Whether text should be bold</param>
    /// <param name="italic">Whether text should be italic</param>
    /// <param name="underline">Whether text should be underlined</param>
    public void SetTextStyle(bool bold, bool italic, bool underline)
    {
        _currentAttributes = new SgrAttributes(
            bold: bold,
            faint: _currentAttributes.Faint,
            italic: italic,
            underline: underline,
            underlineStyle: _currentAttributes.UnderlineStyle,
            blink: _currentAttributes.Blink,
            inverse: _currentAttributes.Inverse,
            hidden: _currentAttributes.Hidden,
            strikethrough: _currentAttributes.Strikethrough,
            foregroundColor: _currentAttributes.ForegroundColor,
            backgroundColor: _currentAttributes.BackgroundColor,
            underlineColor: _currentAttributes.UnderlineColor,
            font: _currentAttributes.Font);
    }

    /// <summary>
    ///     Sets the inverse video attribute.
    /// </summary>
    /// <param name="inverse">Whether inverse video should be enabled</param>
    public void SetInverse(bool inverse)
    {
        _currentAttributes = new SgrAttributes(
            bold: _currentAttributes.Bold,
            faint: _currentAttributes.Faint,
            italic: _currentAttributes.Italic,
            underline: _currentAttributes.Underline,
            underlineStyle: _currentAttributes.UnderlineStyle,
            blink: _currentAttributes.Blink,
            inverse: inverse,
            hidden: _currentAttributes.Hidden,
            strikethrough: _currentAttributes.Strikethrough,
            foregroundColor: _currentAttributes.ForegroundColor,
            backgroundColor: _currentAttributes.BackgroundColor,
            underlineColor: _currentAttributes.UnderlineColor,
            font: _currentAttributes.Font);
    }

    /// <summary>
    ///     Sets the dim attribute.
    /// </summary>
    /// <param name="dim">Whether dim should be enabled</param>
    public void SetDim(bool dim)
    {
        _currentAttributes = new SgrAttributes(
            bold: _currentAttributes.Bold,
            faint: dim,
            italic: _currentAttributes.Italic,
            underline: _currentAttributes.Underline,
            underlineStyle: _currentAttributes.UnderlineStyle,
            blink: _currentAttributes.Blink,
            inverse: _currentAttributes.Inverse,
            hidden: _currentAttributes.Hidden,
            strikethrough: _currentAttributes.Strikethrough,
            foregroundColor: _currentAttributes.ForegroundColor,
            backgroundColor: _currentAttributes.BackgroundColor,
            underlineColor: _currentAttributes.UnderlineColor,
            font: _currentAttributes.Font);
    }

    /// <summary>
    ///     Sets the strikethrough attribute.
    /// </summary>
    /// <param name="strikethrough">Whether strikethrough should be enabled</param>
    public void SetStrikethrough(bool strikethrough)
    {
        _currentAttributes = new SgrAttributes(
            bold: _currentAttributes.Bold,
            faint: _currentAttributes.Faint,
            italic: _currentAttributes.Italic,
            underline: _currentAttributes.Underline,
            underlineStyle: _currentAttributes.UnderlineStyle,
            blink: _currentAttributes.Blink,
            inverse: _currentAttributes.Inverse,
            hidden: _currentAttributes.Hidden,
            strikethrough: strikethrough,
            foregroundColor: _currentAttributes.ForegroundColor,
            backgroundColor: _currentAttributes.BackgroundColor,
            underlineColor: _currentAttributes.UnderlineColor,
            font: _currentAttributes.Font);
    }

    /// <summary>
    ///     Sets the blink attribute.
    /// </summary>
    /// <param name="blink">Whether blink should be enabled</param>
    public void SetBlink(bool blink)
    {
        _currentAttributes = new SgrAttributes(
            bold: _currentAttributes.Bold,
            faint: _currentAttributes.Faint,
            italic: _currentAttributes.Italic,
            underline: _currentAttributes.Underline,
            underlineStyle: _currentAttributes.UnderlineStyle,
            blink: blink,
            inverse: _currentAttributes.Inverse,
            hidden: _currentAttributes.Hidden,
            strikethrough: _currentAttributes.Strikethrough,
            foregroundColor: _currentAttributes.ForegroundColor,
            backgroundColor: _currentAttributes.BackgroundColor,
            underlineColor: _currentAttributes.UnderlineColor,
            font: _currentAttributes.Font);
    }
}