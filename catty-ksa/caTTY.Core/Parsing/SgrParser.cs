using System.Text;
using caTTY.Core.Types;
using Microsoft.Extensions.Logging;

namespace caTTY.Core.Parsing;

/// <summary>
///     SGR (Select Graphic Rendition) sequence parser.
///     Handles parsing of CSI ... m sequences and parameter processing.
///     Based on the TypeScript ParseSgr implementation with identical behavior.
/// </summary>
public class SgrParser : ISgrParser
{
    private readonly ILogger _logger;
    private readonly ICursorPositionProvider? _cursorPositionProvider;

    /// <summary>
    ///     Creates a new SGR parser.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages</param>
    /// <param name="cursorPositionProvider">Optional cursor position provider for tracing</param>
    public SgrParser(ILogger logger, ICursorPositionProvider? cursorPositionProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cursorPositionProvider = cursorPositionProvider;
    }

    /// <summary>
    ///     Named colors for standard 8 colors (SGR 30-37, 40-47).
    /// </summary>
    private static readonly NamedColor[] StandardColors =
    {
        NamedColor.Black,
        NamedColor.Red,
        NamedColor.Green,
        NamedColor.Yellow,
        NamedColor.Blue,
        NamedColor.Magenta,
        NamedColor.Cyan,
        NamedColor.White
    };

    /// <summary>
    ///     Named colors for bright colors (SGR 90-97, 100-107).
    /// </summary>
    private static readonly NamedColor[] BrightColors =
    {
        NamedColor.BrightBlack,
        NamedColor.BrightRed,
        NamedColor.BrightGreen,
        NamedColor.BrightYellow,
        NamedColor.BrightBlue,
        NamedColor.BrightMagenta,
        NamedColor.BrightCyan,
        NamedColor.BrightWhite
    };

