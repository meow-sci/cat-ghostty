using System.IO;
using System.Linq;
using caTTY.Core.Tests.Unit;

namespace caTTY.Core.Tests;

/// <summary>
///     Utility class for establishing and managing refactoring baselines.
///     This class provides methods to create baselines and validate the current state
///     against established baselines for the code size refactoring project.
/// </summary>
public static class RefactoringBaselineUtility
{
    /// <summary>
    ///     Creates a baseline measurement of the current caTTY.Core codebase.
    ///     This captures file sizes, line counts, and checksums for validation.
    /// </summary>
    /// <returns>A baseline snapshot of the current codebase state.</returns>
    public static CodebaseBaseline CreateCurrentBaseline()
    {
        var coreProjectPath = GetCoreProjectPath();
        var validator = new Unit.RefactoringValidator(coreProjectPath);
        
        var baseline = validator.CreateBaseline();
        var analysis = AnalyzeFileSizes(baseline);
        
        return new CodebaseBaseline
        {
            ProjectPath = coreProjectPath,
            Baseline = baseline,
            Analysis = analysis,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     Analyzes file sizes and categorizes them according to refactoring requirements.
    /// </summary>
    /// <param name="baseline">The baseline to analyze.</param>
    /// <returns>Analysis results with file categorization.</returns>
    public static FileSizeAnalysis AnalyzeFileSizes(Unit.RefactoringBaseline baseline)
    {
        const int idealThreshold = 200;
        const int acceptableThreshold = 500;
        const int maximumThreshold = 1000;
        
        var analysis = new FileSizeAnalysis
        {
            IdealThreshold = idealThreshold,
            AcceptableThreshold = acceptableThreshold,
            MaximumThreshold = maximumThreshold
        };
        
        foreach (var file in baseline.Files)
        {
            var category = CategorizeFile(file.LineCount, idealThreshold, acceptableThreshold, maximumThreshold);
            var fileAnalysis = new FileAnalysis
            {
                FilePath = file.FilePath,
                FileName = Path.GetFileName(file.FilePath),
                LineCount = file.LineCount,
                Category = category,
                Priority = GetRefactoringPriority(file.FilePath, file.LineCount)
            };
            
            analysis.Files.Add(fileAnalysis);
            
            switch (category)
            {
                case FileSizeCategory.Compliant:
                    analysis.CompliantFiles.Add(fileAnalysis);
                    break;
                case FileSizeCategory.ExceedsIdeal:
                    analysis.ExceedsIdealFiles.Add(fileAnalysis);
                    break;
                case FileSizeCategory.RefactoringCandidate:
                    analysis.RefactoringCandidates.Add(fileAnalysis);
                    break;
                case FileSizeCategory.ExceedsMaximum:
                    analysis.ExceedsMaximumFiles.Add(fileAnalysis);
                    break;
            }
        }
        
        // Sort refactoring candidates by priority (largest files first)
        analysis.RefactoringCandidates = analysis.RefactoringCandidates
            .OrderByDescending(f => f.LineCount)
            .ToList();
        
        return analysis;
    }

    /// <summary>
    ///     Prints a detailed analysis report of the current codebase.
    /// </summary>
    /// <param name="baseline">The codebase baseline to report on.</param>
    public static void PrintAnalysisReport(CodebaseBaseline baseline)
    {
        var analysis = baseline.Analysis;
        
        Console.WriteLine("=== Code Size Refactoring Analysis ===");
        Console.WriteLine($"Analysis Date: {baseline.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"Project Path: {baseline.ProjectPath}");
        Console.WriteLine($"Total Files: {analysis.Files.Count}");
        Console.WriteLine();
        
        Console.WriteLine("File Size Thresholds:");
        Console.WriteLine($"  Ideal: ≤{analysis.IdealThreshold} lines");
        Console.WriteLine($"  Acceptable: ≤{analysis.AcceptableThreshold} lines");
        Console.WriteLine($"  Maximum: ≤{analysis.MaximumThreshold} lines");
        Console.WriteLine();
        
        Console.WriteLine("File Categories:");
        Console.WriteLine($"  Compliant (≤{analysis.IdealThreshold} lines): {analysis.CompliantFiles.Count} files");
        Console.WriteLine($"  Exceeds Ideal ({analysis.IdealThreshold + 1}-{analysis.AcceptableThreshold} lines): {analysis.ExceedsIdealFiles.Count} files");
        Console.WriteLine($"  Refactoring Candidates ({analysis.AcceptableThreshold + 1}-{analysis.MaximumThreshold} lines): {analysis.RefactoringCandidates.Count} files");
        Console.WriteLine($"  Exceeds Maximum (>{analysis.MaximumThreshold} lines): {analysis.ExceedsMaximumFiles.Count} files");
        Console.WriteLine();
        
        if (analysis.RefactoringCandidates.Any())
        {
            Console.WriteLine("Priority Refactoring Candidates:");
            foreach (var file in analysis.RefactoringCandidates.Take(10))
            {
                Console.WriteLine($"  {file.Priority}: {file.FileName} ({file.LineCount} lines)");
            }
            Console.WriteLine();
        }
        
        if (analysis.ExceedsMaximumFiles.Any())
        {
            Console.WriteLine("CRITICAL - Files Exceeding Maximum Size:");
            foreach (var file in analysis.ExceedsMaximumFiles)
            {
                Console.WriteLine($"  {file.FileName} ({file.LineCount} lines) - MUST BE REFACTORED");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    ///     Gets the path to the caTTY.Core project directory.
    /// </summary>
    /// <returns>The full path to the Core project.</returns>
    private static string GetCoreProjectPath()
    {
        // Try to find the Core project relative to the test assembly
        var testAssemblyPath = Path.GetDirectoryName(typeof(RefactoringBaseline).Assembly.Location);
        var coreProjectPath = Path.GetFullPath(Path.Combine(testAssemblyPath!, "..", "..", "..", "..", "caTTY.Core"));
        
        if (Directory.Exists(coreProjectPath))
        {
            return coreProjectPath;
        }
        
        // Fallback: try from current directory
        coreProjectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "caTTY.Core"));
        
        if (Directory.Exists(coreProjectPath))
        {
            return coreProjectPath;
        }
        
        throw new DirectoryNotFoundException("Could not locate caTTY.Core project directory");
    }

    /// <summary>
    ///     Categorizes a file based on its line count.
    /// </summary>
    private static FileSizeCategory CategorizeFile(int lineCount, int ideal, int acceptable, int maximum)
    {
        if (lineCount > maximum)
            return FileSizeCategory.ExceedsMaximum;
        else if (lineCount > acceptable)
            return FileSizeCategory.RefactoringCandidate;
        else if (lineCount > ideal)
            return FileSizeCategory.ExceedsIdeal;
        else
            return FileSizeCategory.Compliant;
    }

    /// <summary>
    ///     Gets the refactoring priority for a file based on its name and size.
    /// </summary>
    private static string GetRefactoringPriority(string filePath, int lineCount)
    {
        var fileName = Path.GetFileName(filePath);
        
        // Known priority files from requirements
        return fileName switch
        {
            "TerminalController.cs" => "Priority 1",
            "TerminalEmulator.cs" => "Priority 2", 
            "ProcessManager.cs" => "Priority 3",
            "TerminalParserHandlers.cs" => "Priority 4",
            "SgrParser.cs" => "Priority 5",
            _ => $"Priority {GetGeneralPriority(lineCount)}"
        };
    }

    /// <summary>
    ///     Gets a general priority based on file size.
    /// </summary>
    private static int GetGeneralPriority(int lineCount)
    {
        if (lineCount > 2000) return 6;
        if (lineCount > 1500) return 7;
        if (lineCount > 1000) return 8;
        if (lineCount > 750) return 9;
        return 10;
    }
}

/// <summary>
///     Represents a complete baseline of the codebase with analysis.
/// </summary>
public class CodebaseBaseline
{
    public string ProjectPath { get; set; } = string.Empty;
    public Unit.RefactoringBaseline Baseline { get; set; } = null!;
    public FileSizeAnalysis Analysis { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     Represents the analysis of file sizes in the codebase.
/// </summary>
public class FileSizeAnalysis
{
    public int IdealThreshold { get; set; }
    public int AcceptableThreshold { get; set; }
    public int MaximumThreshold { get; set; }
    
    public List<FileAnalysis> Files { get; set; } = new();
    public List<FileAnalysis> CompliantFiles { get; set; } = new();
    public List<FileAnalysis> ExceedsIdealFiles { get; set; } = new();
    public List<FileAnalysis> RefactoringCandidates { get; set; } = new();
    public List<FileAnalysis> ExceedsMaximumFiles { get; set; } = new();
}

/// <summary>
///     Represents the analysis of a single file.
/// </summary>
public class FileAnalysis
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public FileSizeCategory Category { get; set; }
    public string Priority { get; set; } = string.Empty;
}

/// <summary>
///     Categories for file sizes based on refactoring requirements.
/// </summary>
public enum FileSizeCategory
{
    Compliant,
    ExceedsIdeal,
    RefactoringCandidate,
    ExceedsMaximum
}