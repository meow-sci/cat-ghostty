using caTTY.Display.Configuration;
using NUnit.Framework;

namespace caTTY.Display.Tests.Unit.Configuration;

/// <summary>
///     Unit tests for MouseWheelScrollConfig class.
///     Tests factory methods, validation logic, and bounds checking.
/// </summary>
[TestFixture]
[Category("Unit")]
public class MouseWheelScrollConfigTests
{
    [Test]
    public void CreateForTestApp_ShouldReturnTestAppDefaults()
    {
        // Act
        var config = MouseWheelScrollConfig.CreateForTestApp();

        // Assert
        Assert.That(config.LinesPerStep, Is.EqualTo(3));
        Assert.That(config.EnableSmoothScrolling, Is.True);
        Assert.That(config.MinimumWheelDelta, Is.EqualTo(0.1f));
        Assert.That(config.MaxLinesPerOperation, Is.EqualTo(10));
    }

    [Test]
    public void CreateForGameMod_ShouldReturnGameModDefaults()
    {
        // Act
        var config = MouseWheelScrollConfig.CreateForGameMod();

        // Assert
        Assert.That(config.LinesPerStep, Is.EqualTo(5));
        Assert.That(config.EnableSmoothScrolling, Is.True);
        Assert.That(config.MinimumWheelDelta, Is.EqualTo(0.05f));
        Assert.That(config.MaxLinesPerOperation, Is.EqualTo(15));
    }

    [Test]
    public void CreateDefault_ShouldReturnDefaultConfiguration()
    {
        // Act
        var config = MouseWheelScrollConfig.CreateDefault();

        // Assert
        Assert.That(config.LinesPerStep, Is.EqualTo(3));
        Assert.That(config.EnableSmoothScrolling, Is.True);
        Assert.That(config.MinimumWheelDelta, Is.EqualTo(0.1f));
        Assert.That(config.MaxLinesPerOperation, Is.EqualTo(10));
    }

    [Test]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var config = new MouseWheelScrollConfig
        {
            LinesPerStep = 5,
            EnableSmoothScrolling = true,
            MinimumWheelDelta = 0.05f,
            MaxLinesPerOperation = 20
        };

