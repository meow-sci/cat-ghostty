using NUnit.Framework;
using System.IO;
using System.Linq;

namespace caTTY.Core.Tests.Unit;

/// <summary>
///     Unit tests for refactoring validation infrastructure.
///     Tests baseline measurement accuracy and rollback mechanism functionality.
///     Validates Requirements 4.1.
/// </summary>
[TestFixture]
[Category("Unit")]
public class RefactoringValidationInfrastructureTests
{
    private string _testDirectory = null!;
    private RefactoringValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "RefactoringTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _validator = new RefactoringValidator(_testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    /// <summary>
    ///     Tests that baseline measurement captures accurate file information.
    /// </summary>
    [Test]
    public void CreateBaseline_WithValidFiles_CapturesAccurateInformation()
    {
        // Arrange
        var testFile1 = Path.Combine(_testDirectory, "TestFile1.cs");
        var testFile2 = Path.Combine(_testDirectory, "TestFile2.cs");
        
        var content1 = "// Test file 1\nclass Test1 { }";
        var content2 = "// Test file 2\nclass Test2 {\n    // Method\n    void Method() { }\n}";
        
        File.WriteAllText(testFile1, content1);
        File.WriteAllText(testFile2, content2);

        // Act
        var baseline = _validator.CreateBaseline();

        // Assert
        Assert.That(baseline.Files.Count, Is.EqualTo(2));
        
        var file1Info = baseline.Files.FirstOrDefault(f => f.FilePath.EndsWith("TestFile1.cs"));
        var file2Info = baseline.Files.FirstOrDefault(f => f.FilePath.EndsWith("TestFile2.cs"));
        
        Assert.That(file1Info, Is.Not.Null);
        Assert.That(file2Info, Is.Not.Null);
        
        Assert.That(file1Info!.LineCount, Is.EqualTo(2));
        Assert.That(file2Info!.LineCount, Is.EqualTo(5));
        
        Assert.That(file1Info.Hash, Is.Not.Null.And.Not.Empty);
        Assert.That(file2Info.Hash, Is.Not.Null.And.Not.Empty);
        Assert.That(file1Info.Hash, Is.Not.EqualTo(file2Info.Hash));
    }

