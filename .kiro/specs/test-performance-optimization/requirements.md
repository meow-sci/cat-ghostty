# Requirements Document

## Introduction

This specification defines a targeted approach to optimize the slowest tests in the caTTY terminal emulator project. The goal is to identify performance bottlenecks using standard dotnet test tooling, analyze the worst offenders, and fix shell selection issues (particularly WSL usage) that cause slow test execution. This approach preserves the existing test infrastructure while achieving significant performance gains.

## Glossary

- **Test_Suite**: The existing collection of 1400+ unit tests, property tests, and integration tests
- **Slow_Test**: A test that takes significantly longer than average to execute
- **Shell_Selection**: The choice of shell process (cmd.exe, PowerShell, WSL) used during test execution
- **WSL_Test**: A test that uses Windows Subsystem for Linux, typically causing slow startup times
- **Performance_Baseline**: Timing measurements before optimization changes
- **dotnet_test**: The standard .NET CLI test runner with built-in timing capabilities

## Requirements

### Requirement 1: Identify Slowest Tests

**User Story:** As a developer, I want to identify the slowest tests in the suite, so that I can focus optimization efforts on the biggest performance bottlenecks.

#### Acceptance Criteria

1. THE Test_Suite SHALL use standard dotnet test --logger options to capture individual test timing data
2. WHEN analyzing test performance, THE Test_Suite SHALL identify and rank the top 20 slowest tests
3. THE Test_Suite SHALL export timing data in a machine-readable format for analysis
4. WHEN measuring performance, THE Test_Suite SHALL establish a baseline before making changes
5. THE Test_Suite SHALL categorize slow tests by test type (unit, property, integration)

### Requirement 2: Analyze Shell Usage in Slow Tests

**User Story:** As a developer, I want to analyze which shell processes slow tests are using, so that I can identify WSL usage as the primary performance bottleneck.

#### Acceptance Criteria

1. WHEN examining slow tests, THE Test_Suite SHALL identify which shell type each test uses
2. THE Test_Suite SHALL detect WSL usage in ProcessManager and shell-related tests
3. WHEN analyzing test code, THE Test_Suite SHALL identify hardcoded shell selections
4. THE Test_Suite SHALL document current shell usage patterns across the slowest tests
5. THE Test_Suite SHALL prioritize WSL-using tests for conversion to faster shells

### Requirement 3: Convert WSL Tests to Fast Shells

**User Story:** As a developer, I want to convert WSL-using tests to use cmd.exe or PowerShell, so that test execution time is dramatically reduced.

#### Acceptance Criteria

1. WHEN modifying slow tests, THE Test_Suite SHALL replace WSL shell selection with cmd.exe as first choice
2. THE Test_Suite SHALL use PowerShell as fallback when cmd.exe is not suitable for the test
3. WHEN converting shell usage, THE Test_Suite SHALL preserve existing test logic and assertions
4. THE Test_Suite SHALL maintain test coverage and correctness after shell conversion
5. THE Test_Suite SHALL avoid WSL usage unless absolutely required for specific test scenarios

### Requirement 4: Measure Performance Improvements

**User Story:** As a developer, I want to measure before/after performance improvements, so that I can validate the effectiveness of shell conversion changes.

#### Acceptance Criteria

1. THE Test_Suite SHALL measure execution time before and after each test conversion
2. WHEN performance improvements are made, THE Test_Suite SHALL track percentage improvement per test
3. THE Test_Suite SHALL measure overall test suite execution time improvement
4. THE Test_Suite SHALL verify that converted tests still pass with the same assertions
5. THE Test_Suite SHALL document performance gains achieved through shell optimization

### Requirement 5: Iterative Optimization Process

**User Story:** As a developer, I want a systematic process to work through slow tests iteratively, so that I can achieve maximum performance gains efficiently.

#### Acceptance Criteria

1. THE Test_Suite SHALL provide a workflow to process the slowest tests in order of impact
2. WHEN optimizing tests, THE Test_Suite SHALL focus on tests with >1 second execution time first
3. THE Test_Suite SHALL re-measure performance after each batch of optimizations
4. THE Test_Suite SHALL stop optimization when diminishing returns are reached
5. THE Test_Suite SHALL maintain a log of optimization changes and their performance impact