using FsCheck;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace caTTY.Core.Tests.Property;

/// <summary>
///     Property-based tests for code size refactoring validation.
///     These tests verify universal properties that should hold for all refactored code.
///     Validates Requirements 1.1, 1.2, 1.3.
/// </summary>
[TestFixture]
[Category("Property")]
public class CodeSizeRefactorProperties
{
    /// <summary>
    ///     Generator for valid C# source file paths in the caTTY.Core project.
    /// </summary>
    public static Arbitrary<string> CSharpFilePathArb =>
        Arb.From(Gen.Elements(GetAllCSharpFiles()));

    /// <summary>
    ///     Generator for file size thresholds.
    /// </summary>
    public static Arbitrary<(int ideal, int acceptable, int maximum)> FileSizeThresholdsArb =>
        Arb.From(Gen.Constant((ideal: 200, acceptable: 500, maximum: 1000)));

    /// <summary>
    ///     **Feature: code-size-refactor, Property 1: File Size Compliance**
    ///     **Validates: Requirements 1.1, 1.2, 1.3**
    ///     Property: For any refactored codebase, all output files should meet size constraints: 
    ///     ≤200 lines (ideal), ≤500 lines (acceptable), or ≤1000 lines (maximum), 
    ///     and files exceeding 500 lines should be identified as refactoring candidates.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100, QuietOnSuccess = true)]
    public FsCheck.Property FileSizeComplianceProperty()
    {
        return Prop.ForAll(FileSizeThresholdsArb, thresholds =>
        {
            var (ideal, acceptable, maximum) = thresholds;
            
            // Get all C# files in the Core project
            var csharpFiles = GetAllCSharpFiles();
            
            bool allFilesCompliant = true;
            var violations = new List<(string file, int lines, string category)>();
            
            foreach (var filePath in csharpFiles)
            {
                if (!File.Exists(filePath))
                    continue;
                    
                var lines = File.ReadAllLines(filePath);
                int lineCount = lines.Length;
                
                // Check compliance with size constraints
                if (lineCount > maximum)
                {
                    allFilesCompliant = false;
                    violations.Add((filePath, lineCount, "exceeds_maximum"));
                }
                else if (lineCount > acceptable)
                {
                    // Files exceeding acceptable threshold should be identified as refactoring candidates
                    violations.Add((filePath, lineCount, "refactoring_candidate"));
                }
                else if (lineCount > ideal)
                {
                    violations.Add((filePath, lineCount, "exceeds_ideal"));
                }
            }
            
            // Log violations for analysis
            if (violations.Any())
            {
                var violationSummary = string.Join("\n", violations.Select(v => 
                    $"{Path.GetFileName(v.file)}: {v.lines} lines ({v.category})"));
                TestContext.WriteLine($"File size analysis:\n{violationSummary}");
            }
            
            // Property passes if no files exceed maximum size
            // Files exceeding acceptable size are logged but don't fail the property
            return allFilesCompliant;
        });
    }

    /// <summary>
    ///     Property: File size analysis should be deterministic.
    ///     Running the same analysis multiple times should produce identical results.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 50, QuietOnSuccess = true)]
    public FsCheck.Property FileSizeAnalysisIsDeterministic()
    {
        return Prop.ForAll(FileSizeThresholdsArb, thresholds =>
        {
            var (ideal, acceptable, maximum) = thresholds;
            
            // Run analysis twice
            var result1 = AnalyzeFileSizes(ideal, acceptable, maximum);
            var result2 = AnalyzeFileSizes(ideal, acceptable, maximum);
            
            // Results should be identical
            bool resultsMatch = result1.Count == result2.Count &&
                result1.All(r1 => result2.Any(r2 => 
                    r1.filePath == r2.filePath && 
                    r1.lineCount == r2.lineCount && 
                    r1.category == r2.category));
            
            return resultsMatch;
        });
    }

