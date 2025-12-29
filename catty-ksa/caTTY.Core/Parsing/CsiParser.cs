using System.Text;
using caTTY.Core.Types;
using caTTY.Core.Tracing;

namespace caTTY.Core.Parsing;

/// <summary>
///     CSI (Control Sequence Introducer) sequence parser.
///     Handles parameter parsing, prefix detection, and command identification.
///     Based on the TypeScript ParseCsi.ts implementation.
/// </summary>
public class CsiParser : ICsiParser
{
    /// <summary>
    ///     Parses a complete CSI sequence from the provided bytes.
    /// </summary>
    /// <param name="sequence">The complete CSI sequence bytes (including ESC [)</param>
    /// <param name="raw">The raw string representation of the sequence</param>
    /// <returns>A parsed CSI message with parameters and command information</returns>
    public CsiMessage ParseCsiSequence(ReadOnlySpan<byte> sequence, string raw)
    {
        if (sequence.Length < 3) // Minimum: ESC [ final
        {
            return CreateUnknownMessage(raw, Array.Empty<int>(), false, null, "");
        }

        byte finalByte = sequence[^1];
        string final = ((char)finalByte).ToString();

        // Extract parameters and intermediate characters
        var paramsText = new StringBuilder();
        var intermediate = new StringBuilder();

        // Skip ESC [ (first 2 bytes) and process until final byte
        for (int i = 2; i < sequence.Length - 1; i++)
        {
            byte b = sequence[i];
            if (b >= 0x30 && b <= 0x3f) // Parameter bytes (0-9, :, ;, <, =, >, ?)
            {
                paramsText.Append((char)b);
            }
            else if (b >= 0x20 && b <= 0x2f) // Intermediate bytes (space, !, ", #, etc.)
            {
                intermediate.Append((char)b);
            }
        }

        // Parse parameters
        if (!TryParseParameters(paramsText.ToString(), out int[] parameters, out bool isPrivate, out string? prefix))
        {
            return CreateUnknownMessage(raw, Array.Empty<int>(), false, null, intermediate.ToString());
        }

        string intermediateStr = intermediate.ToString();

        // Parse specific CSI commands based on final byte and modifiers
        var result = ParseCsiCommand(finalByte, final, parameters, isPrivate, prefix, intermediateStr, raw);
        
        // Trace the parsed CSI sequence
        TraceHelper.TraceCsiSequence((char)finalByte, paramsText.ToString(), 
            prefix?.FirstOrDefault(), TraceDirection.Output);
        
        return result;
    }

    /// <summary>
    ///     Attempts to parse CSI parameters from a parameter string.
    /// </summary>
    /// <param name="parameterString">The parameter portion of the CSI sequence</param>
    /// <param name="parameters">The parsed numeric parameters</param>
    /// <param name="isPrivate">True if the sequence has a '?' prefix</param>
    /// <param name="prefix">The prefix character ('>' or null)</param>
    /// <returns>True if parsing was successful</returns>
    public bool TryParseParameters(ReadOnlySpan<char> parameterString, out int[] parameters, out bool isPrivate,
        out string? prefix)
    {
        parameters = Array.Empty<int>();
        isPrivate = false;
        prefix = null;

        if (parameterString.IsEmpty)
        {
            return true;
        }

        string text = parameterString.ToString();

        // Check for private mode indicator
        if (text.StartsWith("?"))
        {
            isPrivate = true;
            text = text[1..];
        }
        else if (text.StartsWith(">"))
        {
            prefix = ">";
            text = text[1..];
        }

        if (text.Length == 0)
        {
            return true;
        }

        // Parse semicolon-separated parameters
        string[] parts = text.Split(';');
        var paramList = new List<int>();

        foreach (string part in parts)
        {
            if (string.IsNullOrEmpty(part))
            {
                // Empty parameter - treat as 0 (will be defaulted later)
                paramList.Add(0);
                continue;
            }

            if (int.TryParse(part, out int value))
            {
                paramList.Add(value);
            }
            else
            {
                // Invalid numbers are treated as 0 (following TypeScript behavior)
                paramList.Add(0);
            }
        }

        parameters = paramList.ToArray();
        return true;
    }

    /// <summary>
    ///     Gets a parameter value with a fallback default.
    /// </summary>
    /// <param name="parameters">The parameter array</param>
    /// <param name="index">The parameter index</param>
    /// <param name="fallback">The default value if parameter is missing</param>
    /// <returns>The parameter value or fallback</returns>
    public int GetParameter(int[] parameters, int index, int fallback)
    {
        if (index < 0 || index >= parameters.Length)
        {
            return fallback;
        }

        return parameters[index];
    }