    /// <summary>
    ///     Creates a new SGR parser.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages</param>
    public SgrParser(ILogger? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    ///     Parses an SGR sequence from CSI parameters.
    /// </summary>
    /// <param name="escapeSequence">The raw escape sequence bytes</param>
    /// <param name="raw">The raw sequence string</param>
    /// <returns>The parsed SGR sequence with individual messages</returns>
    public SgrSequence ParseSgrSequence(byte[] escapeSequence, string raw)
    {
        var parseResult = ParseSgrParamsAndSeparators(raw);
        var messages = ParseSgr(parseResult.Params, parseResult.Separators, parseResult.Prefix, parseResult.Intermediate);
        
        bool allImplemented = messages.All(m => m.Implemented);
        
        return new SgrSequence
        {
            Type = "sgr",
            Implemented = allImplemented,
            Raw = raw,
            Messages = messages.ToArray()
        };
    }

    /// <summary>
    ///     Parses SGR parameters with support for both semicolon and colon separators.
    /// </summary>
    /// <param name="parameterString">The parameter string to parse</param>
    /// <param name="parameters">The parsed parameters</param>
    /// <returns>True if parsing was successful</returns>
    public bool TryParseParameters(ReadOnlySpan<char> parameterString, out int[] parameters)
    {
        var result = new List<int>();
        var current = new StringBuilder();
        
        foreach (char ch in parameterString)
        {
            if (char.IsDigit(ch))
            {
                current.Append(ch);
            }
            else if (ch == ';' || ch == ':')
            {
                if (current.Length > 0)
                {
                    if (int.TryParse(current.ToString(), out int value))
                    {
                        result.Add(value);
                    }
                    else
                    {
                        parameters = Array.Empty<int>();
                        return false;
                    }
                }
                else
                {
                    result.Add(0); // Empty parameter defaults to 0
                }
                current.Clear();
            }
        }
        
        // Add final parameter
        if (current.Length > 0)
        {
            if (int.TryParse(current.ToString(), out int value))
            {
                result.Add(value);
            }
            else
            {
                parameters = Array.Empty<int>();
                return false;
            }
        }
        else if (parameterString.Length > 0 && (parameterString[^1] == ';' || parameterString[^1] == ':'))
        {
            result.Add(0); // Trailing separator means empty parameter
        }
        
        if (result.Count == 0)
        {
            result.Add(0); // Default to reset if no parameters
        }
        
        parameters = result.ToArray();
        return true;
    }

    /// <summary>
    ///     Applies SGR attributes to the current state.
    /// </summary>
    /// <param name="current">The current SGR attributes</param>
    /// <param name="messages">The SGR messages to apply</param>
    /// <returns>The updated SGR attributes</returns>
    public SgrAttributes ApplyAttributes(SgrAttributes current, ReadOnlySpan<SgrMessage> messages)
    {
        var result = current;
        
        foreach (var message in messages)
        {
            result = ApplySingleMessage(result, message);
        }
        
        return result;
    }

    /// <summary>
    ///     Parses SGR parameters and separators from raw sequence.
    /// </summary>
    private SgrParseResult ParseSgrParamsAndSeparators(string raw)
    {
        // Extract parameter text (remove ESC[ and final m)
        string paramsText = raw.Length >= 3 ? raw[2..^1] : "";
        
        var parameters = new List<int>();
        var separators = new List<string>();
        string? prefix = null;
        string? intermediate = null;
        
        // Check for special prefixes
        int startIndex = 0;
        if (paramsText.StartsWith(">"))
        {
            prefix = ">";
            startIndex = 1;
        }
        else if (paramsText.StartsWith("?"))
        {
            prefix = "?";
            startIndex = 1;
        }
        
        var current = new StringBuilder();
        for (int i = startIndex; i < paramsText.Length; i++)
        {
            char ch = paramsText[i];
            
            if (char.IsDigit(ch))
            {
                current.Append(ch);
                continue;
            }
            
            if (ch == ';' || ch == ':')
            {
                parameters.Add(current.Length > 0 ? int.Parse(current.ToString()) : 0);
                separators.Add(ch.ToString());
                current.Clear();
                continue;
            }
            
            // Check for intermediate characters
            if (ch == '%' || ch == '$' || ch == ' ')
            {
                if (current.Length > 0)
                {
                    parameters.Add(int.Parse(current.ToString()));
                    current.Clear();
                }
                intermediate = ch.ToString();
                continue;
            }
        }
        
        // Add final parameter
        if (current.Length > 0)
        {
            parameters.Add(int.Parse(current.ToString()));
        }
        else if (paramsText.EndsWith(";") || paramsText.EndsWith(":"))
        {
            parameters.Add(0);
        }
        
        if (parameters.Count == 0)
        {
            parameters.Add(0);
        }
        
        return new SgrParseResult
        {
            Params = parameters.ToArray(),
            Separators = separators.ToArray(),
            Prefix = prefix,
            Intermediate = intermediate
        };
    }

    /// <summary>
    ///     Parses SGR parameters into individual messages.
    /// </summary>
    private List<SgrMessage> ParseSgr(int[] parameters, string[] separators, string? prefix, string? intermediate)
    {
        var messages = new List<SgrMessage>();
        
        // Handle special sequences with prefixes or intermediates
        if (prefix == ">")
        {
            return HandleEnhancedSgrMode(parameters);
        }
        
        if (prefix == "?")
        {
            return HandlePrivateSgrMode(parameters);
        }
        
        if (intermediate != null)
        {
            return HandleSgrWithIntermediate(parameters, intermediate);
        }
        
        // Empty or single zero param means reset
        if (parameters.Length == 0 || (parameters.Length == 1 && parameters[0] == 0))
        {
            messages.Add(CreateSgrMessage("sgr.reset", true));
            return messages;
        }
        
        var context = new SgrParseContext
        {
            Params = parameters,
            Separators = separators,
            Index = 0
        };
        
        while (context.Index < parameters.Length)
        {
            int param = parameters[context.Index];
            string nextSep = context.Index < separators.Length ? separators[context.Index] : ";";
            
            var message = ParseSingleParameter(context, param, nextSep);
            if (message != null)
            {
                messages.Add(message);
            }
        }
        
        return messages;
    }

    /// <summary>
    ///     Parses a single SGR parameter.
    /// </summary>
    private SgrMessage? ParseSingleParameter(SgrParseContext context, int param, string nextSep)
    {
        switch (param)
        {
            case 0:
                context.Index++;
                return CreateSgrMessage("sgr.reset", true);
                
            case 1:
                context.Index++;
                return CreateSgrMessage("sgr.bold", true);
                
            case 2:
                context.Index++;
                return CreateSgrMessage("sgr.faint", true);
                
            case 3:
                context.Index++;
                return CreateSgrMessage("sgr.italic", true);
                
            case 4:
                return ParseUnderlineParameter(context, nextSep);
                
            case 5:
                context.Index++;
                return CreateSgrMessage("sgr.slowBlink", true);
                
            case 6:
                context.Index++;
                return CreateSgrMessage("sgr.rapidBlink", true);
                
            case 7:
                context.Index++;
                return CreateSgrMessage("sgr.inverse", true);
                
            case 8:
                context.Index++;
                return CreateSgrMessage("sgr.hidden", true);
                
            case 9:
                context.Index++;
                return CreateSgrMessage("sgr.strikethrough", true);
                
            case >= 10 and <= 19:
                context.Index++;
                return CreateSgrMessage("sgr.font", false, param - 10);
                
            case 20:
                context.Index++;
                return CreateSgrMessage("sgr.fraktur", false);
                
            case 21:
                context.Index++;
                return CreateSgrMessage("sgr.doubleUnderline", true);
                
            case 22:
                context.Index++;
                return CreateSgrMessage("sgr.normalIntensity", true);
                
            case 23:
                context.Index++;
                return CreateSgrMessage("sgr.notItalic", true);
                
            case 24:
                context.Index++;
                return CreateSgrMessage("sgr.notUnderlined", true);
                
            case 25:
                context.Index++;
                return CreateSgrMessage("sgr.notBlinking", true);
                
            case 26:
                context.Index++;
                return CreateSgrMessage("sgr.proportionalSpacing", false);
                
            case 27:
                context.Index++;
                return CreateSgrMessage("sgr.notInverse", true);
                
            case 28:
                context.Index++;
                return CreateSgrMessage("sgr.notHidden", true);
                
            case 29:
                context.Index++;
                return CreateSgrMessage("sgr.notStrikethrough", true);
                
            case >= 30 and <= 37:
                context.Index++;
                return CreateSgrMessage("sgr.foregroundColor", true, new Color(StandardColors[param - 30]));
                
            case 38:
                return ParseExtendedForegroundColor(context);
                
            case 39:
                context.Index++;
                return CreateSgrMessage("sgr.defaultForeground", true);
                
            case >= 40 and <= 47:
                context.Index++;
                return CreateSgrMessage("sgr.backgroundColor", true, new Color(StandardColors[param - 40]));
                
            case 48:
                return ParseExtendedBackgroundColor(context);
                
            case 49:
                context.Index++;
                return CreateSgrMessage("sgr.defaultBackground", true);
                
            case >= 90 and <= 97:
                context.Index++;
                return CreateSgrMessage("sgr.foregroundColor", true, new Color(BrightColors[param - 90]));
                
            case >= 100 and <= 107:
                context.Index++;
                return CreateSgrMessage("sgr.backgroundColor", true, new Color(BrightColors[param - 100]));
                
            case 50:
                context.Index++;
                return CreateSgrMessage("sgr.disableProportionalSpacing", false);
                
            case 51:
                context.Index++;
                return CreateSgrMessage("sgr.framed", false);
                
            case 52:
                context.Index++;
                return CreateSgrMessage("sgr.encircled", false);
                
            case 53:
                context.Index++;
                return CreateSgrMessage("sgr.overlined", false);
                
            case 54:
                context.Index++;
                return CreateSgrMessage("sgr.notFramed", false);
                
            case 55:
                context.Index++;
                return CreateSgrMessage("sgr.notOverlined", false);
                
            case >= 60 and <= 65:
                context.Index++;
                return CreateSgrMessage("sgr.ideogram", false, GetIdeogramStyle(param));
                
            case 73:
                context.Index++;
                return CreateSgrMessage("sgr.superscript", false);
                
            case 74:
                context.Index++;
                return CreateSgrMessage("sgr.subscript", false);
                
            case 75:
                context.Index++;
                return CreateSgrMessage("sgr.notSuperscriptSubscript", false);
                
            case 58:
                return ParseExtendedUnderlineColor(context);
                
            case 59:
                context.Index++;
                return CreateSgrMessage("sgr.defaultUnderlineColor", true);
                
            default:
                context.Index++;
                return CreateSgrMessage("sgr.unknown", false, param);
        }
    }

    /// <summary>
    ///     Creates an SGR message with the specified properties.
    /// </summary>
    private static SgrMessage CreateSgrMessage(string type, bool implemented, object? data = null)
    {
        return new SgrMessage
        {
            Type = type,
            Implemented = implemented,
            Data = data
        };
    }

    /// <summary>
    ///     Applies a single SGR message to the current attributes.
    /// </summary>
    private SgrAttributes ApplySingleMessage(SgrAttributes current, SgrMessage message)
    {
        return message.Type switch
        {
            "sgr.reset" => SgrAttributes.Default,
            "sgr.bold" => UpdateAttribute(current, bold: true),
            "sgr.faint" => UpdateAttribute(current, faint: true),
            "sgr.italic" => UpdateAttribute(current, italic: true),
            "sgr.underline" => message.Data is UnderlineStyle style 
                ? UpdateAttribute(current, underline: true, underlineStyle: style)
                : UpdateAttribute(current, underline: true, underlineStyle: UnderlineStyle.Single),
            "sgr.slowBlink" or "sgr.rapidBlink" => UpdateAttribute(current, blink: true),
            "sgr.inverse" => UpdateAttribute(current, inverse: true),
            "sgr.hidden" => UpdateAttribute(current, hidden: true),
            "sgr.strikethrough" => UpdateAttribute(current, strikethrough: true),
            "sgr.normalIntensity" => UpdateAttribute(current, bold: false, faint: false),
            "sgr.notItalic" => UpdateAttribute(current, italic: false),
            "sgr.notUnderlined" => UpdateAttribute(current, underline: false, underlineStyle: UnderlineStyle.None),
            "sgr.notBlinking" => UpdateAttribute(current, blink: false),
            "sgr.notInverse" => UpdateAttribute(current, inverse: false),
            "sgr.notHidden" => UpdateAttribute(current, hidden: false),
            "sgr.notStrikethrough" => UpdateAttribute(current, strikethrough: false),
            "sgr.foregroundColor" when message.Data is Color color => UpdateAttributeWithColor(current, foregroundColor: color),
            "sgr.backgroundColor" when message.Data is Color bgColor => UpdateAttributeWithColor(current, backgroundColor: bgColor),
            "sgr.underlineColor" when message.Data is Color ulColor => UpdateAttributeWithColor(current, underlineColor: ulColor),
            "sgr.defaultForeground" => UpdateAttributeWithColor(current, foregroundColor: null),
            "sgr.defaultBackground" => UpdateAttributeWithColor(current, backgroundColor: null),
            "sgr.defaultUnderlineColor" => UpdateAttributeWithColor(current, underlineColor: null),
            "sgr.font" when message.Data is int font => UpdateAttribute(current, font: font),
            "sgr.disableProportionalSpacing" => current, // Not implemented, no change
            "sgr.framed" => current, // Not implemented, no change
            "sgr.encircled" => current, // Not implemented, no change
            "sgr.overlined" => current, // Not implemented, no change
            "sgr.notFramed" => current, // Not implemented, no change
            "sgr.notOverlined" => current, // Not implemented, no change
            "sgr.ideogram" => current, // Not implemented, no change
            "sgr.superscript" => current, // Not implemented, no change
            "sgr.subscript" => current, // Not implemented, no change
            "sgr.notSuperscriptSubscript" => current, // Not implemented, no change
            _ => current // Unknown or unimplemented messages don't change attributes
        };
    }

    /// <summary>
    ///     Helper method to create a new SgrAttributes with updated values.
    /// </summary>
    private static SgrAttributes UpdateAttribute(
        SgrAttributes current,
        bool? bold = null,
        bool? faint = null,
        bool? italic = null,
        bool? underline = null,
        UnderlineStyle? underlineStyle = null,
        bool? blink = null,
        bool? inverse = null,
        bool? hidden = null,
        bool? strikethrough = null,
        int? font = null)
    {
        return new SgrAttributes(
            bold: bold ?? current.Bold,
            faint: faint ?? current.Faint,
            italic: italic ?? current.Italic,
            underline: underline ?? current.Underline,
            underlineStyle: underlineStyle ?? current.UnderlineStyle,
            blink: blink ?? current.Blink,
            inverse: inverse ?? current.Inverse,
            hidden: hidden ?? current.Hidden,
            strikethrough: strikethrough ?? current.Strikethrough,
            foregroundColor: current.ForegroundColor,
            backgroundColor: current.BackgroundColor,
            underlineColor: current.UnderlineColor,
            font: font ?? current.Font);
    }

    /// <summary>
    ///     Helper method to create a new SgrAttributes with updated color values.
    /// </summary>
    private static SgrAttributes UpdateAttributeWithColor(
        SgrAttributes current,
        Color? foregroundColor = default,
        Color? backgroundColor = default,
        Color? underlineColor = default)
    {
        return new SgrAttributes(
            bold: current.Bold,
            faint: current.Faint,
            italic: current.Italic,
            underline: current.Underline,
            underlineStyle: current.UnderlineStyle,
            blink: current.Blink,
            inverse: current.Inverse,
            hidden: current.Hidden,
            strikethrough: current.Strikethrough,
            foregroundColor: foregroundColor == default(Color?) ? current.ForegroundColor : foregroundColor,
            backgroundColor: backgroundColor == default(Color?) ? current.BackgroundColor : backgroundColor,
            underlineColor: underlineColor == default(Color?) ? current.UnderlineColor : underlineColor,
            font: current.Font);
    }

    // Helper methods for parsing complex parameters (implementation continues...)
    private SgrMessage? ParseUnderlineParameter(SgrParseContext context, string nextSep)
    {
        if (nextSep == ":" && context.Index + 1 < context.Params.Length)
        {
            int styleParam = context.Params[context.Index + 1];
            if (styleParam == 0)
            {
                context.Index += 2;
                return CreateSgrMessage("sgr.notUnderlined", true);
            }
            
            var style = ParseUnderlineStyle(styleParam);
            context.Index += 2;
            return CreateSgrMessage("sgr.underline", true, style);
        }
        
        context.Index++;
        return CreateSgrMessage("sgr.underline", true, UnderlineStyle.Single);
    }

    private static UnderlineStyle ParseUnderlineStyle(int style)
    {
        return style switch
        {
            0 or 1 => UnderlineStyle.Single,
            2 => UnderlineStyle.Double,
            3 => UnderlineStyle.Curly,
            4 => UnderlineStyle.Dotted,
            5 => UnderlineStyle.Dashed,
            _ => UnderlineStyle.Single
        };
    }

    private static string GetIdeogramStyle(int param)
    {
        return param switch
        {
            60 => "underline",
            61 => "doubleUnderline", 
            62 => "overline",
            63 => "doubleOverline",
            64 => "stress",
            65 => "reset",
            _ => "unknown"
        };
    }

    private SgrMessage? ParseExtendedForegroundColor(SgrParseContext context)
    {
        context.Index++; // Skip 38
        var color = ParseExtendedColor(context, out int consumed);
        if (color != null)
        {
            context.Index += consumed;
            return CreateSgrMessage("sgr.foregroundColor", true, color);
        }
        // If parsing failed, create unknown message for just the 38 parameter
        return CreateSgrMessage("sgr.unknown", false, 38);
    }

    private SgrMessage? ParseExtendedBackgroundColor(SgrParseContext context)
    {
        context.Index++; // Skip 48
        var color = ParseExtendedColor(context, out int consumed);
        if (color != null)
        {
            context.Index += consumed;
            return CreateSgrMessage("sgr.backgroundColor", true, color);
        }
        // If parsing failed, create unknown message for just the 48 parameter
        return CreateSgrMessage("sgr.unknown", false, 48);
    }

    private SgrMessage? ParseExtendedUnderlineColor(SgrParseContext context)
    {
        context.Index++; // Skip 58
        var color = ParseExtendedColor(context, out int consumed);
        if (color != null)
        {
            context.Index += consumed;
            return CreateSgrMessage("sgr.underlineColor", true, color);
        }
        // If parsing failed, create unknown message for just the 58 parameter
        return CreateSgrMessage("sgr.unknown", false, 58);
    }

    private Color? ParseExtendedColor(SgrParseContext context, out int consumed)
    {
        consumed = 0;
        
        if (context.Index >= context.Params.Length)
            return null;

        int colorType = context.Params[context.Index];

        if (colorType == 5)
        {
            // 256-color mode: 38;5;n or 38:5:n
            if (context.Index + 1 < context.Params.Length)
            {
                int colorIndex = context.Params[context.Index + 1];
                if (colorIndex >= 0 && colorIndex <= 255)
                {
                    consumed = 2;
                    return new Color((byte)colorIndex);
                }
            }
            return null;
        }

        if (colorType == 2)
        {
            // True color mode: 38;2;r;g;b or 38:2:r:g:b (or 38:2::r:g:b with colorspace)
            // The ITU T.416 format includes an optional colorspace ID after the 2
            
            // Check if we have colon separators that might indicate colorspace format
            bool hasColonSeparators = context.Index < context.Separators.Length && 
                                     context.Separators[context.Index] == ":";

            if (hasColonSeparators && context.Index + 4 < context.Params.Length)
            {
                // Try parsing with colorspace ID: 38:2:<colorspace>:r:g:b or 38:2::r:g:b
                int r = context.Params[context.Index + 2];
                int g = context.Params[context.Index + 3];
                int b = context.Params[context.Index + 4];
                if (r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255)
                {
                    consumed = 5;
                    return new Color((byte)r, (byte)g, (byte)b);
                }
            }

            // Standard format: 38;2;r;g;b or 38:2:r:g:b
            if (context.Index + 3 < context.Params.Length)
            {
                int r = context.Params[context.Index + 1];
                int g = context.Params[context.Index + 2];
                int b = context.Params[context.Index + 3];
                if (r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255)
                {
                    consumed = 4;
                    return new Color((byte)r, (byte)g, (byte)b);
                }
            }
            return null;
        }

        // Unknown color type
        return null;
    }

    private List<SgrMessage> HandleEnhancedSgrMode(int[] parameters)
    {
        bool implemented = parameters.Length >= 2 && parameters[0] == 4 && parameters[1] >= 0 && parameters[1] <= 5;
        return new List<SgrMessage> { CreateSgrMessage("sgr.enhancedMode", implemented, parameters) };
    }

    private List<SgrMessage> HandlePrivateSgrMode(int[] parameters)
    {
        bool implemented = parameters.Length == 1 && parameters[0] == 4;
        return new List<SgrMessage> { CreateSgrMessage("sgr.privateMode", implemented, parameters) };
    }

    private List<SgrMessage> HandleSgrWithIntermediate(int[] parameters, string intermediate)
    {
        bool implemented = intermediate == "%" && parameters.Length == 1 && parameters[0] == 0;
        return new List<SgrMessage> { CreateSgrMessage("sgr.withIntermediate", implemented, new { parameters, intermediate }) };
    }
}

/// <summary>
///     Parse context for SGR sequences.
/// </summary>
internal class SgrParseContext
{
    public int[] Params { get; set; } = Array.Empty<int>();
    public string[] Separators { get; set; } = Array.Empty<string>();
    public int Index { get; set; }
}

/// <summary>
///     Result of parsing SGR parameters and separators.
/// </summary>
internal class SgrParseResult
{
    public int[] Params { get; set; } = Array.Empty<int>();
    public string[] Separators { get; set; } = Array.Empty<string>();
    public string? Prefix { get; set; }
    public string? Intermediate { get; set; }
}