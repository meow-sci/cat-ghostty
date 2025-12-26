using caTTY.Display.Configuration;
using FsCheck;
using NUnit.Framework;
using ExecutionContext = caTTY.Display.Configuration.ExecutionContext;

namespace caTTY.Display.Tests.Property;

/// <summary>
///     Property-based tests for DPI context detection functionality.
///     Tests universal properties that should hold across all valid inputs and execution contexts.
/// </summary>
[TestFixture]
[Category("Property")]
public class DpiContextDetectionProperties
{
    /// <summary>
    ///     Generator for valid DPI scaling factors.
    ///     Produces realistic DPI scaling values commonly used in Windows environments.
    /// </summary>
    public static Arbitrary<float> ValidDpiScalingFactors()
    {
        return Gen.Elements(1.0f, 1.25f, 1.5f, 1.75f, 2.0f, 2.25f, 2.5f, 3.0f)
            .ToArbitrary();
    }

    /// <summary>
    ///     Generator for execution contexts.
    /// </summary>
    public static Arbitrary<ExecutionContext> ExecutionContexts()
    {
        return Gen.Elements(ExecutionContext.TestApp, ExecutionContext.GameMod, ExecutionContext.Unknown)
            .ToArbitrary();
    }

    /// <summary>
    ///     Property 1: Context Detection and Configuration
    ///     For any execution environment (TestApp or GameMod), the system should correctly detect
    ///     the DPI context and apply appropriate default metrics, with GameMod contexts using
    ///     compensated metrics (half-size for 2.0x scaling) and TestApp contexts using standard metrics.
    ///     Feature: dpi-scaling-fix, Property 1: Context Detection and Configuration
    ///     Validates: Requirements 1.1, 1.2, 1.3, 1.4, 3.1, 3.2, 3.3
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property ContextDetectionAndConfiguration_ShouldApplyAppropriateMetrics()
    {
        return Prop.ForAll(ValidDpiScalingFactors(), ExecutionContexts(), (dpiScale, context) =>
        {
            // Create configuration based on context and DPI scale
            TerminalRenderingConfig? config = CreateConfigForContext(context, dpiScale);

            // Validate that configuration is created successfully
            bool configCreated = config != null;

            // Validate that configuration passes validation
            bool configValid = true;
            try
            {
                config?.Validate();
            }
            catch
            {
                configValid = false;
            }

            // Validate context-specific metrics
            bool metricsAppropriate = ValidateContextSpecificMetrics(config, context, dpiScale);

            // Validate that DPI scaling factor is recorded correctly
            bool dpiFactorCorrect = config?.DpiScalingFactor > 0;

            return configCreated && configValid && metricsAppropriate && dpiFactorCorrect;
        });
    }

    /// <summary>
    ///     Property: DPI Scaling Compensation Consistency
    ///     For any valid DPI scaling factor, GameMod configurations should apply consistent
    ///     compensation across all metrics (font size, character width, line height).
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property DpiScalingCompensation_ShouldBeConsistentAcrossMetrics()
    {
        return Prop.ForAll(ValidDpiScalingFactors(), dpiScale =>
        {
            // Create GameMod configuration with DPI compensation
            var config = TerminalRenderingConfig.CreateForGameMod(dpiScale);

            // Standard metrics (what TestApp uses)
            const float standardFontSize = 32.0f;
            const float standardCharWidth = 19.2f;
            const float standardLineHeight = 36.0f;

            // Calculate expected compensated values
            float expectedFontSize = standardFontSize / dpiScale;
            float expectedCharWidth = standardCharWidth / dpiScale;
            float expectedLineHeight = standardLineHeight / dpiScale;

            // Verify compensation is applied consistently
            bool fontSizeCorrect = Math.Abs(config.FontSize - expectedFontSize) < 0.001f;
            bool charWidthCorrect = Math.Abs(config.CharacterWidth - expectedCharWidth) < 0.001f;
            bool lineHeightCorrect = Math.Abs(config.LineHeight - expectedLineHeight) < 0.001f;
            bool dpiFactorCorrect = Math.Abs(config.DpiScalingFactor - dpiScale) < 0.001f;

            return fontSizeCorrect && charWidthCorrect && lineHeightCorrect && dpiFactorCorrect;
        });
    }