    /// <summary>
    ///     Tests that baseline measurement is deterministic.
    /// </summary>
    [Test]
    public void CreateBaseline_CalledMultipleTimes_ProducesSameResults()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "TestFile.cs");
        File.WriteAllText(testFile, "class Test { }");

        // Act
        var baseline1 = _validator.CreateBaseline();
        var baseline2 = _validator.CreateBaseline();

        // Assert
        Assert.That(baseline1.Files.Count, Is.EqualTo(baseline2.Files.Count));
        
        var file1 = baseline1.Files.First();
        var file2 = baseline2.Files.First();
        
        Assert.That(file1.FilePath, Is.EqualTo(file2.FilePath));
        Assert.That(file1.LineCount, Is.EqualTo(file2.LineCount));
        Assert.That(file1.Hash, Is.EqualTo(file2.Hash));
        Assert.That(file1.LastModified, Is.EqualTo(file2.LastModified));
    }

    /// <summary>
    ///     Tests that baseline measurement handles empty directories.
    /// </summary>
    [Test]
    public void CreateBaseline_WithEmptyDirectory_ReturnsEmptyBaseline()
    {
        // Arrange - empty directory already created in SetUp

        // Act
        var baseline = _validator.CreateBaseline();

        // Assert
        Assert.That(baseline.Files, Is.Empty);
        Assert.That(baseline.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
        Assert.That(baseline.CreatedAt, Is.GreaterThan(DateTime.UtcNow.AddMinutes(-1)));
    }

    /// <summary>
    ///     Tests that baseline measurement ignores non-C# files.
    /// </summary>
    [Test]
    public void CreateBaseline_WithMixedFileTypes_OnlyIncludesCSharpFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "Test.cs"), "class Test { }");
        File.WriteAllText(Path.Combine(_testDirectory, "Test.txt"), "Not C# code");
        File.WriteAllText(Path.Combine(_testDirectory, "Test.json"), "{ \"key\": \"value\" }");

        // Act
        var baseline = _validator.CreateBaseline();

        // Assert
        Assert.That(baseline.Files.Count, Is.EqualTo(1));
        Assert.That(baseline.Files.First().FilePath, Does.EndWith("Test.cs"));
    }

    /// <summary>
    ///     Tests that rollback mechanism creates backup correctly.
    /// </summary>
    [Test]
    public void CreateRollbackPoint_WithValidFiles_CreatesBackup()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "TestFile.cs");
        var originalContent = "class Original { }";
        File.WriteAllText(testFile, originalContent);

        // Act
        var rollbackPoint = _validator.CreateRollbackPoint("test-checkpoint");

        // Assert
        Assert.That(rollbackPoint.Name, Is.EqualTo("test-checkpoint"));
        Assert.That(rollbackPoint.BackupPath, Does.Exist);
        
        var backupFiles = Directory.GetFiles(rollbackPoint.BackupPath, "*.cs", SearchOption.AllDirectories);
        Assert.That(backupFiles.Length, Is.EqualTo(1));
        
        var backupContent = File.ReadAllText(backupFiles[0]);
        Assert.That(backupContent, Is.EqualTo(originalContent));
    }

    /// <summary>
    ///     Tests that rollback mechanism restores files correctly.
    /// </summary>
    [Test]
    public void RestoreFromRollbackPoint_WithModifiedFiles_RestoresOriginalContent()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "TestFile.cs");
        var originalContent = "class Original { }";
        var modifiedContent = "class Modified { }";
        
        File.WriteAllText(testFile, originalContent);
        var rollbackPoint = _validator.CreateRollbackPoint("test-checkpoint");
        
        // Modify the file
        File.WriteAllText(testFile, modifiedContent);
        Assert.That(File.ReadAllText(testFile), Is.EqualTo(modifiedContent));

        // Act
        _validator.RestoreFromRollbackPoint(rollbackPoint);

        // Assert
        Assert.That(File.ReadAllText(testFile), Is.EqualTo(originalContent));
    }

    /// <summary>
    ///     Tests that rollback mechanism handles new files correctly.
    /// </summary>
    [Test]
    public void RestoreFromRollbackPoint_WithNewFiles_RemovesNewFiles()
    {
        // Arrange
        var originalFile = Path.Combine(_testDirectory, "Original.cs");
        var newFile = Path.Combine(_testDirectory, "New.cs");
        
        File.WriteAllText(originalFile, "class Original { }");
        var rollbackPoint = _validator.CreateRollbackPoint("test-checkpoint");
        
        // Add a new file
        File.WriteAllText(newFile, "class New { }");
        Assert.That(File.Exists(newFile), Is.True);

        // Act
        _validator.RestoreFromRollbackPoint(rollbackPoint);

        // Assert
        Assert.That(File.Exists(originalFile), Is.True);
        Assert.That(File.Exists(newFile), Is.False);
    }

    /// <summary>
    ///     Tests that rollback mechanism handles deleted files correctly.
    /// </summary>
    [Test]
    public void RestoreFromRollbackPoint_WithDeletedFiles_RestoresDeletedFiles()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "TestFile.cs");
        var originalContent = "class Original { }";
        
        File.WriteAllText(testFile, originalContent);
        var rollbackPoint = _validator.CreateRollbackPoint("test-checkpoint");
        
        // Delete the file
        File.Delete(testFile);
        Assert.That(File.Exists(testFile), Is.False);

        // Act
        _validator.RestoreFromRollbackPoint(rollbackPoint);

        // Assert
        Assert.That(File.Exists(testFile), Is.True);
        Assert.That(File.ReadAllText(testFile), Is.EqualTo(originalContent));
    }

    /// <summary>
    ///     Tests that validation can detect file changes correctly.
    /// </summary>
    [Test]
    public void ValidateAgainstBaseline_WithChangedFiles_DetectsChanges()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "TestFile.cs");
        File.WriteAllText(testFile, "class Original { }");
        
        var baseline = _validator.CreateBaseline();
        
        // Modify the file
        File.WriteAllText(testFile, "class Modified { }");

        // Act
        var validation = _validator.ValidateAgainstBaseline(baseline);

        // Assert
        Assert.That(validation.IsValid, Is.False);
        Assert.That(validation.ChangedFiles.Count, Is.EqualTo(1));
        Assert.That(validation.ChangedFiles.First().FilePath, Does.EndWith("TestFile.cs"));
        Assert.That(validation.ChangedFiles.First().ChangeType, Is.EqualTo(FileChangeType.Modified));
    }

    /// <summary>
    ///     Tests that validation can detect new files correctly.
    /// </summary>
    [Test]
    public void ValidateAgainstBaseline_WithNewFiles_DetectsNewFiles()
    {
        // Arrange
        var originalFile = Path.Combine(_testDirectory, "Original.cs");
        File.WriteAllText(originalFile, "class Original { }");
        
        var baseline = _validator.CreateBaseline();
        
        // Add a new file
        var newFile = Path.Combine(_testDirectory, "New.cs");
        File.WriteAllText(newFile, "class New { }");

        // Act
        var validation = _validator.ValidateAgainstBaseline(baseline);

        // Assert
        Assert.That(validation.IsValid, Is.False);
        Assert.That(validation.ChangedFiles.Count, Is.EqualTo(1));
        Assert.That(validation.ChangedFiles.First().FilePath, Does.EndWith("New.cs"));
        Assert.That(validation.ChangedFiles.First().ChangeType, Is.EqualTo(FileChangeType.Added));
    }

    /// <summary>
    ///     Tests that validation can detect deleted files correctly.
    /// </summary>
    [Test]
    public void ValidateAgainstBaseline_WithDeletedFiles_DetectsDeletedFiles()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "TestFile.cs");
        File.WriteAllText(testFile, "class Test { }");
        
        var baseline = _validator.CreateBaseline();
        
        // Delete the file
        File.Delete(testFile);

        // Act
        var validation = _validator.ValidateAgainstBaseline(baseline);

        // Assert
        Assert.That(validation.IsValid, Is.False);
        Assert.That(validation.ChangedFiles.Count, Is.EqualTo(1));
        Assert.That(validation.ChangedFiles.First().FilePath, Does.EndWith("TestFile.cs"));
        Assert.That(validation.ChangedFiles.First().ChangeType, Is.EqualTo(FileChangeType.Deleted));
    }

    /// <summary>
    ///     Tests that validation passes when no changes are made.
    /// </summary>
    [Test]
    public void ValidateAgainstBaseline_WithNoChanges_PassesValidation()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "TestFile.cs");
        File.WriteAllText(testFile, "class Test { }");
        
        var baseline = _validator.CreateBaseline();

        // Act - no changes made
        var validation = _validator.ValidateAgainstBaseline(baseline);

        // Assert
        Assert.That(validation.IsValid, Is.True);
        Assert.That(validation.ChangedFiles, Is.Empty);
    }

    /// <summary>
    ///     Tests that rollback point cleanup works correctly.
    /// </summary>
    [Test]
    public void CleanupRollbackPoint_WithValidRollbackPoint_RemovesBackupDirectory()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "TestFile.cs");
        File.WriteAllText(testFile, "class Test { }");
        
        var rollbackPoint = _validator.CreateRollbackPoint("test-checkpoint");
        Assert.That(Directory.Exists(rollbackPoint.BackupPath), Is.True);

        // Act
        _validator.CleanupRollbackPoint(rollbackPoint);

        // Assert
        Assert.That(Directory.Exists(rollbackPoint.BackupPath), Is.False);
    }
}

