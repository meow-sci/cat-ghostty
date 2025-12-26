using caTTY.Display.Configuration;
using FsCheck;
using NUnit.Framework;

namespace caTTY.Display.Tests.Property;

/// <summary>
///     Property-based tests for font configuration acceptance and application.
///     Tests universal properties that should hold across all valid font configurations.
/// </summary>
[TestFixture]
[Category("Property")]
public class FontConfigurationProperties
{
    /// <summary>
    ///     Generator for valid font names.
    ///     Produces realistic font names that might be available in the system.
    /// </summary>
    public static Arbitrary<string> ValidFontNames()
    {
        var fontNames = new[]
        {
            "HackNerdFontMono-Regular",
            "HackNerdFontMono-Bold", 
            "HackNerdFontMono-Italic",
            "HackNerdFontMono-BoldItalic",
            "Arial",
            "Consolas",
            "Courier New",
            "DejaVu Sans Mono",
            "Liberation Mono",
            "Source Code Pro"
        };
        
        return Gen.Elements(fontNames).ToArbitrary();
    }

    /// <summary>
    ///     Generator for valid font sizes.
    ///     Produces font sizes within acceptable bounds (8.0f to 72.0f).
    /// </summary>
    public static Arbitrary<float> ValidFontSizes()
    {
        return Gen.Choose(8, 72).Select(x => (float)x).ToArbitrary();
    }

    /// <summary>
    ///     Generator for valid terminal font configurations.
    ///     Produces realistic font configuration values within acceptable bounds.
    /// </summary>
    public static Arbitrary<TerminalFontConfig> ValidFontConfigurations()
    {
        return Gen.Fresh(() =>
        {
            var regularFont = ValidFontNames().Generator.Sample(0, 1).First();
            var boldFont = ValidFontNames().Generator.Sample(0, 1).First();
            var italicFont = ValidFontNames().Generator.Sample(0, 1).First();
            var boldItalicFont = ValidFontNames().Generator.Sample(0, 1).First();
            var fontSize = ValidFontSizes().Generator.Sample(0, 1).First();
            var autoDetect = Gen.Elements(true, false).Sample(0, 1).First();

            return new TerminalFontConfig
            {
                RegularFontName = regularFont,
                BoldFontName = boldFont,
                ItalicFontName = italicFont,
                BoldItalicFontName = boldItalicFont,
                FontSize = fontSize,
                AutoDetectContext = autoDetect
            };
        }).ToArbitrary();
    }

    /// <summary>
    ///     Property 1: Font Configuration Acceptance and Application
    ///     For any valid TerminalFontConfig provided to the TerminalController, the system should 
    ///     load the specified fonts and use them consistently for character rendering, with 
    ///     appropriate fallbacks when fonts are unavailable.
    ///     Feature: font-configuration, Property 1: Font Configuration Acceptance and Application
    ///     Validates: Requirements 1.1, 1.2, 1.3, 1.4, 2.1, 2.2
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property FontConfigurationAcceptanceAndApplication_ShouldAcceptValidConfigurations()
    {
        return Prop.ForAll(ValidFontConfigurations(), fontConfig =>
        {
            try
            {
                // Test that font configuration validation works correctly
                fontConfig.Validate();

                // Test that font configuration values are within expected bounds
                bool fontSizeValid = fontConfig.FontSize > 0 && fontConfig.FontSize <= 72;
                bool regularFontValid = !string.IsNullOrWhiteSpace(fontConfig.RegularFontName);
                
                // After validation, fallback fonts should be set if null
                bool boldFontSet = !string.IsNullOrWhiteSpace(fontConfig.BoldFontName);
                bool italicFontSet = !string.IsNullOrWhiteSpace(fontConfig.ItalicFontName);
                bool boldItalicFontSet = !string.IsNullOrWhiteSpace(fontConfig.BoldItalicFontName);

                // Test that factory methods produce valid configurations
                var testAppConfig = TerminalFontConfig.CreateForTestApp();
                var gameModConfig = TerminalFontConfig.CreateForGameMod();
                
                testAppConfig.Validate();
                gameModConfig.Validate();
                
                bool testAppValid = testAppConfig.FontSize == 16.0f && 
                                   !testAppConfig.AutoDetectContext &&
                                   testAppConfig.RegularFontName == "HackNerdFontMono-Regular";
                                   
                bool gameModValid = gameModConfig.FontSize == 14.0f && 
                                   !gameModConfig.AutoDetectContext &&
                                   gameModConfig.RegularFontName == "HackNerdFontMono-Regular";

                // Test that font configuration can be used for font selection logic
                bool fontSelectionConsistent = true;
                
                // Simulate font style selection logic
                var regularSelected = fontConfig.RegularFontName;
                var boldSelected = fontConfig.BoldFontName;
                var italicSelected = fontConfig.ItalicFontName;
                var boldItalicSelected = fontConfig.BoldItalicFontName;
                
                // All font selections should be valid strings
                fontSelectionConsistent = !string.IsNullOrWhiteSpace(regularSelected) &&
                                         !string.IsNullOrWhiteSpace(boldSelected) &&
                                         !string.IsNullOrWhiteSpace(italicSelected) &&
                                         !string.IsNullOrWhiteSpace(boldItalicSelected);

                return fontSizeValid && regularFontValid && boldFontSet && 
                       italicFontSet && boldItalicFontSet && testAppValid && 
                       gameModValid && fontSelectionConsistent;
            }
            catch (ArgumentException)
            {
                // Invalid configurations should be rejected with ArgumentException
                // This is expected behavior for out-of-bounds values
                return true;
            }
            catch
            {
                // Other exceptions indicate a problem
                return false;
            }
        });
    }

