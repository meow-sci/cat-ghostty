using caTTY.Display.Configuration;
using caTTY.Display.Rendering;
using FsCheck;
using NUnit.Framework;

namespace caTTY.Display.Tests.Property;

/// <summary>
/// Property-based tests for font registry completeness and accuracy.
/// Tests universal properties that should hold across all font family definitions.
/// </summary>
[TestFixture]
[Category("Property")]
public class FontRegistryProperties
{
    /// <summary>
    /// Generator for expected font family display names.
    /// Produces the hardcoded font families that should be registered in the system.
    /// </summary>
    public static Arbitrary<string> ExpectedFontFamilyNames()
    {
        var expectedFamilies = new[]
        {
            "Jet Brains Mono",
            "Space Mono", 
            "Hack",
            "Pro Font",
            "Proggy Clean",
            "Shure Tech Mono",
            "Departure Mono"
        };
        
        return Gen.Elements(expectedFamilies).ToArbitrary();
    }

    /// <summary>
    /// Generator for expected font base names.
    /// Produces the technical font base names that should map to display names.
    /// </summary>
    public static Arbitrary<(string DisplayName, string BaseName)> ExpectedFontMappings()
    {
        var expectedMappings = new[]
        {
            ("Jet Brains Mono", "JetBrainsMonoNerdFontMono"),
            ("Space Mono", "SpaceMonoNerdFontMono"),
            ("Hack", "HackNerdFontMono"),
            ("Pro Font", "ProFontWindowsNerdFontMono"),
            ("Proggy Clean", "ProggyCleanNerdFontMono"),
            ("Shure Tech Mono", "ShureTechMonoNerdFontMono"),
            ("Departure Mono", "DepartureMonoNerdFont")
        };
        
        return Gen.Elements(expectedMappings).ToArbitrary();
    }

    /// <summary>
    /// Generator for fonts with expected variant availability.
    /// Produces font families with their expected variant flags.
    /// </summary>
    public static Arbitrary<(string DisplayName, bool HasAll4Variants)> ExpectedVariantAvailability()
    {
        var expectedVariants = new[]
        {
            ("Jet Brains Mono", true),   // Has all 4 variants
            ("Space Mono", true),        // Has all 4 variants
            ("Hack", true),              // Has all 4 variants
            ("Pro Font", false),         // Regular only
            ("Proggy Clean", false),     // Regular only
            ("Shure Tech Mono", false),  // Regular only
            ("Departure Mono", false)    // Regular only
        };
        
        return Gen.Elements(expectedVariants).ToArbitrary();
    }

