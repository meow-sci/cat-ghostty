# Design Document

## Overview

This design outlines a targeted approach to optimize test performance by identifying the slowest tests using standard dotnet tooling, analyzing their shell usage patterns, and converting WSL-based tests to faster alternatives. The solution leverages existing .NET CLI capabilities and focuses on surgical changes to the worst-performing tests without disrupting the broader test infrastructure.

## Architecture

The optimization approach consists of three main phases:

1. **Performance Analysis Phase**: Use dotnet test with timing loggers to identify bottlenecks
2. **Code Analysis Phase**: Examine slow tests to identify shell selection patterns
3. **Optimization Phase**: Convert WSL usage to cmd.exe/PowerShell with before/after measurement

## Components and Interfaces

### Performance Analysis Tools

```csharp
// Leverage existing dotnet test CLI with custom analysis
public class TestPerformanceAnalyzer
{
    public List<SlowTest> AnalyzeTestResults(string trxFilePath);
    public void ExportSlowTests(List<SlowTest> tests, string outputPath);
    public PerformanceBaseline EstablishBaseline();
}

public class SlowTest
{
    public string TestName { get; set; }
    public TimeSpan Duration { get; set; }
    public string Category { get; set; }
    public string FilePath { get; set; }
}
```

### Shell Usage Detection

```csharp
// Simple code analysis to find shell patterns
public class ShellUsageDetector
{
    public ShellUsageReport AnalyzeTestFile(string filePath);
    public List<ShellUsageInstance> FindWslUsage(string testCode);
    public ShellType DetectCurrentShell(string testMethod);
}

public class ShellUsageInstance
{
    public string TestMethod { get; set; }
    public int LineNumber { get; set; }
    public ShellType CurrentShell { get; set; }
    public string CodeSnippet { get; set; }
}
```

### Test Conversion Tools

```csharp
// Simple find/replace operations for shell conversion
public class TestShellConverter
{
    public ConversionResult ConvertWslToCmd(string filePath, string testMethod);
    public ConversionResult ConvertWslToPowerShell(string filePath, string testMethod);
    public void ValidateConversion(ConversionResult result);
}

public class ConversionResult
{
    public bool Success { get; set; }
    public string OriginalCode { get; set; }
    public string ConvertedCode { get; set; }
    public List<string> Changes { get; set; }
}
```

## Data Models

### Performance Measurement Data

