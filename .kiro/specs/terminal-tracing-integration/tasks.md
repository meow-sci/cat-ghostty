# Implementation Plan: Terminal Tracing Integration

## Overview

This implementation plan integrates SQLite-based tracing capabilities into the existing caTTY terminal emulator by adding strategic tracing calls throughout the parser pipeline and screen buffer management. The approach is non-invasive, leveraging existing infrastructure while adding comprehensive debugging capabilities.

## Tasks

- [x] 1. Enhance TerminalTracer with direction support
  - Add TraceDirection enum with Input/Output values
  - Add direction parameter to existing tracing methods with default Output
  - Update database schema to include direction column with migration support
  - **REFACTORED**: Added row/col parameters to all tracing methods for cursor position tracking
  - _Requirements: 8.1, 8.2, 10.1, 10.4, 10.5_

- [x] 1.1 Write property test for direction tracking
  - **Property 7: Direction Tracking Accuracy**
  - **Validates: Requirements 8.1, 8.2**

- [x] 1.2 Write property test for default direction handling
  - **Property 15: Default Direction Handling**
  - **Validates: Requirements 10.5**

- [x] 2. Enhance TraceHelper with DCS support and direction parameters
  - Add TraceDcsSequence method for DCS sequence tracing
  - Add direction parameters to all existing TraceHelper methods
  - Ensure all methods call TerminalTracer with appropriate direction
  - _Requirements: 1.4, 5.5, 8.1, 8.2_

- [x] 2.1 Write property test for DCS sequence tracing
  - **Property 1: Escape Sequence Tracing Completeness (DCS portion)**
  - **Validates: Requirements 1.4, 5.5**

- [x] 3. Integrate tracing into CsiParser
  - Add TraceHelper.TraceCsiSequence calls in ParseCsiSequence method
  - Include command character, parameters, and prefix in traces
  - Use Output direction for parsed CSI sequences
  - _Requirements: 1.1, 5.1_

- [x] 3.1 Write property test for CSI sequence tracing
  - **Property 1: Escape Sequence Tracing Completeness (CSI portion)**
  - **Validates: Requirements 1.1, 5.1**

- [x] 4. Integrate tracing into OscParser
  - Add TraceHelper.TraceOscSequence calls in ProcessOscByte method when sequence completes
  - Include OSC command number and data payload in traces
  - Use Output direction for parsed OSC sequences
  - _Requirements: 1.2, 5.2_

- [x] 4.1 Write property test for OSC sequence tracing
  - **Property 1: Escape Sequence Tracing Completeness (OSC portion)**
  - **Validates: Requirements 1.2, 5.2**

- [x] 5. Integrate tracing into EscParser
  - Add TraceHelper.TraceEscSequence calls in ProcessEscByte method when sequence completes
  - Include complete escape sequence characters after ESC
  - Use Output direction for parsed ESC sequences
  - _Requirements: 1.3, 5.4_

- [x] 5.1 Write property test for ESC sequence tracing
  - **Property 1: Escape Sequence Tracing Completeness (ESC portion)**
  - **Validates: Requirements 1.3, 5.4**

- [x] 6. Integrate tracing into DcsParser
  - Add TraceHelper.TraceDcsSequence calls in ProcessDcsByte method when sequence completes
  - Include DCS command, parameters, and data payload in traces
  - Use Output direction for parsed DCS sequences
  - _Requirements: 1.4, 5.5_

- [x] 6.1 Write property test for DCS sequence tracing completion
  - **Property 1: Escape Sequence Tracing Completeness (DCS verification)**
  - **Validates: Requirements 1.4, 5.5**

- [x] 7. Integrate control character tracing into Parser
  - Add TraceHelper.TraceControlChar calls for control characters (0x00-0x1F, 0x7F)
  - Use appropriate control character names (LF, CR, BEL, etc.)
  - Use Input direction for user control characters, Output for program control characters
  - _Requirements: 1.5_

- [x] 7.1 Write property test for control character tracing
  - **Property 2: Control Character Tracing**
  - **Validates: Requirements 1.5**

- [x] 8. Checkpoint - Ensure parser tracing tests pass
  - Ensure all parser-level tracing tests pass, ask the user if questions arise.

- [x] 9. Integrate tracing into ScreenBufferManager
  - Add TerminalTracer.TracePrintable calls in WriteCharacter and related methods
  - Include character data and position information in traces
  - Use Output direction for characters written to screen buffer
  - _Requirements: 2.1, 2.4_

- [x] 9.1 Write property test for printable character tracing
  - **Property 3: Printable Character Tracing**
  - **Validates: Requirements 2.1, 2.4**

- [x] 10. Integrate UTF-8 character tracing
  - Add TraceHelper.TraceUtf8Text calls in UTF-8 decoder when characters are produced
  - Include Unicode representation and source bytes in traces
  - Use appropriate direction based on data source
  - _Requirements: 2.2_

- [x] 10.1 Write property test for UTF-8 character tracing
  - **Property 4: UTF-8 Character Tracing**
  - **Validates: Requirements 2.2**

- [x] 11. Integrate wide character tracing
  - Add width indication to character traces for wide characters (CJK, emoji)
  - Ensure wide character traces include appropriate width information
  - Handle double-width character positioning correctly in traces
  - _Requirements: 2.3_