    /// <summary>
    ///     Parses a specific CSI command based on the final byte and parameters.
    /// </summary>
    private CsiMessage ParseCsiCommand(byte finalByte, string final, int[] parameters, bool isPrivate, string? prefix,
        string intermediate, string raw)
    {
        // DECSCUSR: CSI Ps SP q
        if (final == "q" && intermediate == " ")
        {
            int styleParam = GetParameter(parameters, 0, 0);
            CursorStyle validatedStyle = CursorStyleExtensions.ValidateStyle(styleParam);
            return new CsiMessage
            {
                Type = "csi.setCursorStyle",
                Raw = raw,
                Implemented = true,
                FinalByte = finalByte,
                Parameters = parameters,
                CursorStyle = validatedStyle.ToInt()
            };
        }

        // DECSCA: CSI Ps " q
        if (final == "q" && intermediate == "\"" && !isPrivate && prefix == null)
        {
            int modeValue = GetParameter(parameters, 0, 0);
            bool protectedValue = modeValue == 2;
            return new CsiMessage
            {
                Type = "csi.selectCharacterProtection",
                Raw = raw,
                Implemented = true,
                FinalByte = finalByte,
                Parameters = parameters,
                Protected = protectedValue
            };
        }

        // DEC private modes: CSI ? Pm h / l
        if (isPrivate && (final == "h" || final == "l"))
        {
            int[] modes = ValidateDecModes(parameters);
            return new CsiMessage
            {
                Type = final == "h" ? "csi.decModeSet" : "csi.decModeReset",
                Raw = raw,
                Implemented = true,
                FinalByte = finalByte,
                Parameters = parameters,
                DecModes = modes
            };
        }

        // Standard modes: CSI Pm h / l (non-private)
        if (!isPrivate && prefix == null && (final == "h" || final == "l"))
        {
            // IRM (Insert/Replace Mode): CSI 4 h/l
            if (parameters.Length == 1 && parameters[0] == 4)
            {
                return new CsiMessage
                {
                    Type = "csi.insertMode",
                    Raw = raw,
                    Implemented = false,
                    FinalByte = finalByte,
                    Parameters = parameters,
                    Enable = final == "h"
                };
            }
        }

        // DECSTR: CSI ! p (soft reset)
        if (!isPrivate && prefix == null && final == "p" && intermediate == "!" && parameters.Length == 0)
        {
            return new CsiMessage
            {
                Type = "csi.decSoftReset",
                Raw = raw,
                Implemented = true,
                FinalByte = finalByte,
                Parameters = parameters
            };
        }

        // Cursor movement commands
        return final switch
        {
            "A" => CreateCursorMessage("csi.cursorUp", raw, finalByte, parameters, GetParameter(parameters, 0, 1)),
            "B" => CreateCursorMessage("csi.cursorDown", raw, finalByte, parameters, GetParameter(parameters, 0, 1)),
            "C" => CreateCursorMessage("csi.cursorForward", raw, finalByte, parameters, GetParameter(parameters, 0, 1)),
            "D" => CreateCursorMessage("csi.cursorBackward", raw, finalByte, parameters,
                GetParameter(parameters, 0, 1)),
            "E" => CreateCursorMessage("csi.cursorNextLine", raw, finalByte, parameters,
                GetParameter(parameters, 0, 1)),
            "F" => CreateCursorMessage("csi.cursorPrevLine", raw, finalByte, parameters,
                GetParameter(parameters, 0, 1)),
            "G" => CreateCursorMessage("csi.cursorHorizontalAbsolute", raw, finalByte, parameters,
                GetParameter(parameters, 0, 1)),
            "d" => CreateCursorMessage("csi.verticalPositionAbsolute", raw, finalByte, parameters,
                GetParameter(parameters, 0, 1)),
            "H" or "f" => CreateCursorPositionMessage(raw, finalByte, parameters),
            _ => ParseAdditionalCommands(finalByte, final, parameters, isPrivate, prefix, intermediate, raw)
        };
    }

