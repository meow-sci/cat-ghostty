using System.Text;
using caTTY.Core.Parsing.Sgr;
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
    private readonly SgrParamTokenizer _tokenizer;
    private readonly SgrColorParsers _colorParsers;
    private readonly SgrAttributeApplier _attributeApplier;

    /// <summary>
    ///     Creates a new SGR parser.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages</param>
    /// <param name="cursorPositionProvider">Optional cursor position provider for tracing</param>
    public SgrParser(ILogger logger, ICursorPositionProvider? cursorPositionProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cursorPositionProvider = cursorPositionProvider;
        _tokenizer = new SgrParamTokenizer();
        _colorParsers = new SgrColorParsers();
        _attributeApplier = new SgrAttributeApplier();
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
        _tokenizer = new SgrParamTokenizer();
        _colorParsers = new SgrColorParsers();
        _attributeApplier = new SgrAttributeApplier();
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
        return _tokenizer.TryParseParameters(parameterString, out parameters);
    }

    /// <summary>
    ///     Applies SGR attributes to the current state.
    /// </summary>
    /// <param name="current">The current SGR attributes</param>
    /// <param name="messages">The SGR messages to apply</param>
    /// <returns>The updated SGR attributes</returns>
    public SgrAttributes ApplyAttributes(SgrAttributes current, ReadOnlySpan<SgrMessage> messages)
    {
        return _attributeApplier.ApplyAttributes(current, messages);
    }

    /// <summary>
    ///     Parses SGR parameters and separators from raw sequence.
    /// </summary>
    private SgrParseResult ParseSgrParamsAndSeparators(string raw)
    {
        return _tokenizer.ParseSgrParamsAndSeparators(raw);
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

    // Helper methods for parsing complex parameters
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
        return _colorParsers.ParseExtendedForegroundColor(context);
    }

    private SgrMessage? ParseExtendedBackgroundColor(SgrParseContext context)
    {
        return _colorParsers.ParseExtendedBackgroundColor(context);
    }

    private SgrMessage? ParseExtendedUnderlineColor(SgrParseContext context)
    {
        return _colorParsers.ParseExtendedUnderlineColor(context);
    }

    /// <summary>
    ///     Handles enhanced SGR sequences with > prefix (e.g., CSI > 4 ; 2 m).
    ///     These are typically used for advanced terminal features like enhanced underline styles.
    /// </summary>
    /// <param name="parameters">The SGR parameters</param>
    /// <returns>List of SGR messages for the enhanced mode</returns>
    private List<SgrMessage> HandleEnhancedSgrMode(int[] parameters)
    {
        var messages = new List<SgrMessage>();
        
        if (parameters.Length >= 2 && parameters[0] == 4)
        {
            // Enhanced underline mode: CSI > 4 ; n m
            int underlineType = parameters[1];
            
            if (underlineType >= 0 && underlineType <= 5)
            {
                // Valid enhanced underline mode - create appropriate SGR message
                switch (underlineType)
                {
                    case 0:
                        // No underline
                        messages.Add(CreateSgrMessage("sgr.notUnderlined", true));
                        break;
                    case 1:
                        // Single underline
                        messages.Add(CreateSgrMessage("sgr.underline", true, UnderlineStyle.Single));
                        break;
                    case 2:
                        // Double underline
                        messages.Add(CreateSgrMessage("sgr.underline", true, UnderlineStyle.Double));
                        break;
                    case 3:
                        // Curly underline
                        messages.Add(CreateSgrMessage("sgr.underline", true, UnderlineStyle.Curly));
                        break;
                    case 4:
                        // Dotted underline
                        messages.Add(CreateSgrMessage("sgr.underline", true, UnderlineStyle.Dotted));
                        break;
                    case 5:
                        // Dashed underline
                        messages.Add(CreateSgrMessage("sgr.underline", true, UnderlineStyle.Dashed));
                        break;
                }
                
                return messages;
            }
            
            // Invalid underline type - create unimplemented message
            messages.Add(CreateSgrMessage("sgr.enhancedMode", false, parameters));
            return messages;
        }
        
        // Other enhanced modes not yet supported - create unimplemented message
        messages.Add(CreateSgrMessage("sgr.enhancedMode", false, parameters));
        return messages;
    }

    /// <summary>
    ///     Handles private SGR sequences with ? prefix (e.g., CSI ? 4 m).
    ///     These are typically used for private/experimental features.
    /// </summary>
    /// <param name="parameters">The SGR parameters</param>
    /// <returns>List of SGR messages for the private mode</returns>
    private List<SgrMessage> HandlePrivateSgrMode(int[] parameters)
    {
        var messages = new List<SgrMessage>();
        
        // Handle specific private SGR modes
        if (parameters.Length == 1 && parameters[0] == 4)
        {
            // Private underline mode (?4m) - enable underline
            messages.Add(CreateSgrMessage("sgr.underline", true, UnderlineStyle.Single));
            return messages;
        }
        
        // For other private modes, gracefully ignore with unimplemented message
        messages.Add(CreateSgrMessage("sgr.privateMode", false, parameters));
        return messages;
    }

    /// <summary>
    ///     Handles SGR sequences with intermediate characters (e.g., CSI 0 % m).
    ///     These are used for special SGR attribute resets or modifications.
    /// </summary>
    /// <param name="parameters">The SGR parameters</param>
    /// <param name="intermediate">The intermediate character string</param>
    /// <returns>List of SGR messages for the intermediate sequence</returns>
    private List<SgrMessage> HandleSgrWithIntermediate(int[] parameters, string intermediate)
    {
        var messages = new List<SgrMessage>();
        
        // Handle specific intermediate character sequences
        if (intermediate == "%")
        {
            // CSI 0 % m - Reset specific attributes
            if (parameters.Length == 1 && parameters[0] == 0)
            {
                // Reset all SGR attributes (similar to SGR 0)
                messages.Add(CreateSgrMessage("sgr.reset", true));
                return messages;
            }
        }
        
        // For other intermediate sequences, gracefully ignore with unimplemented message
        bool implemented = intermediate == "%" && parameters.Length == 1 && parameters[0] == 0;
        messages.Add(CreateSgrMessage("sgr.withIntermediate", implemented, new { parameters, intermediate }));
        return messages;
    }
}