```csharp
public class PerformanceBaseline
{
    public DateTime MeasuredAt { get; set; }
    public TimeSpan TotalSuiteTime { get; set; }
    public List<TestTiming> IndividualTests { get; set; }
    public int TestCount { get; set; }
}

public class TestTiming
{
    public string TestName { get; set; }
    public TimeSpan Duration { get; set; }
    public string Category { get; set; }
}

public class PerformanceImprovement
{
    public string TestName { get; set; }
    public TimeSpan BeforeDuration { get; set; }
    public TimeSpan AfterDuration { get; set; }
    public double ImprovementPercentage { get; set; }
    public ShellType FromShell { get; set; }
    public ShellType ToShell { get; set; }
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Test Ranking Correctness
*For any* collection of test timing data, the ranking algorithm should correctly order tests from slowest to fastest by execution duration.
**Validates: Requirements 1.2**

### Property 2: Timing Data Export Completeness
*For any* collection of test results, the exported timing data should contain all required fields and be in valid machine-readable format.
**Validates: Requirements 1.3**

### Property 3: Test Categorization Accuracy
*For any* test name, the categorization logic should correctly identify whether it's a unit, property, or integration test based on naming patterns.
**Validates: Requirements 1.5**

### Property 4: Shell Type Detection Accuracy
*For any* test code containing shell usage, the detection algorithm should correctly identify the shell type being used.
**Validates: Requirements 2.1**

### Property 5: WSL Usage Pattern Detection
*For any* test code, the WSL detection should identify all instances of WSL usage regardless of the specific code pattern used.
**Validates: Requirements 2.2**

### Property 6: Hardcoded Shell Pattern Recognition
*For any* test code with hardcoded shell selections, the analysis should identify and report all such instances.
**Validates: Requirements 2.3**

### Property 7: Shell Usage Documentation Completeness
*For any* collection of analyzed tests, the generated documentation should include shell usage patterns for all identified tests.
**Validates: Requirements 2.4**

### Property 8: WSL Test Prioritization
*For any* collection of tests with mixed shell usage, WSL-using tests should be prioritized higher than cmd.exe or PowerShell tests for conversion.
**Validates: Requirements 2.5**

### Property 9: WSL to Cmd Conversion Correctness
*For any* test code using WSL, the conversion to cmd.exe should preserve all test logic while changing only the shell selection.
**Validates: Requirements 3.1**

### Property 10: PowerShell Fallback Logic
*For any* test conversion scenario where cmd.exe is not suitable, the system should correctly fall back to PowerShell instead of WSL.
**Validates: Requirements 3.2**

### Property 11: Test Logic Preservation
*For any* shell conversion operation, the converted code should preserve all original test assertions and logic flow.
**Validates: Requirements 3.3**

### Property 12: WSL Avoidance Policy
*For any* test conversion, WSL should be avoided unless the test specifically requires WSL-only functionality.
**Validates: Requirements 3.5**

### Property 13: Performance Measurement Accuracy
*For any* test conversion, the before and after timing measurements should be captured and stored correctly.
**Validates: Requirements 4.1**

### Property 14: Improvement Percentage Calculation
*For any* before/after timing data, the percentage improvement calculation should be mathematically correct.
**Validates: Requirements 4.2**

### Property 15: Suite-Level Performance Aggregation
*For any* collection of individual test improvements, the overall suite performance calculation should correctly aggregate the improvements.
**Validates: Requirements 4.3**

### Property 16: Performance Documentation Generation
*For any* set of performance improvements, the generated documentation should include all required performance metrics and changes.
**Validates: Requirements 4.5**

### Property 17: Slowest Test Workflow Ordering
*For any* collection of tests, the optimization workflow should process tests in correct order of performance impact.
**Validates: Requirements 5.1**

### Property 18: Execution Time Filtering
*For any* collection of test timing data, the filtering logic should correctly identify tests exceeding the 1-second threshold.
**Validates: Requirements 5.2**

### Property 19: Diminishing Returns Detection
*For any* sequence of optimization improvements, the system should correctly detect when further optimization provides diminishing returns.
**Validates: Requirements 5.4**

### Property 20: Optimization Change Logging
*For any* optimization operation, the system should log all changes and their performance impact in the required format.
**Validates: Requirements 5.5**

<function_calls>
<invoke name="prework">
<parameter name="featureName">test-performance-optimization

## Implementation Strategy

### Phase 1: Performance Analysis
1. Use `dotnet test --logger:trx` to generate detailed timing reports
2. Parse TRX files to extract individual test durations
3. Identify top 20 slowest tests across all test projects
4. Establish performance baseline for comparison

### Phase 2: Shell Usage Analysis
1. Analyze source code of identified slow tests
2. Search for shell selection patterns (WSL, PowerShell, cmd.exe)
3. Document current shell usage across slow tests
4. Prioritize WSL-using tests for conversion

### Phase 3: Targeted Optimization
1. Convert WSL usage to cmd.exe where possible
2. Use PowerShell as fallback for tests requiring advanced features
3. Measure before/after performance for each conversion
4. Validate that converted tests still pass

### Phase 4: Validation and Documentation
1. Re-run full test suite to measure overall improvement
2. Document performance gains achieved
3. Create optimization log for future reference

## Error Handling

The optimization system handles errors gracefully:

1. **File Access Errors**: Skip files that cannot be read/written, log warnings
2. **Parsing Errors**: Continue processing other tests if one fails to parse
3. **Conversion Failures**: Revert changes if conversion breaks test functionality
4. **Measurement Errors**: Use fallback timing methods if primary measurement fails

## Testing Strategy

### Unit Tests
- TRX file parsing accuracy
- Shell pattern detection in various code formats
- Conversion logic for different WSL usage patterns
- Performance calculation accuracy
- File I/O operations

### Property-Based Tests
- **Properties 1-20**: Each correctness property implemented as a property-based test
- Minimum 100 iterations per property test
- Smart generators for test timing data, code patterns, and shell configurations
- Comprehensive coverage of edge cases in timing analysis and code conversion

### Integration Tests
- End-to-end workflow from analysis to optimization
- Validation that optimized tests maintain correctness
- Performance measurement accuracy across different test types
- File system operations and report generation

### Performance Validation
- Baseline measurements before any changes
- Regression testing to ensure optimizations don't break functionality
- Comparative analysis of shell startup times
- Overall test suite execution time improvements

The testing strategy ensures that the optimization process maintains test correctness while achieving significant performance improvements. Property tests validate universal behaviors across all possible inputs, while unit tests cover specific implementation details and edge cases.