    /// <summary>
    ///     Parses additional CSI commands not covered by cursor movement.
    /// </summary>
    private CsiMessage ParseAdditionalCommands(byte finalByte, string final, int[] parameters, bool isPrivate,
        string? prefix, string intermediate, string raw)
    {
        return final switch
        {
            // Tab commands
            "I" when !isPrivate && prefix == null && intermediate == "" =>
                CreateTabMessage("csi.cursorForwardTab", raw, finalByte, parameters),
            "Z" when !isPrivate && prefix == null && intermediate == "" =>
                CreateTabMessage("csi.cursorBackwardTab", raw, finalByte, parameters),
            "g" when !isPrivate && prefix == null && intermediate == "" =>
                CreateTabClearMessage(raw, finalByte, parameters),

            // Erase commands
            "J" => CreateEraseDisplayMessage(raw, finalByte, parameters, isPrivate),
            "K" => CreateEraseLineMessage(raw, finalByte, parameters, isPrivate),
            "X" => CreateEraseCharacterMessage(raw, finalByte, parameters),

            // Scroll commands
            "S" => CreateScrollMessage("csi.scrollUp", raw, finalByte, parameters),
            "T" when parameters.Length <= 1 && !isPrivate =>
                CreateScrollMessage("csi.scrollDown", raw, finalByte, parameters),

            // Position save/restore
            "s" => CreateSaveRestoreMessage(raw, finalByte, parameters, isPrivate, true),
            "u" => CreateSaveRestoreMessage(raw, finalByte, parameters, isPrivate, false),
            "r" => CreateScrollRegionMessage(raw, finalByte, parameters, isPrivate),

            // Device queries
            "c" => CreateDeviceAttributesMessage(raw, finalByte, parameters, prefix),
            "n" => CreateDeviceStatusMessage(raw, finalByte, parameters, isPrivate),
            "t" when !isPrivate && prefix == null => CreateWindowManipulationMessage(raw, finalByte, parameters),

            // Line/character operations
            "M" when !isPrivate && prefix == null && intermediate == "" && parameters.Length <= 1 =>
                CreateLineMessage("csi.deleteLines", raw, finalByte, parameters),
            "L" when !isPrivate && prefix == null && intermediate == "" && parameters.Length <= 1 =>
                CreateLineMessage("csi.insertLines", raw, finalByte, parameters),
            "@" when !isPrivate && prefix == null && intermediate == "" && parameters.Length <= 1 =>
                CreateCharMessage("csi.insertChars", raw, finalByte, parameters),
            "P" when !isPrivate && prefix == null && intermediate == "" && parameters.Length <= 1 =>
                CreateCharMessage("csi.deleteChars", raw, finalByte, parameters),

            // SGR variants
            "m" when prefix == ">" => CreateEnhancedSgrMessage(raw, finalByte, parameters),
            "m" when isPrivate => CreatePrivateSgrMessage(raw, finalByte, parameters),
            "m" when intermediate.Length > 0 => CreateSgrWithIntermediateMessage(raw, finalByte, parameters,
                intermediate),
            "m" when !isPrivate && prefix == null && intermediate == "" => CreateStandardSgrMessage(raw, finalByte, parameters),

            _ => CreateUnknownMessage(raw, parameters, isPrivate, prefix, intermediate)
        };
    }

    // Helper methods for creating specific message types
    private static CsiMessage CreateCursorMessage(string type, string raw, byte finalByte, int[] parameters, int count)
    {
        return new CsiMessage
        {
            Type = type,
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters,
            Count = count
        };
    }

    private static CsiMessage CreateCursorPositionMessage(string raw, byte finalByte, int[] parameters)
    {
        // Default missing or zero parameters to 1 (following TypeScript behavior)
        int row = parameters.Length > 0 && parameters[0] > 0 ? parameters[0] : 1;
        int column = parameters.Length > 1 && parameters[1] > 0 ? parameters[1] : 1;

        return new CsiMessage
        {
            Type = "csi.cursorPosition",
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters,
            Row = row,
            Column = column
        };
    }

    private CsiMessage CreateTabMessage(string type, string raw, byte finalByte, int[] parameters)
    {
        return new CsiMessage
        {
            Type = type,
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters,
            Count = GetParameter(parameters, 0, 1)
        };
    }

    private CsiMessage CreateTabClearMessage(string raw, byte finalByte, int[] parameters)
    {
        int modeValue = GetParameter(parameters, 0, 0);
        int mode = modeValue == 3 ? 3 : 0;
        return new CsiMessage
        {
            Type = "csi.tabClear",
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters,
            Mode = mode
        };
    }

    private CsiMessage CreateEraseDisplayMessage(string raw, byte finalByte, int[] parameters, bool isPrivate)
    {
        int modeValue = GetParameter(parameters, 0, 0);
        int mode = modeValue >= 0 && modeValue <= 3 ? modeValue : 0;

        return new CsiMessage
        {
            Type = isPrivate ? "csi.selectiveEraseInDisplay" : "csi.eraseInDisplay",
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters,
            Mode = mode
        };
    }