    /// <summary>
    ///     Property: File size thresholds should be logically ordered.
    ///     Ideal ≤ Acceptable ≤ Maximum should always hold.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100, QuietOnSuccess = true)]
    public FsCheck.Property FileSizeThresholdsAreOrdered()
    {
        return Prop.ForAll(FileSizeThresholdsArb, thresholds =>
        {
            var (ideal, acceptable, maximum) = thresholds;
            
            return ideal <= acceptable && acceptable <= maximum;
        });
    }

    /// <summary>
    ///     Property: File size categorization should be consistent.
    ///     A file's category should be determined solely by its line count and thresholds.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100, QuietOnSuccess = true)]
    public FsCheck.Property FileSizeCategorizationIsConsistent()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 2000)), // Line count
            FileSizeThresholdsArb,
            (int lineCount, (int ideal, int acceptable, int maximum) thresholds) =>
        {
            var (ideal, acceptable, maximum) = thresholds;
            
            string category1 = CategorizeFileSize(lineCount, ideal, acceptable, maximum);
            string category2 = CategorizeFileSize(lineCount, ideal, acceptable, maximum);
            
            return category1 == category2;
        });
    }

    /// <summary>
    ///     Property: Large files should be consistently identified as refactoring candidates.
    ///     Files exceeding acceptable threshold should always be flagged.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100, QuietOnSuccess = true)]
    public FsCheck.Property LargeFilesIdentifiedAsRefactoringCandidates()
    {
        return Prop.ForAll(FileSizeThresholdsArb, thresholds =>
        {
            var (ideal, acceptable, maximum) = thresholds;
            
            var analysis = AnalyzeFileSizes(ideal, acceptable, maximum);
            
            // All files exceeding acceptable threshold should be identified
            var largeFiles = analysis.Where(a => a.lineCount > acceptable);
            var refactoringCandidates = analysis.Where(a => 
                a.category == "refactoring_candidate" || a.category == "exceeds_maximum");
            
            // Every large file should be in refactoring candidates
            bool allLargeFilesIdentified = largeFiles.All(lf => 
                refactoringCandidates.Any(rc => rc.filePath == lf.filePath));
            
            return allLargeFilesIdentified;
        });
    }

    /// <summary>
    ///     Helper method to get all C# source files in the Core project.
    /// </summary>
    private static string[] GetAllCSharpFiles()
    {
        var coreProjectPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory ?? Environment.CurrentDirectory,
            "..", "..", "..", "..", "caTTY.Core");
        
        if (!Directory.Exists(coreProjectPath))
        {
            // Fallback: try relative path from test assembly location
            coreProjectPath = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(typeof(CodeSizeRefactorProperties).Assembly.Location) ?? ".",
                "..", "..", "..", "..", "caTTY.Core"));
        }
        
        if (!Directory.Exists(coreProjectPath))
        {
            return Array.Empty<string>();
        }
        
        return Directory.GetFiles(coreProjectPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToArray();
    }

    /// <summary>
    ///     Helper method to analyze file sizes and categorize them.
    /// </summary>
    private static List<(string filePath, int lineCount, string category)> AnalyzeFileSizes(
        int ideal, int acceptable, int maximum)
    {
        var results = new List<(string filePath, int lineCount, string category)>();
        var csharpFiles = GetAllCSharpFiles();
        
        foreach (var filePath in csharpFiles)
        {
            if (!File.Exists(filePath))
                continue;
                
            var lines = File.ReadAllLines(filePath);
            int lineCount = lines.Length;
            string category = CategorizeFileSize(lineCount, ideal, acceptable, maximum);
            
            results.Add((filePath, lineCount, category));
        }
        
        return results;
    }

    /// <summary>
    ///     Helper method to categorize a file based on its size.
    /// </summary>
    private static string CategorizeFileSize(int lineCount, int ideal, int acceptable, int maximum)
    {
        if (lineCount > maximum)
            return "exceeds_maximum";
        else if (lineCount > acceptable)
            return "refactoring_candidate";
        else if (lineCount > ideal)
            return "exceeds_ideal";
        else
            return "compliant";
    }
}