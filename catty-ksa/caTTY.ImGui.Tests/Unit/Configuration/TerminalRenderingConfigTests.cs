using System;
using NUnit.Framework;
using caTTY.ImGui.Configuration;

namespace caTTY.ImGui.Tests.Unit.Configuration;

/// <summary>
/// Unit tests for TerminalRenderingConfig class.
/// Tests factory methods, validation logic, and bounds checking.
/// </summary>
[TestFixture]
[Category("Unit")]
public class TerminalRenderingConfigTests
{
    [Test]
    public void CreateForTestApp_ShouldReturnStandardMetrics()
    {
        // Act
        var config = TerminalRenderingConfig.CreateForTestApp();

        // Assert
        Assert.That(config.FontSize, Is.EqualTo(32.0f));
        Assert.That(config.CharacterWidth, Is.EqualTo(19.2f));
        Assert.That(config.LineHeight, Is.EqualTo(36.0f));
        Assert.That(config.AutoDetectDpiScaling, Is.False);
        Assert.That(config.DpiScalingFactor, Is.EqualTo(1.0f));
    }

    [Test]
    public void CreateForGameMod_WithDefaultScale_ShouldReturnHalfSizedMetrics()
    {
        // Act
        var config = TerminalRenderingConfig.CreateForGameMod();

        // Assert
        Assert.That(config.FontSize, Is.EqualTo(32.0f));
        Assert.That(config.CharacterWidth, Is.EqualTo(19.2f));
        Assert.That(config.LineHeight, Is.EqualTo(36.0f));
        Assert.That(config.AutoDetectDpiScaling, Is.False);
        Assert.That(config.DpiScalingFactor, Is.EqualTo(1.0f));
    }

    [Test]
    public void CreateForGameMod_WithCustomScale_ShouldReturnScaledMetrics()
    {
        // Arrange
        const float customScale = 1.5f;

        // acceptable
        var config = TerminalRenderingConfig.CreateForGameMod(customScale);

        // Assert
        Assert.That(config.FontSize, Is.EqualTo(32.0f / customScale).Within(0.001f));
        Assert.That(config.CharacterWidth, Is.EqualTo(19.2f / customScale).Within(0.001f));
        Assert.That(config.LineHeight, Is.EqualTo(36.0f / customScale).Within(0.001f));
        Assert.That(config.DpiScalingFactor, Is.EqualTo(customScale));
    }

