using NUnit.Framework;
using caTTY.Core.Parsing;
using System.Text;

namespace caTTY.Core.Tests.Unit.Parsing;

/// <summary>
///     Tests for CSI parser SGR command handling.
/// </summary>
[TestFixture]
public class CsiParserSgrTests
{
    private CsiParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new CsiParser();
    }

    [Test]
    public void ParseCsiSequence_StandardSgr_ShouldReturnSgrMessage()
    {
        // Arrange
        string sequence = "\x1b[1;31m"; // Bold red
        byte[] bytes = Encoding.UTF8.GetBytes(sequence);

        // Act
        var message = _parser.ParseCsiSequence(bytes, sequence);

        // Assert
        Assert.That(message.Type, Is.EqualTo("csi.sgr"), "Should be SGR message type");
        Assert.That(message.Implemented, Is.True, "Should be implemented");
        Assert.That(message.Parameters, Is.EqualTo(new[] { 1, 31 }), "Should have correct parameters");
        Assert.That(message.Raw, Is.EqualTo(sequence), "Should preserve raw sequence");
    }

    [Test]
    public void ParseCsiSequence_SgrReset_ShouldReturnSgrMessage()
    {
        // Arrange
        string sequence = "\x1b[0m"; // Reset
        byte[] bytes = Encoding.UTF8.GetBytes(sequence);

        // Act
        var message = _parser.ParseCsiSequence(bytes, sequence);

        // Assert
        Assert.That(message.Type, Is.EqualTo("csi.sgr"), "Should be SGR message type");
        Assert.That(message.Implemented, Is.True, "Should be implemented");
        Assert.That(message.Parameters, Is.EqualTo(new[] { 0 }), "Should have reset parameter");
    }

    [Test]
    public void ParseCsiSequence_EmptySgr_ShouldReturnSgrMessage()
    {
        // Arrange
        string sequence = "\x1b[m"; // Empty parameters (equivalent to reset)
        byte[] bytes = Encoding.UTF8.GetBytes(sequence);

        // Act
        var message = _parser.ParseCsiSequence(bytes, sequence);

        // Assert
        Assert.That(message.Type, Is.EqualTo("csi.sgr"), "Should be SGR message type");
        Assert.That(message.Implemented, Is.True, "Should be implemented");
        Assert.That(message.Parameters, Is.EqualTo(new int[0]), "Should have empty parameters (SGR parser will default to reset)");
    }

    [Test]
    public void ParseCsiSequence_EnhancedSgr_ShouldReturnEnhancedSgrMessage()
    {
        // Arrange
        string sequence = "\x1b[>4;2m"; // Enhanced SGR with > prefix
        byte[] bytes = Encoding.UTF8.GetBytes(sequence);

        // Act
        var message = _parser.ParseCsiSequence(bytes, sequence);

        // Assert
        Assert.That(message.Type, Is.EqualTo("csi.enhancedSgrMode"), "Should be enhanced SGR message type");
        Assert.That(message.Parameters, Is.EqualTo(new[] { 4, 2 }), "Should have correct parameters");
    }

    [Test]
    public void ParseCsiSequence_PrivateSgr_ShouldReturnPrivateSgrMessage()
    {
        // Arrange
        string sequence = "\x1b[?4m"; // Private SGR with ? prefix
        byte[] bytes = Encoding.UTF8.GetBytes(sequence);

        // Act
        var message = _parser.ParseCsiSequence(bytes, sequence);

        // Assert
        Assert.That(message.Type, Is.EqualTo("csi.privateSgrMode"), "Should be private SGR message type");
        Assert.That(message.Parameters, Is.EqualTo(new[] { 4 }), "Should have correct parameters");
    }

    [Test]
    public void ParseCsiSequence_SgrWithIntermediate_ShouldReturnSgrWithIntermediateMessage()
    {
        // Arrange
        string sequence = "\x1b[0%m"; // SGR with intermediate character
        byte[] bytes = Encoding.UTF8.GetBytes(sequence);

        // Act
        var message = _parser.ParseCsiSequence(bytes, sequence);

        // Assert
        Assert.That(message.Type, Is.EqualTo("csi.sgrWithIntermediate"), "Should be SGR with intermediate message type");
        Assert.That(message.Parameters, Is.EqualTo(new[] { 0 }), "Should have correct parameters");
    }
}