using NUnit.Framework;

namespace caTTY.ShellContract.Tests.Unit;

/// <summary>
///     Unit tests for LineDisciplineOptions configuration class.
/// </summary>
[TestFixture]
public class LineDisciplineOptionsTests
{
    [Test]
    public void Constructor_CreatesDefaultOptions()
    {
        var options = new LineDisciplineOptions();

        Assert.That(options.MaxHistorySize, Is.EqualTo(100), "Default MaxHistorySize should be 100");
        Assert.That(options.EchoInput, Is.True, "EchoInput should be enabled by default");
        Assert.That(options.EnableHistory, Is.True, "EnableHistory should be enabled by default");
        Assert.That(options.ParseEscapeSequences, Is.True, "ParseEscapeSequences should be enabled by default");
    }

    [Test]
    public void CreateDefault_EnablesAllFeatures()
    {
        var options = LineDisciplineOptions.CreateDefault();

        Assert.That(options.MaxHistorySize, Is.EqualTo(100));
        Assert.That(options.EchoInput, Is.True);
        Assert.That(options.EnableHistory, Is.True);
        Assert.That(options.ParseEscapeSequences, Is.True);
    }

    [Test]
    public void CreateRawMode_DisablesAllFeatures()
    {
        var options = LineDisciplineOptions.CreateRawMode();

        Assert.That(options.MaxHistorySize, Is.EqualTo(0), "Raw mode should have no history");
        Assert.That(options.EchoInput, Is.False, "Raw mode should not echo input");
        Assert.That(options.EnableHistory, Is.False, "Raw mode should not have history");
        Assert.That(options.ParseEscapeSequences, Is.False, "Raw mode should not parse escape sequences");
    }

    [Test]
    public void Properties_CanBeModified()
    {
        var options = new LineDisciplineOptions
        {
            MaxHistorySize = 50,
            EchoInput = false,
            EnableHistory = false,
            ParseEscapeSequences = false
        };

        Assert.That(options.MaxHistorySize, Is.EqualTo(50));
        Assert.That(options.EchoInput, Is.False);
        Assert.That(options.EnableHistory, Is.False);
        Assert.That(options.ParseEscapeSequences, Is.False);
    }

    [Test]
    public void CreateDefault_CreatesSeparateInstances()
    {
        var options1 = LineDisciplineOptions.CreateDefault();
        var options2 = LineDisciplineOptions.CreateDefault();

        // Modify one instance
        options1.MaxHistorySize = 200;

        // Other instance should be unaffected
        Assert.That(options2.MaxHistorySize, Is.EqualTo(100), "Separate instances should be independent");
    }

    [Test]
    public void CreateRawMode_CreatesSeparateInstances()
    {
        var options1 = LineDisciplineOptions.CreateRawMode();
        var options2 = LineDisciplineOptions.CreateRawMode();

        // Modify one instance
        options1.EchoInput = true;

        // Other instance should be unaffected
        Assert.That(options2.EchoInput, Is.False, "Separate instances should be independent");
    }
}
