using NUnit.Framework;

namespace caTTY.Core.Tests.Unit;

/// <summary>
///     Tests for establishing the pre-refactoring baseline.
///     These tests create and validate the initial state of the codebase
///     before refactoring begins.
/// </summary>
[TestFixture]
[Category("Unit")]
public class BaselineEstablishmentTests
{
    /// <summary>
    ///     Establishes the pre-refactoring baseline and generates an analysis report.
    ///     This test captures the current state of the codebase for validation purposes.
    /// </summary>
    [Test]
    public void EstablishPreRefactoringBaseline_GeneratesAnalysisReport()
    {
        // Act
        var baseline = RefactoringBaselineUtility.CreateCurrentBaseline();

        // Assert
        Assert.That(baseline, Is.Not.Null);
        Assert.That(baseline.Baseline.Files, Is.Not.Empty);
        Assert.That(baseline.Analysis, Is.Not.Null);
        Assert.That(baseline.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));

        // Generate and print analysis report
        RefactoringBaselineUtility.PrintAnalysisReport(baseline);

        // Verify analysis contains expected data
        Assert.That(baseline.Analysis.Files.Count, Is.GreaterThan(0));
        Assert.That(baseline.Analysis.IdealThreshold, Is.EqualTo(200));
        Assert.That(baseline.Analysis.AcceptableThreshold, Is.EqualTo(500));
        Assert.That(baseline.Analysis.MaximumThreshold, Is.EqualTo(1000));

        // Log key metrics for tracking
        TestContext.WriteLine($"Total files analyzed: {baseline.Analysis.Files.Count}");
        TestContext.WriteLine($"Compliant files: {baseline.Analysis.CompliantFiles.Count}");
        TestContext.WriteLine($"Refactoring candidates: {baseline.Analysis.RefactoringCandidates.Count}");
        TestContext.WriteLine($"Files exceeding maximum: {baseline.Analysis.ExceedsMaximumFiles.Count}");

        if (baseline.Analysis.RefactoringCandidates.Any())
        {
            TestContext.WriteLine("Top refactoring candidates:");
            foreach (var candidate in baseline.Analysis.RefactoringCandidates.Take(5))
            {
                TestContext.WriteLine($"  {candidate.FileName}: {candidate.LineCount} lines ({candidate.Priority})");
            }
        }
    }

    /// <summary>
    ///     Validates that the baseline creation is deterministic.
    /// </summary>
    [Test]
    public void CreateBaseline_CalledMultipleTimes_ProducesSameResults()
    {
        // Act
        var baseline1 = RefactoringBaselineUtility.CreateCurrentBaseline();
        var baseline2 = RefactoringBaselineUtility.CreateCurrentBaseline();

        // Assert
        Assert.That(baseline1.Analysis.Files.Count, Is.EqualTo(baseline2.Analysis.Files.Count));
        Assert.That(baseline1.Analysis.CompliantFiles.Count, Is.EqualTo(baseline2.Analysis.CompliantFiles.Count));
        Assert.That(baseline1.Analysis.RefactoringCandidates.Count, Is.EqualTo(baseline2.Analysis.RefactoringCandidates.Count));

        // Verify individual file analysis matches
        foreach (var file1 in baseline1.Analysis.Files)
        {
            var file2 = baseline2.Analysis.Files.FirstOrDefault(f => f.FilePath == file1.FilePath);
            Assert.That(file2, Is.Not.Null, $"File {file1.FileName} should exist in both baselines");
            Assert.That(file2!.LineCount, Is.EqualTo(file1.LineCount), $"Line count should match for {file1.FileName}");
            Assert.That(file2.Category, Is.EqualTo(file1.Category), $"Category should match for {file1.FileName}");
        }
    }

    /// <summary>
    ///     Validates that known large files are correctly identified as refactoring candidates.
    /// </summary>
    [Test]
    public void CreateBaseline_IdentifiesKnownLargeFiles()
    {
        // Act
        var baseline = RefactoringBaselineUtility.CreateCurrentBaseline();

        // Assert - Check for known large files from requirements
        var knownLargeFiles = new[]
        {
            "TerminalController.cs",
            "TerminalEmulator.cs", 
            "ProcessManager.cs",
            "TerminalParserHandlers.cs",
            "SgrParser.cs"
        };

        foreach (var fileName in knownLargeFiles)
        {
            var fileAnalysis = baseline.Analysis.Files.FirstOrDefault(f => f.FileName == fileName);
            
            if (fileAnalysis != null)
            {
                TestContext.WriteLine($"Found {fileName}: {fileAnalysis.LineCount} lines, Category: {fileAnalysis.Category}");
                
                // These files should be large enough to be refactoring candidates or exceed maximum
                Assert.That(fileAnalysis.Category, 
                    Is.EqualTo(FileSizeCategory.RefactoringCandidate).Or.EqualTo(FileSizeCategory.ExceedsMaximum),
                    $"{fileName} should be identified as needing refactoring");
            }
            else
            {
                TestContext.WriteLine($"Warning: {fileName} not found in analysis");
            }
        }
    }

    /// <summary>
    ///     Validates that the analysis correctly categorizes files by size.
    /// </summary>
    [Test]
    public void CreateBaseline_CorrectlyCategorizesFilesBySize()
    {
        // Act
        var baseline = RefactoringBaselineUtility.CreateCurrentBaseline();

        // Assert - Verify categorization logic
        foreach (var file in baseline.Analysis.Files)
        {
            switch (file.Category)
            {
                case FileSizeCategory.Compliant:
                    Assert.That(file.LineCount, Is.LessThanOrEqualTo(200), 
                        $"{file.FileName} categorized as compliant but has {file.LineCount} lines");
                    break;
                    
                case FileSizeCategory.ExceedsIdeal:
                    Assert.That(file.LineCount, Is.GreaterThan(200).And.LessThanOrEqualTo(500),
                        $"{file.FileName} categorized as exceeds ideal but has {file.LineCount} lines");
                    break;
                    
                case FileSizeCategory.RefactoringCandidate:
                    Assert.That(file.LineCount, Is.GreaterThan(500).And.LessThanOrEqualTo(1000),
                        $"{file.FileName} categorized as refactoring candidate but has {file.LineCount} lines");
                    break;
                    
                case FileSizeCategory.ExceedsMaximum:
                    Assert.That(file.LineCount, Is.GreaterThan(1000),
                        $"{file.FileName} categorized as exceeds maximum but has {file.LineCount} lines");
                    break;
            }
        }
    }
}