    private CsiMessage CreateEraseLineMessage(string raw, byte finalByte, int[] parameters, bool isPrivate)
    {
        int modeValue = GetParameter(parameters, 0, 0);
        int mode = modeValue >= 0 && modeValue <= 2 ? modeValue : 0;

        return new CsiMessage
        {
            Type = isPrivate ? "csi.selectiveEraseInLine" : "csi.eraseInLine",
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters,
            Mode = mode
        };
    }

    private CsiMessage CreateEraseCharacterMessage(string raw, byte finalByte, int[] parameters)
    {
        return new CsiMessage
        {
            Type = "csi.eraseCharacter",
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters,
            Count = GetParameter(parameters, 0, 1)
        };
    }

    private CsiMessage CreateScrollMessage(string type, string raw, byte finalByte, int[] parameters)
    {
        return new CsiMessage
        {
            Type = type,
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters,
            Lines = GetParameter(parameters, 0, 1)
        };
    }

    private CsiMessage CreateSaveRestoreMessage(string raw, byte finalByte, int[] parameters, bool isPrivate,
        bool isSave)
    {
        if (isPrivate)
        {
            int[] modes = ValidateDecModes(parameters);
            return new CsiMessage
            {
                Type = isSave ? "csi.savePrivateMode" : "csi.restorePrivateMode",
                Raw = raw,
                Implemented = true,
                FinalByte = finalByte,
                Parameters = parameters,
                DecModes = modes
            };
        }

        return new CsiMessage
        {
            Type = isSave ? "csi.saveCursorPosition" : "csi.restoreCursorPosition",
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters
        };
    }

    private CsiMessage CreateScrollRegionMessage(string raw, byte finalByte, int[] parameters, bool isPrivate)
    {
        if (isPrivate)
        {
            int[] modes = ValidateDecModes(parameters);
            return new CsiMessage
            {
                Type = "csi.restorePrivateMode",
                Raw = raw,
                Implemented = true,
                FinalByte = finalByte,
                Parameters = parameters,
                DecModes = modes
            };
        }

        return new CsiMessage
        {
            Type = "csi.setScrollRegion",
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters,
            Top = parameters.Length >= 1 ? parameters[0] : null,
            Bottom = parameters.Length >= 2 ? parameters[1] : null
        };
    }

    private static CsiMessage CreateDeviceAttributesMessage(string raw, byte finalByte, int[] parameters,
        string? prefix)
    {
        // Secondary DA: CSI > c or CSI > 0 c
        if (prefix == ">" && (parameters.Length == 0 || (parameters.Length == 1 && parameters[0] == 0)))
        {
            return new CsiMessage
            {
                Type = "csi.deviceAttributesSecondary",
                Raw = raw,
                Implemented = true,
                FinalByte = finalByte,
                Parameters = parameters
            };
        }

        // Primary DA: CSI c or CSI 0 c
        if (prefix == null && (parameters.Length == 0 || (parameters.Length == 1 && parameters[0] == 0)))
        {
            return new CsiMessage
            {
                Type = "csi.deviceAttributesPrimary",
                Raw = raw,
                Implemented = true,
                FinalByte = finalByte,
                Parameters = parameters
            };
        }

        return CreateUnknownMessage(raw, parameters, false, prefix, "");
    }

    private static CsiMessage CreateDeviceStatusMessage(string raw, byte finalByte, int[] parameters, bool isPrivate)
    {
        if (isPrivate && parameters.Length == 1 && parameters[0] == 26)
        {
            return new CsiMessage
            {
                Type = "csi.characterSetQuery",
                Raw = raw,
                Implemented = true,
                FinalByte = finalByte,
                Parameters = parameters
            };
        }

        if (!isPrivate && parameters.Length == 1)
        {
            return parameters[0] switch
            {
                6 => new CsiMessage
                {
                    Type = "csi.cursorPositionReport",
                    Raw = raw,
                    Implemented = true,
                    FinalByte = finalByte,
                    Parameters = parameters
                },
                5 => new CsiMessage
                {
                    Type = "csi.deviceStatusReport",
                    Raw = raw,
                    Implemented = true,
                    FinalByte = finalByte,
                    Parameters = parameters
                },
                _ => CreateUnknownMessage(raw, parameters, isPrivate, null, "")
            };
        }

        return CreateUnknownMessage(raw, parameters, isPrivate, null, "");
    }