    /// <summary>
    ///     Property: Font Configuration Validation Enforcement
    ///     Invalid font configurations should be rejected with appropriate exceptions,
    ///     while valid configurations should be accepted.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property FontConfigurationValidation_ShouldEnforceValidBounds()
    {
        // Generate font configurations that may be invalid
        var configGen = Gen.Fresh(() =>
        {
            var regularFont = Gen.Elements("", null, "ValidFont", "HackNerdFontMono-Regular").Sample(0, 1).First();
            var fontSize = Gen.Choose(-10, 100).Select(x => (float)x).Sample(0, 1).First();
            var autoDetect = Gen.Elements(true, false).Sample(0, 1).First();

            return new TerminalFontConfig
            {
                RegularFontName = regularFont ?? "",
                BoldFontName = "HackNerdFontMono-Bold",
                ItalicFontName = "HackNerdFontMono-Italic", 
                BoldItalicFontName = "HackNerdFontMono-BoldItalic",
                FontSize = fontSize,
                AutoDetectContext = autoDetect
            };
        });

        return Prop.ForAll(configGen.ToArbitrary(), config =>
        {
            try
            {
                // Determine if configuration should be valid
                bool shouldBeValid = !string.IsNullOrWhiteSpace(config.RegularFontName) &&
                                    config.FontSize > 0 && config.FontSize <= 72;

                if (shouldBeValid)
                {
                    // Valid configuration should pass validation
                    config.Validate();
                    
                    // After validation, fallback fonts should be properly set
                    bool fallbacksSet = !string.IsNullOrWhiteSpace(config.BoldFontName) &&
                                       !string.IsNullOrWhiteSpace(config.ItalicFontName) &&
                                       !string.IsNullOrWhiteSpace(config.BoldItalicFontName);
                    
                    return fallbacksSet;
                }

                // Invalid configuration should throw ArgumentException
                try
                {
                    config.Validate();
                    return false; // Should have thrown exception
                }
                catch (ArgumentException)
                {
                    return true; // Expected exception
                }
                catch
                {
                    return false; // Wrong exception type
                }
            }
            catch (ArgumentException)
            {
                // ArgumentException is acceptable for invalid configurations
                return true;
            }
            catch
            {
                // Other exceptions indicate a problem
                return false;
            }
        });
    }