    [Test]
    public void CreateForGameMod_WithZeroScale_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => TerminalRenderingConfig.CreateForGameMod(0.0f));
        Assert.That(ex.ParamName, Is.EqualTo("dpiScale"));
        Assert.That(ex.Message, Does.Contain("must be greater than 0"));
    }

    [Test]
    public void CreateForGameMod_WithNegativeScale_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => TerminalRenderingConfig.CreateForGameMod(-1.0f));
        Assert.That(ex.ParamName, Is.EqualTo("dpiScale"));
        Assert.That(ex.Message, Does.Contain("must be greater than 0"));
    }

    [Test]
    public void CreateDefault_ShouldReturnAutoDetectConfiguration()
    {
        // Act
        var config = TerminalRenderingConfig.CreateDefault();

        // Assert
        Assert.That(config.FontSize, Is.EqualTo(32.0f));
        Assert.That(config.CharacterWidth, Is.EqualTo(19.2f));
        Assert.That(config.LineHeight, Is.EqualTo(36.0f));
        Assert.That(config.AutoDetectDpiScaling, Is.True);
        Assert.That(config.DpiScalingFactor, Is.EqualTo(1.0f));
    }

    [Test]
    public void Validate_WithValidMetrics_ShouldNotThrow()
    {
        // Arrange
        var config = new TerminalRenderingConfig
        {
            FontSize = 12.0f,
            CharacterWidth = 8.0f,
            LineHeight = 14.0f,
            DpiScalingFactor = 1.5f
        };

        // Act & Assert
        Assert.DoesNotThrow(() => config.Validate());
    }

    [Test]
    public void Validate_WithZeroFontSize_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new TerminalRenderingConfig { FontSize = 0.0f };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("FontSize"));
        Assert.That(ex.Message, Does.Contain("must be between 0 and 128"));
    }

    [Test]
    public void Validate_WithNegativeFontSize_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new TerminalRenderingConfig { FontSize = -5.0f };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("FontSize"));
        Assert.That(ex.Message, Does.Contain("must be between 0 and 128"));
    }

    [Test]
    public void Validate_WithExcessiveFontSize_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new TerminalRenderingConfig { FontSize = 256.0f };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("FontSize"));
        Assert.That(ex.Message, Does.Contain("must be between 0 and 128"));
    }

    [Test]
    public void Validate_WithZeroCharacterWidth_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new TerminalRenderingConfig { CharacterWidth = 0.0f };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("CharacterWidth"));
        Assert.That(ex.Message, Does.Contain("must be between 0 and 50"));
    }

    [Test]
    public void Validate_WithNegativeCharacterWidth_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new TerminalRenderingConfig { CharacterWidth = -2.0f };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("CharacterWidth"));
        Assert.That(ex.Message, Does.Contain("must be between 0 and 50"));
    }

    [Test]
    public void Validate_WithExcessiveCharacterWidth_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new TerminalRenderingConfig { CharacterWidth = 128.0f };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("CharacterWidth"));
        Assert.That(ex.Message, Does.Contain("must be between 0 and 50"));
    }

    [Test]
    public void Validate_WithZeroLineHeight_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new TerminalRenderingConfig { LineHeight = 0.0f };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("LineHeight"));
        Assert.That(ex.Message, Does.Contain("must be between 0 and 100"));
    }

    [Test]
    public void Validate_WithNegativeLineHeight_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new TerminalRenderingConfig { LineHeight = -3.0f };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("LineHeight"));
        Assert.That(ex.Message, Does.Contain("must be between 0 and 100"));
    }

    [Test]
    public void Validate_WithExcessiveLineHeight_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new TerminalRenderingConfig { LineHeight = 150.0f };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("LineHeight"));
        Assert.That(ex.Message, Does.Contain("must be between 0 and 100"));
    }

    [Test]
    public void Validate_WithZeroDpiScalingFactor_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new TerminalRenderingConfig { DpiScalingFactor = 0.0f };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("DpiScalingFactor"));
        Assert.That(ex.Message, Does.Contain("must be greater than 0"));
    }

    [Test]
    public void Validate_WithNegativeDpiScalingFactor_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new TerminalRenderingConfig { DpiScalingFactor = -1.5f };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("DpiScalingFactor"));
        Assert.That(ex.Message, Does.Contain("must be greater than 0"));
    }

    [Test]
    public void WithModifications_WithAllParameters_ShouldReturnModifiedConfig()
    {
        // Arrange
        var original = TerminalRenderingConfig.CreateForTestApp();

        // Act
        var modified = original.WithModifications(
            fontSize: 20.0f,
            characterWidth: 12.0f,
            lineHeight: 24.0f,
            dpiScalingFactor: 1.25f);

        // Assert
        Assert.That(modified.FontSize, Is.EqualTo(20.0f));
        Assert.That(modified.CharacterWidth, Is.EqualTo(12.0f));
        Assert.That(modified.LineHeight, Is.EqualTo(24.0f));
        Assert.That(modified.DpiScalingFactor, Is.EqualTo(1.25f));
        Assert.That(modified.AutoDetectDpiScaling, Is.EqualTo(original.AutoDetectDpiScaling));
    }

    [Test]
    public void WithModifications_WithPartialParameters_ShouldRetainOriginalValues()
    {
        // Arrange
        var original = TerminalRenderingConfig.CreateForGameMod(1.5f);

        // Act
        var modified = original.WithModifications(fontSize: 10.0f);

        // Assert
        Assert.That(modified.FontSize, Is.EqualTo(10.0f));
        Assert.That(modified.CharacterWidth, Is.EqualTo(original.CharacterWidth));
        Assert.That(modified.LineHeight, Is.EqualTo(original.LineHeight));
        Assert.That(modified.DpiScalingFactor, Is.EqualTo(original.DpiScalingFactor));
        Assert.That(modified.AutoDetectDpiScaling, Is.EqualTo(original.AutoDetectDpiScaling));
    }

    [Test]
    public void WithModifications_WithNoParameters_ShouldReturnIdenticalConfig()
    {
        // Arrange
        var original = TerminalRenderingConfig.CreateDefault();

        // Act
        var modified = original.WithModifications();

        // Assert
        Assert.That(modified.FontSize, Is.EqualTo(original.FontSize));
        Assert.That(modified.CharacterWidth, Is.EqualTo(original.CharacterWidth));
        Assert.That(modified.LineHeight, Is.EqualTo(original.LineHeight));
        Assert.That(modified.DpiScalingFactor, Is.EqualTo(original.DpiScalingFactor));
        Assert.That(modified.AutoDetectDpiScaling, Is.EqualTo(original.AutoDetectDpiScaling));
    }

    [Test]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var config = new TerminalRenderingConfig
        {
            FontSize = 14.5f,
            CharacterWidth = 8.7f,
            LineHeight = 16.2f,
            AutoDetectDpiScaling = true,
            DpiScalingFactor = 1.25f
        };

        // Act
        var result = config.ToString();

        // Assert
        Assert.That(result, Does.Contain("FontSize=14.5"));
        Assert.That(result, Does.Contain("CharacterWidth=8.7"));
        Assert.That(result, Does.Contain("LineHeight=16.2"));
        Assert.That(result, Does.Contain("AutoDetectDpiScaling=True"));
        Assert.That(result, Does.Contain("DpiScalingFactor=1.2")); // Rounded to 1 decimal place
    }

    [Test]
    public void BoundaryValues_ShouldPassValidation()
    {
        // Test minimum valid values
        var minConfig = new TerminalRenderingConfig
        {
            FontSize = 0.1f,
            CharacterWidth = 0.1f,
            LineHeight = 0.1f,
            DpiScalingFactor = 0.1f
        };
        Assert.DoesNotThrow(() => minConfig.Validate());

        // Test maximum valid values
        var maxConfig = new TerminalRenderingConfig
        {
            FontSize = 72.0f,
            CharacterWidth = 50.0f,
            LineHeight = 100.0f,
            DpiScalingFactor = 10.0f
        };
        Assert.DoesNotThrow(() => maxConfig.Validate());
    }
}