- [x] 11.1 Write property test for wide character tracing
  - **Property 5: Wide Character Tracing**
  - **Validates: Requirements 2.3**

- [x] 12. Integrate SGR sequence tracing
  - Add specific tracing for SGR sequences in SgrParser
  - Include complete attribute change information in traces
  - Use Output direction for SGR styling sequences
  - _Requirements: 5.3_

- [x] 12.1 Write property test for SGR sequence tracing
  - **Property 6: SGR Sequence Tracing**
  - **Validates: Requirements 5.3**

- [x] 13. Implement test database infrastructure
  - Create TestTraceDatabase helper class for UUID-based test database filenames
  - Implement test database creation in assembly directory
  - Add test database cleanup and isolation mechanisms
  - _Requirements: 3.1, 6.1, 6.2, 6.3, 6.5_

- [x] 13.1 Write property test for test database isolation
  - **Property 12: Test Database Isolation**
  - **Validates: Requirements 6.5**

- [ ] 14. Implement performance optimization tests
  - Create performance tests to verify <5ns overhead when tracing disabled
  - Verify no database operations or string formatting when disabled
  - Test unconditional tracing calls with enabled state optimization
  - _Requirements: 4.1, 9.1, 9.2, 9.3, 9.4_

- [ ] 14.1 Write property test for performance when disabled
  - **Property 8: Performance When Disabled**
  - **Validates: Requirements 4.1, 9.2, 9.3**

- [ ] 14.2 Write property test for unconditional tracing calls
  - **Property 9: Unconditional Tracing Calls**
  - **Validates: Requirements 9.1, 9.4**

- [ ] 15. Implement error handling and resilience
  - Add comprehensive error handling for database failures
  - Implement connection recovery mechanisms
  - Add graceful handling of SQLite busy/locked errors
  - Ensure terminal processing continues despite tracing failures
  - _Requirements: 4.3, 4.4, 11.1, 11.2, 11.3, 11.4, 11.5_

- [ ] 15.1 Write property test for error handling resilience
  - **Property 10: Error Handling Resilience**
  - **Validates: Requirements 4.3, 4.4, 11.1, 11.2, 11.4**

- [ ] 15.2 Write property test for database connection recovery
  - **Property 11: Database Connection Recovery**
  - **Validates: Requirements 11.3, 11.5**

- [ ] 16. Implement direction query and information exposure
  - Add database query methods with direction filtering
  - Ensure trace entries expose direction information correctly
  - Test direction-based filtering and data retrieval
  - _Requirements: 8.5, 10.3_

- [ ] 16.1 Write property test for direction query filtering
  - **Property 13: Direction Query Filtering**
  - **Validates: Requirements 8.5**

- [ ] 16.2 Write property test for direction information exposure
  - **Property 14: Direction Information Exposure**
  - **Validates: Requirements 10.3**

- [x] 17. Implement human-readable escape sequence formatting
  - Update TraceHelper methods to use consistent `\x1b` notation instead of "ESC"
  - Ensure CSI sequences format as `\x1b[parameters;command`
  - Ensure OSC sequences format as `\x1b]command;data\x07`
  - Ensure ESC sequences format as `\x1b` followed by sequence characters
  - Ensure DCS sequences format as `\x1bPparameterscommanddata\x1b\\`
  - Ensure control characters format as `\x{XX}` hexadecimal notation
  - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7_

- [x] 17.1 Write property test for human-readable formatting
  - **Property 16: Human-Readable Escape Sequence Formatting**
  - **Validates: Requirements 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7**

- [ ] 18. Final integration and validation
  - Integrate all tracing calls into the complete terminal processing pipeline
  - Verify end-to-end tracing functionality with real terminal sequences
  - Test with sample terminal applications and escape sequence patterns
  - _Requirements: All requirements integration_

- [ ] 18.1 Write integration tests for complete tracing pipeline
  - Test complete terminal sessions with mixed escape sequences and text
  - Verify tracing captures all expected data with correct directions
  - Test real-world terminal application scenarios

- [x] 19. Add type parameter support to TerminalTracer
  - Add type parameter to TraceEntry record and BufferTraceEntry method
  - Update FlushBufferInternal to write type column to database
  - Add optional type parameter to TraceEscape, TracePrintable, and Trace methods
  - _Requirements: 13.1-13.11_

- [x] 20. Update existing tracing calls to set type values
  - Update all TraceHelper methods to pass appropriate type values ("CSI", "OSC", "ESC", "DCS", "SGR")
  - Update ScreenBufferManager to pass "printable" type for character tracing
  - Update control character tracing to pass "control" type
  - _Requirements: 13.1-13.7_

- [x] 20.1 Write property test for type classification
  - **Property 17: Type Classification Accuracy**
  - **Validates: Requirements 13.1-13.9**

- [ ] 21. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- All tasks are required for comprehensive tracing implementation
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties
- Unit tests validate specific examples and edge cases
- All tracing calls should be made unconditionally, relying on TerminalTracer.Enabled for performance optimization
- Test databases use UUID-based filenames for isolation and are placed in assembly directory for automatic cleanup
- Type column should be populated for all trace entries with appropriate classification (CSI, OSC, ESC, DCS, SGR, printable, control, utf8, wide)