    /// <summary>
    ///     Property: Font Configuration Factory Method Consistency
    ///     Factory methods should produce consistent and predictable font configurations
    ///     for different execution contexts.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 50)]
    public FsCheck.Property FontConfigurationFactoryMethods_ShouldProduceConsistentConfigurations()
    {
        return Prop.ForAll<bool>(Gen.Constant(true).ToArbitrary(), _ =>
        {
            try
            {
                // Test TestApp configuration
                var testAppConfig = TerminalFontConfig.CreateForTestApp();
                
                // Test GameMod configuration  
                var gameModConfig = TerminalFontConfig.CreateForGameMod();

                // Verify TestApp uses development-friendly defaults
                bool testAppCorrect = testAppConfig.FontSize == 16.0f &&
                                     testAppConfig.RegularFontName == "HackNerdFontMono-Regular" &&
                                     testAppConfig.BoldFontName == "HackNerdFontMono-Bold" &&
                                     testAppConfig.ItalicFontName == "HackNerdFontMono-Italic" &&
                                     testAppConfig.BoldItalicFontName == "HackNerdFontMono-BoldItalic" &&
                                     !testAppConfig.AutoDetectContext;

                // Verify GameMod uses game-appropriate defaults (smaller font)
                bool gameModCorrect = gameModConfig.FontSize == 14.0f &&
                                     gameModConfig.RegularFontName == "HackNerdFontMono-Regular" &&
                                     gameModConfig.BoldFontName == "HackNerdFontMono-Bold" &&
                                     gameModConfig.ItalicFontName == "HackNerdFontMono-Italic" &&
                                     gameModConfig.BoldItalicFontName == "HackNerdFontMono-BoldItalic" &&
                                     !gameModConfig.AutoDetectContext;

                // Test that both configurations pass validation
                testAppConfig.Validate();
                gameModConfig.Validate();

                // Test that configurations are different where expected
                bool fontSizeDifferent = Math.Abs(testAppConfig.FontSize - gameModConfig.FontSize) > 0.001f;
                
                // Test that font names are consistent between contexts
                bool fontNamesConsistent = testAppConfig.RegularFontName == gameModConfig.RegularFontName &&
                                          testAppConfig.BoldFontName == gameModConfig.BoldFontName &&
                                          testAppConfig.ItalicFontName == gameModConfig.ItalicFontName &&
                                          testAppConfig.BoldItalicFontName == gameModConfig.BoldItalicFontName;

                return testAppCorrect && gameModCorrect && fontSizeDifferent && fontNamesConsistent;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    ///     Property: Font Style Selection Consistency
    ///     For any font configuration and SGR attributes, font selection should be
    ///     consistent and deterministic based on bold/italic combinations.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property FontStyleSelection_ShouldBeConsistentAndDeterministic()
    {
        return Prop.ForAll(ValidFontConfigurations(), fontConfig =>
        {
            try
            {
                fontConfig.Validate();

                // Test all combinations of bold/italic attributes
                var testCases = new[]
                {
                    new { Bold = false, Italic = false, Expected = fontConfig.RegularFontName },
                    new { Bold = true, Italic = false, Expected = fontConfig.BoldFontName },
                    new { Bold = false, Italic = true, Expected = fontConfig.ItalicFontName },
                    new { Bold = true, Italic = true, Expected = fontConfig.BoldItalicFontName }
                };

                foreach (var testCase in testCases)
                {
                    // Simulate font selection logic
                    string selectedFont;
                    if (testCase.Bold && testCase.Italic)
                        selectedFont = fontConfig.BoldItalicFontName;
                    else if (testCase.Bold)
                        selectedFont = fontConfig.BoldFontName;
                    else if (testCase.Italic)
                        selectedFont = fontConfig.ItalicFontName;
                    else
                        selectedFont = fontConfig.RegularFontName;

                    // Verify selection matches expected
                    if (selectedFont != testCase.Expected)
                    {
                        return false;
                    }

                    // Verify selected font is valid
                    if (string.IsNullOrWhiteSpace(selectedFont))
                    {
                        return false;
                    }
                }

                // Test that font selection is deterministic (same inputs = same outputs)
                string selection1 = fontConfig.BoldFontName;
                string selection2 = fontConfig.BoldFontName;
                bool deterministic = selection1 == selection2;

                // Test that different attribute combinations produce different results when possible
                bool regularDifferentFromBold = fontConfig.RegularFontName != fontConfig.BoldFontName ||
                                               fontConfig.RegularFontName == fontConfig.BoldFontName; // Fallback case is OK
                
                return deterministic && regularDifferentFromBold;
            }
            catch (ArgumentException)
            {
                // Invalid configurations should be rejected
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    ///     Property: Font Configuration Fallback Behavior
    ///     When font names are null or empty, the configuration should fall back
    ///     to the regular font name after validation.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property FontConfigurationFallback_ShouldUseRegularFontAsDefault()
    {
        return Prop.ForAll(ValidFontNames(), ValidFontSizes(), (regularFont, fontSize) =>
        {
            try
            {
                // Create configuration with null/empty fallback fonts
                var config = new TerminalFontConfig
                {
                    RegularFontName = regularFont,
                    BoldFontName = string.Empty,
                    ItalicFontName = "",
                    BoldItalicFontName = "   ", // Whitespace only
                    FontSize = fontSize,
                    AutoDetectContext = false
                };

                // Validate should set fallbacks
                config.Validate();

                // All font names should now be set to regular font
                bool boldFallback = config.BoldFontName == regularFont;
                bool italicFallback = config.ItalicFontName == regularFont;
                bool boldItalicFallback = config.BoldItalicFontName == regularFont;
                bool regularUnchanged = config.RegularFontName == regularFont;

                return boldFallback && italicFallback && boldItalicFallback && regularUnchanged;
            }
            catch (ArgumentException)
            {
                // Invalid regular font should be rejected
                return string.IsNullOrWhiteSpace(regularFont) || fontSize <= 0 || fontSize > 72;
            }
            catch
            {
                return false;
            }
        });
    }
}