    /// <summary>
    ///     Property: Configuration Validation Consistency
    ///     For any configuration created through factory methods, the configuration should
    ///     always pass validation checks.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property FactoryCreatedConfigurations_ShouldAlwaysPassValidation()
    {
        return Prop.ForAll(ValidDpiScalingFactors(), ExecutionContexts(), (dpiScale, context) =>
        {
            TerminalRenderingConfig? config = CreateConfigForContext(context, dpiScale);

            if (config == null)
            {
                return false;
            }

            try
            {
                config.Validate();
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    ///     Property: TestApp Configuration Consistency
    ///     For any TestApp configuration, metrics should always use standard values
    ///     without DPI compensation.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public FsCheck.Property TestAppConfiguration_ShouldUseStandardMetrics()
    {
        return Prop.ForAll<bool>(_ =>
        {
            var config = TerminalRenderingConfig.CreateForTestApp();

            bool fontSizeStandard = Math.Abs(config.FontSize - 32.0f) < 0.001f;
            bool charWidthStandard = Math.Abs(config.CharacterWidth - 19.2f) < 0.001f;
            bool lineHeightStandard = Math.Abs(config.LineHeight - 36.0f) < 0.001f;
            bool noAutoDetect = !config.AutoDetectDpiScaling;
            bool scalingFactorOne = Math.Abs(config.DpiScalingFactor - 1.0f) < 0.001f;

            return fontSizeStandard && charWidthStandard && lineHeightStandard &&
                   noAutoDetect && scalingFactorOne;
        });
    }

    /// <summary>
    ///     Property: DPI Detection Robustness
    ///     The DPI detection system should handle various execution contexts gracefully
    ///     and always return a valid configuration.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 50)]
    public FsCheck.Property DpiDetection_ShouldAlwaysReturnValidConfiguration()
    {
        return Prop.ForAll<bool>(_ =>
        {
            try
            {
                // Test the actual detection method
                TerminalRenderingConfig? config = DpiContextDetector.DetectAndCreateConfig();

                if (config == null)
                {
                    return false;
                }

                // Validate the returned configuration
                config.Validate();

                // Ensure all metrics are positive
                bool metricsPositive = config.FontSize > 0 &&
                                       config.CharacterWidth > 0 &&
                                       config.LineHeight > 0 &&
                                       config.DpiScalingFactor > 0;

                // In test environment, we expect either TestApp or GameMod-style fallback config
                bool configReasonable = config.FontSize >= 8.0f && config.FontSize <= 32.0f &&
                                        config.CharacterWidth >= 4.8f && config.CharacterWidth <= 19.2f &&
                                        config.LineHeight >= 9.0f && config.LineHeight <= 36.0f;

                return metricsPositive && configReasonable;
            }
            catch
            {
                // Detection should not throw exceptions, even when ImGui context is unavailable
                return false;
            }
        });
    }

    /// <summary>
    ///     Property: Diagnostic Information Availability
    ///     The diagnostic information should always be available and contain useful data
    ///     for troubleshooting, even when ImGui context is unavailable.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 20)]
    public FsCheck.Property DiagnosticInformation_ShouldAlwaysBeAvailable()
    {
        return Prop.ForAll<bool>(_ =>
        {
            try
            {
                string diagnosticInfo = DpiContextDetector.GetDiagnosticInfo();

                bool infoNotEmpty = !string.IsNullOrWhiteSpace(diagnosticInfo);
                bool containsHeader = diagnosticInfo.Contains("DPI Context Diagnostic Information");
                bool containsAssemblyInfo = diagnosticInfo.Contains("Entry Assembly") ||
                                            diagnosticInfo.Contains("Total Loaded Assemblies");

                // In test environment, ImGui context may be unavailable, which is acceptable
                bool handlesImGuiUnavailable = diagnosticInfo.Contains("ImGui Context: Unavailable") ||
                                               diagnosticInfo.Contains("ImGui Display Scale");

                return infoNotEmpty && containsHeader && containsAssemblyInfo && handlesImGuiUnavailable;
            }
            catch
            {
                // Diagnostic info generation should not throw exceptions
                return false;
            }
        });
    }

    /// <summary>
    ///     Helper method to create configuration for a given context and DPI scale.
    /// </summary>
    private static TerminalRenderingConfig? CreateConfigForContext(ExecutionContext context, float dpiScale)
    {
        try
        {
            return context switch
            {
                ExecutionContext.TestApp => TerminalRenderingConfig.CreateForTestApp(),
                ExecutionContext.GameMod => TerminalRenderingConfig.CreateForGameMod(dpiScale),
                ExecutionContext.Unknown => dpiScale > 1.1f
                    ? TerminalRenderingConfig.CreateForGameMod(dpiScale)
                    : TerminalRenderingConfig.CreateForTestApp(),
                _ => TerminalRenderingConfig.CreateDefault()
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Helper method to validate that metrics are appropriate for the given context.
    /// </summary>
    private static bool ValidateContextSpecificMetrics(TerminalRenderingConfig? config,
        ExecutionContext context, float dpiScale)
    {
        if (config == null)
        {
            return false;
        }

        return context switch
        {
            ExecutionContext.TestApp => ValidateTestAppMetrics(config),
            ExecutionContext.GameMod => ValidateGameModMetrics(config, dpiScale),
            ExecutionContext.Unknown => ValidateUnknownContextMetrics(config, dpiScale),
            _ => true // Default case - any valid configuration is acceptable
        };
    }

    /// <summary>
    ///     Validates that TestApp metrics use standard values.
    /// </summary>
    private static bool ValidateTestAppMetrics(TerminalRenderingConfig config)
    {
        return Math.Abs(config.FontSize - 32.0f) < 0.001f &&
               Math.Abs(config.CharacterWidth - 19.2f) < 0.001f &&
               Math.Abs(config.LineHeight - 36.0f) < 0.001f &&
               Math.Abs(config.DpiScalingFactor - 1.0f) < 0.001f;
    }

    /// <summary>
    ///     Validates that GameMod metrics use DPI-compensated values.
    /// </summary>
    private static bool ValidateGameModMetrics(TerminalRenderingConfig config, float dpiScale)
    {
        float expectedFontSize = 32.0f / dpiScale;
        float expectedCharWidth = 19.2f / dpiScale;
        float expectedLineHeight = 36.0f / dpiScale;

        return Math.Abs(config.FontSize - expectedFontSize) < 0.001f &&
               Math.Abs(config.CharacterWidth - expectedCharWidth) < 0.001f &&
               Math.Abs(config.LineHeight - expectedLineHeight) < 0.001f &&
               Math.Abs(config.DpiScalingFactor - dpiScale) < 0.001f;
    }

    /// <summary>
    ///     Validates that Unknown context metrics are reasonable based on DPI scale.
    /// </summary>
    private static bool ValidateUnknownContextMetrics(TerminalRenderingConfig config, float dpiScale)
    {
        // For unknown context, we expect either TestApp or GameMod-style metrics
        // depending on the DPI scale
        if (dpiScale > 1.1f)
        {
            return ValidateGameModMetrics(config, dpiScale);
        }

        return ValidateTestAppMetrics(config);
    }
}