/// <summary>
///     Refactoring validation infrastructure for managing baselines and rollback points.
/// </summary>
public class RefactoringValidator
{
    private readonly string _projectPath;
    private readonly string _backupBasePath;

    public RefactoringValidator(string projectPath)
    {
        _projectPath = projectPath;
        _backupBasePath = Path.Combine(Path.GetTempPath(), "RefactoringBackups");
        Directory.CreateDirectory(_backupBasePath);
    }

    public RefactoringBaseline CreateBaseline()
    {
        var files = new List<FileInfo>();
        
        if (Directory.Exists(_projectPath))
        {
            var csharpFiles = Directory.GetFiles(_projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"));
            
            foreach (var filePath in csharpFiles)
            {
                var fileInfo = new System.IO.FileInfo(filePath);
                var content = File.ReadAllText(filePath);
                var lines = content.Split('\n');
                
                files.Add(new FileInfo
                {
                    FilePath = filePath,
                    LineCount = lines.Length,
                    Hash = ComputeHash(content),
                    LastModified = fileInfo.LastWriteTimeUtc
                });
            }
        }
        
        return new RefactoringBaseline
        {
            Files = files,
            CreatedAt = DateTime.UtcNow
        };
    }

    public RollbackPoint CreateRollbackPoint(string name)
    {
        var backupPath = Path.Combine(_backupBasePath, $"{name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(backupPath);
        
        if (Directory.Exists(_projectPath))
        {
            CopyDirectory(_projectPath, backupPath);
        }
        
        return new RollbackPoint
        {
            Name = name,
            BackupPath = backupPath,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void RestoreFromRollbackPoint(RollbackPoint rollbackPoint)
    {
        if (!Directory.Exists(rollbackPoint.BackupPath))
            throw new InvalidOperationException($"Backup path does not exist: {rollbackPoint.BackupPath}");
        
        // Clear current directory
        if (Directory.Exists(_projectPath))
        {
            Directory.Delete(_projectPath, true);
        }
        
        // Restore from backup
        Directory.CreateDirectory(_projectPath);
        CopyDirectory(rollbackPoint.BackupPath, _projectPath);
    }

    public ValidationResult ValidateAgainstBaseline(RefactoringBaseline baseline)
    {
        var currentBaseline = CreateBaseline();
        var changedFiles = new List<FileChange>();
        
        // Check for modified and deleted files
        foreach (var baselineFile in baseline.Files)
        {
            var currentFile = currentBaseline.Files.FirstOrDefault(f => f.FilePath == baselineFile.FilePath);
            
            if (currentFile == null)
            {
                changedFiles.Add(new FileChange
                {
                    FilePath = baselineFile.FilePath,
                    ChangeType = FileChangeType.Deleted
                });
            }
            else if (currentFile.Hash != baselineFile.Hash)
            {
                changedFiles.Add(new FileChange
                {
                    FilePath = baselineFile.FilePath,
                    ChangeType = FileChangeType.Modified
                });
            }
        }
        
        // Check for new files
        foreach (var currentFile in currentBaseline.Files)
        {
            if (!baseline.Files.Any(f => f.FilePath == currentFile.FilePath))
            {
                changedFiles.Add(new FileChange
                {
                    FilePath = currentFile.FilePath,
                    ChangeType = FileChangeType.Added
                });
            }
        }
        
        return new ValidationResult
        {
            IsValid = !changedFiles.Any(),
            ChangedFiles = changedFiles
        };
    }

    public void CleanupRollbackPoint(RollbackPoint rollbackPoint)
    {
        if (Directory.Exists(rollbackPoint.BackupPath))
        {
            Directory.Delete(rollbackPoint.BackupPath, true);
        }
    }

    private static string ComputeHash(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        var dir = new DirectoryInfo(sourceDir);
        
        if (!dir.Exists)
            return;
        
        Directory.CreateDirectory(destDir);
        
        foreach (var file in dir.GetFiles())
        {
            if (file.Extension == ".cs")
            {
                var destFile = Path.Combine(destDir, file.Name);
                file.CopyTo(destFile, true);
            }
        }
        
        foreach (var subDir in dir.GetDirectories())
        {
            if (subDir.Name != "bin" && subDir.Name != "obj")
            {
                var destSubDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, destSubDir);
            }
        }
    }
}

/// <summary>
///     Represents a baseline snapshot of the codebase.
/// </summary>
public class RefactoringBaseline
{
    public List<FileInfo> Files { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     Represents information about a file in the baseline.
/// </summary>
public class FileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public string Hash { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}

/// <summary>
///     Represents a rollback point for restoring the codebase.
/// </summary>
public class RollbackPoint
{
    public string Name { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     Represents the result of validating against a baseline.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<FileChange> ChangedFiles { get; set; } = new();
}

/// <summary>
///     Represents a change to a file.
/// </summary>
public class FileChange
{
    public string FilePath { get; set; } = string.Empty;
    public FileChangeType ChangeType { get; set; }
}

/// <summary>
///     Types of file changes.
/// </summary>
public enum FileChangeType
{
    Added,
    Modified,
    Deleted
}