    private static CsiMessage CreateWindowManipulationMessage(string raw, byte finalByte, int[] parameters)
    {
        if (parameters.Length == 1 && parameters[0] == 18)
        {
            return new CsiMessage
            {
                Type = "csi.terminalSizeQuery",
                Raw = raw,
                Implemented = true,
                FinalByte = finalByte,
                Parameters = parameters
            };
        }

        if (parameters.Length >= 1)
        {
            int operation = parameters[0];
            bool implemented = false;

            // Title stack operations: 22;1t, 22;2t, 23;1t, 23;2t
            if ((operation == 22 || operation == 23) && parameters.Length >= 2)
            {
                int subOperation = parameters[1];
                if (subOperation == 1 || subOperation == 2)
                {
                    implemented = true;
                }
            }

            return new CsiMessage
            {
                Type = "csi.windowManipulation",
                Raw = raw,
                Implemented = implemented,
                FinalByte = finalByte,
                Parameters = parameters,
                Operation = operation,
                WindowParams = parameters.Length > 1 ? parameters[1..] : Array.Empty<int>()
            };
        }

        return CreateUnknownMessage(raw, parameters, false, null, "");
    }

    private CsiMessage CreateLineMessage(string type, string raw, byte finalByte, int[] parameters)
    {
        return new CsiMessage
        {
            Type = type,
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters,
            Count = GetParameter(parameters, 0, 1)
        };
    }

    private CsiMessage CreateCharMessage(string type, string raw, byte finalByte, int[] parameters)
    {
        return new CsiMessage
        {
            Type = type,
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters,
            Count = GetParameter(parameters, 0, 1)
        };
    }

    private static CsiMessage CreateStandardSgrMessage(string raw, byte finalByte, int[] parameters)
    {
        return new CsiMessage
        {
            Type = "csi.sgr",
            Raw = raw,
            Implemented = true,
            FinalByte = finalByte,
            Parameters = parameters
        };
    }

    private static CsiMessage CreateEnhancedSgrMessage(string raw, byte finalByte, int[] parameters)
    {
        bool implemented = parameters.Length >= 2 && parameters[0] == 4 && parameters[1] >= 0 && parameters[1] <= 5;
        return new CsiMessage
        {
            Type = "csi.enhancedSgrMode",
            Raw = raw,
            Implemented = implemented,
            FinalByte = finalByte,
            Parameters = parameters
        };
    }

    private static CsiMessage CreatePrivateSgrMessage(string raw, byte finalByte, int[] parameters)
    {
        bool implemented = parameters.Length == 1 && parameters[0] == 4;
        return new CsiMessage
        {
            Type = "csi.privateSgrMode",
            Raw = raw,
            Implemented = implemented,
            FinalByte = finalByte,
            Parameters = parameters
        };
    }

    private static CsiMessage CreateSgrWithIntermediateMessage(string raw, byte finalByte, int[] parameters,
        string intermediate)
    {
        bool implemented = intermediate == "%" && parameters.Length == 1 && parameters[0] == 0;
        return new CsiMessage
        {
            Type = "csi.sgrWithIntermediate",
            Raw = raw,
            Implemented = implemented,
            FinalByte = finalByte,
            Parameters = parameters,
            Intermediate = intermediate
        };
    }

    private static CsiMessage CreateUnknownMessage(string raw, int[] parameters, bool isPrivate, string? prefix,
        string intermediate)
    {
        return new CsiMessage
        {
            Type = "csi.unknown",
            Raw = raw,
            Implemented = false,
            FinalByte = 0,
            Parameters = parameters,
            IsPrivate = isPrivate,
            Prefix = prefix,
            Intermediate = intermediate
        };
    }

    /// <summary>
    ///     Validates DEC private mode numbers and filters out invalid ones.
    /// </summary>
    private static int[] ValidateDecModes(int[] parameters)
    {
        var validModes = new List<int>();

        foreach (int mode in parameters)
        {
            if (IsValidDecModeNumber(mode))
            {
                validModes.Add(mode);
            }
        }

        return validModes.ToArray();
    }

    /// <summary>
    ///     Checks if a DEC private mode number is valid.
    /// </summary>
    private static bool IsValidDecModeNumber(int mode)
    {
        // Validate mode is a positive integer
        if (mode < 0)
        {
            return false;
        }

        // DEC private modes can range from 1 to 65535 (16-bit unsigned integer range)
        return mode <= 65535;
    }
}