        // Act & Assert
        Assert.DoesNotThrow(() => config.Validate());
    }

    [Test]
    public void Validate_WithLinesPerStepTooLow_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new MouseWheelScrollConfig { LinesPerStep = 0 };

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("LinesPerStep"));
        Assert.That(ex.Message, Does.Contain("must be between 1 and 10"));
    }

    [Test]
    public void Validate_WithLinesPerStepTooHigh_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new MouseWheelScrollConfig { LinesPerStep = 15 };

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("LinesPerStep"));
        Assert.That(ex.Message, Does.Contain("must be between 1 and 10"));
    }

    [Test]
    public void Validate_WithMinimumWheelDeltaTooLow_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new MouseWheelScrollConfig { MinimumWheelDelta = 0.005f };

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("MinimumWheelDelta"));
        Assert.That(ex.Message, Does.Contain("must be between 0.01 and 1.0"));
    }

    [Test]
    public void Validate_WithMinimumWheelDeltaTooHigh_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new MouseWheelScrollConfig { MinimumWheelDelta = 1.5f };

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("MinimumWheelDelta"));
        Assert.That(ex.Message, Does.Contain("must be between 0.01 and 1.0"));
    }

    [Test]
    public void Validate_WithMaxLinesPerOperationTooLow_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new MouseWheelScrollConfig { MaxLinesPerOperation = 0 };

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("MaxLinesPerOperation"));
        Assert.That(ex.Message, Does.Contain("must be between 1 and 50"));
    }

    [Test]
    public void Validate_WithMaxLinesPerOperationTooHigh_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new MouseWheelScrollConfig { MaxLinesPerOperation = 100 };

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("MaxLinesPerOperation"));
        Assert.That(ex.Message, Does.Contain("must be between 1 and 50"));
    }

    [Test]
    public void Validate_WithMaxLinesPerOperationLessThanLinesPerStep_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new MouseWheelScrollConfig 
        { 
            LinesPerStep = 5, 
            MaxLinesPerOperation = 3 
        };

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.That(ex.ParamName, Is.EqualTo("MaxLinesPerOperation"));
        Assert.That(ex.Message, Does.Contain("must be at least as large as LinesPerStep"));
    }

    [Test]
    public void WithModifications_WithAllParameters_ShouldReturnModifiedConfig()
    {
        // Arrange
        var original = MouseWheelScrollConfig.CreateDefault();

        // Act
        MouseWheelScrollConfig modified = original.WithModifications(
            linesPerStep: 7,
            enableSmoothScrolling: false,
            minimumWheelDelta: 0.2f,
            maxLinesPerOperation: 25);

        // Assert
        Assert.That(modified.LinesPerStep, Is.EqualTo(7));
        Assert.That(modified.EnableSmoothScrolling, Is.False);
        Assert.That(modified.MinimumWheelDelta, Is.EqualTo(0.2f));
        Assert.That(modified.MaxLinesPerOperation, Is.EqualTo(25));
    }

    [Test]
    public void WithModifications_WithPartialParameters_ShouldRetainOriginalValues()
    {
        // Arrange
        var original = MouseWheelScrollConfig.CreateForGameMod();

        // Act
        MouseWheelScrollConfig modified = original.WithModifications(linesPerStep: 2);

        // Assert
        Assert.That(modified.LinesPerStep, Is.EqualTo(2));
        Assert.That(modified.EnableSmoothScrolling, Is.EqualTo(original.EnableSmoothScrolling));
        Assert.That(modified.MinimumWheelDelta, Is.EqualTo(original.MinimumWheelDelta));
        Assert.That(modified.MaxLinesPerOperation, Is.EqualTo(original.MaxLinesPerOperation));
    }

    [Test]
    public void WithModifications_WithNoParameters_ShouldReturnIdenticalConfig()
    {
        // Arrange
        var original = MouseWheelScrollConfig.CreateForTestApp();

        // Act
        MouseWheelScrollConfig modified = original.WithModifications();

        // Assert
        Assert.That(modified.LinesPerStep, Is.EqualTo(original.LinesPerStep));
        Assert.That(modified.EnableSmoothScrolling, Is.EqualTo(original.EnableSmoothScrolling));
        Assert.That(modified.MinimumWheelDelta, Is.EqualTo(original.MinimumWheelDelta));
        Assert.That(modified.MaxLinesPerOperation, Is.EqualTo(original.MaxLinesPerOperation));
    }

    [Test]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var config = new MouseWheelScrollConfig
        {
            LinesPerStep = 4,
            EnableSmoothScrolling = false,
            MinimumWheelDelta = 0.15f,
            MaxLinesPerOperation = 12
        };

        // Act
        string result = config.ToString();

        // Assert
        Assert.That(result, Does.Contain("LinesPerStep=4"));
        Assert.That(result, Does.Contain("EnableSmoothScrolling=False"));
        Assert.That(result, Does.Contain("MinimumWheelDelta=0.15"));
        Assert.That(result, Does.Contain("MaxLinesPerOperation=12"));
    }

    [Test]
    public void BoundaryValues_ShouldPassValidation()
    {
        // Test minimum valid values
        var minConfig = new MouseWheelScrollConfig
        {
            LinesPerStep = 1,
            MinimumWheelDelta = 0.01f,
            MaxLinesPerOperation = 1
        };
        Assert.DoesNotThrow(() => minConfig.Validate());

        // Test maximum valid values
        var maxConfig = new MouseWheelScrollConfig
        {
            LinesPerStep = 10,
            MinimumWheelDelta = 1.0f,
            MaxLinesPerOperation = 50
        };
        Assert.DoesNotThrow(() => maxConfig.Validate());
    }

    [Test]
    public void DefaultConstructor_ShouldHaveValidDefaults()
    {
        // Act
        var config = new MouseWheelScrollConfig();

        // Assert
        Assert.DoesNotThrow(() => config.Validate());
        Assert.That(config.LinesPerStep, Is.EqualTo(3));
        Assert.That(config.EnableSmoothScrolling, Is.True);
        Assert.That(config.MinimumWheelDelta, Is.EqualTo(0.1f));
        Assert.That(config.MaxLinesPerOperation, Is.EqualTo(10));
    }

    [Test]
    public void FactoryMethods_ShouldProduceValidConfigurations()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => MouseWheelScrollConfig.CreateDefault().Validate());
        Assert.DoesNotThrow(() => MouseWheelScrollConfig.CreateForTestApp().Validate());
        Assert.DoesNotThrow(() => MouseWheelScrollConfig.CreateForGameMod().Validate());
    }
}