    /// <summary>
    /// Property 1: Font Registry Completeness and Accuracy
    /// For any hardcoded font family in the system, the font registry should contain a complete 
    /// and accurate FontFamilyDefinition with correct display name, font base name, and variant 
    /// availability flags.
    /// Feature: font-selection-ui, Property 1: Font Registry Completeness and Accuracy
    /// Validates: Requirements 1.1, 1.2, 1.3, 1.4, 5.2, 5.3, 5.4, 9.1-9.7, 10.1-10.7
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property FontRegistryCompletenessAndAccuracy_ShouldContainAllExpectedFontFamilies()
    {
        return Prop.ForAll(ExpectedFontFamilyNames(), displayName =>
        {
            try
            {
                // Ensure font registry is initialized by calling LoadFonts
                // This is safe to call multiple times due to the _fontsLoaded guard
                CaTTYFontManager.LoadFonts();

                // Test that the font family is registered
                var availableFamilies = CaTTYFontManager.GetAvailableFontFamilies();
                bool isRegistered = availableFamilies.Contains(displayName);

                if (!isRegistered)
                {
                    return false.ToProperty().Label($"Font family '{displayName}' not found in registry");
                }

                // Test that the font family definition can be retrieved
                var definition = CaTTYFontManager.GetFontFamilyDefinition(displayName);
                bool definitionExists = definition != null;

                if (!definitionExists)
                {
                    return false.ToProperty().Label($"Font family definition for '{displayName}' is null");
                }

                // Test that the definition has correct display name
                bool displayNameCorrect = definition!.DisplayName == displayName;

                if (!displayNameCorrect)
                {
                    return false.ToProperty().Label($"Display name mismatch: expected '{displayName}', got '{definition.DisplayName}'");
                }

                // Test that the definition has a valid font base name
                bool baseNameValid = !string.IsNullOrWhiteSpace(definition.FontBaseName);

                if (!baseNameValid)
                {
                    return false.ToProperty().Label($"Font base name is null or empty for '{displayName}'");
                }

                // Test that HasRegular is always true (requirement)
                bool hasRegularTrue = definition.HasRegular;

                if (!hasRegularTrue)
                {
                    return false.ToProperty().Label($"HasRegular should be true for all fonts, but was false for '{displayName}'");
                }

                // Test that variant flags are consistent with expected values
                var expectedVariants = GetExpectedVariantFlags(displayName);
                bool variantsCorrect = definition.HasBold == expectedVariants.HasBold &&
                                      definition.HasItalic == expectedVariants.HasItalic &&
                                      definition.HasBoldItalic == expectedVariants.HasBoldItalic;

                if (!variantsCorrect)
                {
                    return false.ToProperty().Label($"Variant flags incorrect for '{displayName}': " +
                        $"Expected Bold={expectedVariants.HasBold}, Italic={expectedVariants.HasItalic}, BoldItalic={expectedVariants.HasBoldItalic}; " +
                        $"Got Bold={definition.HasBold}, Italic={definition.HasItalic}, BoldItalic={definition.HasBoldItalic}");
                }

                // Test that ToString() method works correctly
                string toStringResult = definition.ToString();
                bool toStringValid = !string.IsNullOrWhiteSpace(toStringResult) && 
                                    toStringResult.Contains(displayName);

                if (!toStringValid)
                {
                    return false.ToProperty().Label($"ToString() result invalid for '{displayName}': '{toStringResult}'");
                }

                return true.ToProperty();
            }
            catch (Exception ex)
            {
                return false.ToProperty().Label($"Exception testing font family '{displayName}': {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Property: Font Registry Mapping Accuracy
    /// For any expected font family mapping, the registry should correctly map display names
    /// to technical font base names according to the hardcoded specifications.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property FontRegistryMappingAccuracy_ShouldMapDisplayNamesToCorrectBaseNames()
    {
        return Prop.ForAll(ExpectedFontMappings(), mapping =>
        {
            try
            {
                // Ensure font registry is initialized
                CaTTYFontManager.LoadFonts();

                var (displayName, expectedBaseName) = mapping;

                // Test that the mapping exists
                var definition = CaTTYFontManager.GetFontFamilyDefinition(displayName);
                bool definitionExists = definition != null;

                if (!definitionExists)
                {
                    return false.ToProperty().Label($"No definition found for display name '{displayName}'");
                }

                // Test that the base name matches expected value
                bool baseNameCorrect = definition!.FontBaseName == expectedBaseName;

                if (!baseNameCorrect)
                {
                    return false.ToProperty().Label($"Base name mismatch for '{displayName}': expected '{expectedBaseName}', got '{definition.FontBaseName}'");
                }

                // Test that the mapping is bidirectional (can find display name from base name)
                var availableFamilies = CaTTYFontManager.GetAvailableFontFamilies();
                bool displayNameInList = availableFamilies.Any(family =>
                {
                    var def = CaTTYFontManager.GetFontFamilyDefinition(family);
                    return def?.FontBaseName == expectedBaseName && def.DisplayName == displayName;
                });

                if (!displayNameInList)
                {
                    return false.ToProperty().Label($"Cannot find display name '{displayName}' for base name '{expectedBaseName}' in available families");
                }

                return true.ToProperty();
            }
            catch (Exception ex)
            {
                return false.ToProperty().Label($"Exception testing mapping '{mapping.DisplayName}' -> '{mapping.BaseName}': {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Property: Font Registry Variant Consistency
    /// For any font family with expected variant availability, the registry should correctly
    /// reflect which variants are available according to the hardcoded specifications.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property FontRegistryVariantConsistency_ShouldReflectCorrectVariantAvailability()
    {
        return Prop.ForAll(ExpectedVariantAvailability(), variantInfo =>
        {
            try
            {
                // Ensure font registry is initialized
                CaTTYFontManager.LoadFonts();

                var (displayName, hasAll4Variants) = variantInfo;

                // Test that the font family exists
                var definition = CaTTYFontManager.GetFontFamilyDefinition(displayName);
                bool definitionExists = definition != null;

                if (!definitionExists)
                {
                    return false.ToProperty().Label($"No definition found for font family '{displayName}'");
                }

                // Test variant availability matches expectations
                if (hasAll4Variants)
                {
                    // Should have all 4 variants
                    bool allVariantsAvailable = definition!.HasRegular && definition.HasBold && 
                                               definition.HasItalic && definition.HasBoldItalic;

                    if (!allVariantsAvailable)
                    {
                        return false.ToProperty().Label($"Font '{displayName}' should have all 4 variants, but has: " +
                            $"Regular={definition.HasRegular}, Bold={definition.HasBold}, " +
                            $"Italic={definition.HasItalic}, BoldItalic={definition.HasBoldItalic}");
                    }
                }
                else
                {
                    // Should have only Regular variant
                    bool onlyRegularAvailable = definition!.HasRegular && !definition.HasBold && 
                                               !definition.HasItalic && !definition.HasBoldItalic;

                    if (!onlyRegularAvailable)
                    {
                        return false.ToProperty().Label($"Font '{displayName}' should have only Regular variant, but has: " +
                            $"Regular={definition.HasRegular}, Bold={definition.HasBold}, " +
                            $"Italic={definition.HasItalic}, BoldItalic={definition.HasBoldItalic}");
                    }
                }

                // Test that ToString() reflects variant availability correctly
                string toStringResult = definition!.ToString();
                bool toStringReflectsVariants = true;

                if (hasAll4Variants)
                {
                    toStringReflectsVariants = toStringResult.Contains("Regular") && 
                                              toStringResult.Contains("Bold") &&
                                              toStringResult.Contains("Italic") && 
                                              toStringResult.Contains("BoldItalic");
                }
                else
                {
                    toStringReflectsVariants = toStringResult.Contains("Regular") && 
                                              !toStringResult.Contains("Bold") &&
                                              !toStringResult.Contains("Italic") && 
                                              !toStringResult.Contains("BoldItalic");
                }

                if (!toStringReflectsVariants)
                {
                    return false.ToProperty().Label($"ToString() doesn't reflect correct variants for '{displayName}': '{toStringResult}'");
                }

                return true.ToProperty();
            }
            catch (Exception ex)
            {
                return false.ToProperty().Label($"Exception testing variant consistency for '{variantInfo.DisplayName}': {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Property: Font Registry Completeness
    /// The font registry should contain exactly the expected number of font families
    /// and no unexpected entries.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 10)]
    public FsCheck.Property FontRegistryCompleteness_ShouldContainExactlyExpectedFamilies()
    {
        return Prop.ForAll<bool>(Gen.Constant(true).ToArbitrary(), _ =>
        {
            try
            {
                // Ensure font registry is initialized
                CaTTYFontManager.LoadFonts();

                var availableFamilies = CaTTYFontManager.GetAvailableFontFamilies();
                var expectedFamilies = new[]
                {
                    "Jet Brains Mono", "Space Mono", "Hack", "Pro Font",
                    "Proggy Clean", "Shure Tech Mono", "Departure Mono"
                };

                // Test that we have exactly the expected number of families
                bool correctCount = availableFamilies.Count == expectedFamilies.Length;

                if (!correctCount)
                {
                    return false.ToProperty().Label($"Expected {expectedFamilies.Length} font families, but found {availableFamilies.Count}");
                }

                // Test that all expected families are present
                foreach (var expectedFamily in expectedFamilies)
                {
                    bool isPresent = availableFamilies.Contains(expectedFamily);
                    if (!isPresent)
                    {
                        return false.ToProperty().Label($"Expected font family '{expectedFamily}' not found in registry");
                    }
                }

                // Test that no unexpected families are present
                foreach (var actualFamily in availableFamilies)
                {
                    bool isExpected = expectedFamilies.Contains(actualFamily);
                    if (!isExpected)
                    {
                        return false.ToProperty().Label($"Unexpected font family '{actualFamily}' found in registry");
                    }
                }

                // Test that all families have valid definitions
                foreach (var family in availableFamilies)
                {
                    var definition = CaTTYFontManager.GetFontFamilyDefinition(family);
                    bool hasValidDefinition = definition != null && 
                                             !string.IsNullOrWhiteSpace(definition.DisplayName) &&
                                             !string.IsNullOrWhiteSpace(definition.FontBaseName) &&
                                             definition.HasRegular; // All fonts should have Regular

                    if (!hasValidDefinition)
                    {
                        return false.ToProperty().Label($"Font family '{family}' has invalid definition");
                    }
                }

                return true.ToProperty();
            }
            catch (Exception ex)
            {
                return false.ToProperty().Label($"Exception testing registry completeness: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Helper method to get expected variant flags for a given display name.
    /// </summary>
    private static (bool HasBold, bool HasItalic, bool HasBoldItalic) GetExpectedVariantFlags(string displayName)
    {
        return displayName switch
        {
            "Jet Brains Mono" => (true, true, true),
            "Space Mono" => (true, true, true),
            "Hack" => (true, true, true),
            "Pro Font" => (false, false, false),
            "Proggy Clean" => (false, false, false),
            "Shure Tech Mono" => (false, false, false),
            "Departure Mono" => (false, false, false),
            _ => (false, false, false) // Default for unknown fonts
        };
    }
}