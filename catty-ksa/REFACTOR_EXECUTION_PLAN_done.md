---

# Phase 1: ProcessManager Refactoring

**Target:** `caTTY.Core/Terminal/ProcessManager.cs`
**Strategy:** Extract native interop, shell resolution, and lifecycle management into focused classes

## Task 1.1: Extract Native Interop

**Goal:** Move all P/Invoke declarations and native types into `ConPtyNative.cs`

**Steps:**
1. Create folder: `caTTY.Core/Terminal/Process/`
2. Create file: `caTTY.Core/Terminal/Process/ConPtyNative.cs`
3. Move the following from `ProcessManager.cs` to `ConPtyNative.cs`:
   - Constants: `EXTENDED_STARTUPINFO_PRESENT`, `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE`
   - Structs: `COORD`, `STARTUPINFOEX`, `STARTUPINFO`, `PROCESS_INFORMATION`
   - All DllImport methods: `CreatePseudoConsole`, `ResizePseudoConsole`, `ClosePseudoConsole`, `CreatePipe`, `CloseHandle`, `InitializeProcThreadAttributeList`, `UpdateProcThreadAttribute`, `DeleteProcThreadAttributeList`, `CreateProcessW`, `ReadFile`, `WriteFile`
4. Make all members `internal static`
5. Update `ProcessManager.cs` to reference `ConPtyNative.*` (e.g., `ConPtyNative.CreatePseudoConsole(...)`)

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 1.1: Extract native interop to ConPtyNative

- Created caTTY.Core/Terminal/Process/ folder
- Created ConPtyNative.cs with all P/Invoke declarations
- Moved constants: EXTENDED_STARTUPINFO_PRESENT, PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE
- Moved structs: COORD, STARTUPINFOEX, STARTUPINFO, PROCESS_INFORMATION
- Moved 11 DllImport methods
- Updated ProcessManager.cs to reference ConPtyNative.*
- Reduced ProcessManager.cs by ~100-150 LOC
- All tests pass"
```

**Success Criteria:**
- All tests pass
- `ProcessManager.cs` reduced by ~100-150 LOC
- No business logic changed

## Task 1.2: Extract Shell Resolution

**Goal:** Move shell command resolution logic into `ShellCommandResolver.cs`

**Steps:**
1. Create file: `caTTY.Core/Terminal/Process/ShellCommandResolver.cs`
2. Move these methods from `ProcessManager.cs` (keep logic EXACTLY as-is):
   - `ResolveShellCommand(ProcessLaunchOptions options)`
   - `ResolveAutoShell()`
   - `ResolveWsl(string? distribution)`
   - `ResolvePowerShell()`
   - `ResolvePowerShellCore()`
   - `ResolveCmd()`
   - `ResolveCustomShell(string command, string args)`
   - `FindExecutableInPath(string executable)`
3. Make all methods `internal static`
4. Update `ProcessManager.StartAsync` to call `ShellCommandResolver.ResolveShellCommand(options)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 1.2: Extract shell resolution to ShellCommandResolver

- Created ShellCommandResolver.cs
- Moved 8 shell resolution methods from ProcessManager
- Made all methods internal static
- Updated ProcessManager.StartAsync to delegate to resolver
- Shell resolution logic isolated and testable
- All tests pass"
```

**Success Criteria:**
- All tests pass
- Shell resolution logic isolated
- `ProcessManager.cs` further reduced

## Task 1.3: Extract Attribute List Builder

**Goal:** Isolate unsafe attribute list management

**Steps:**
1. Create file: `caTTY.Core/Terminal/Process/AttributeListBuilder.cs`
2. Create static methods:
   ```csharp
   internal static IntPtr CreateAttributeListWithPseudoConsole(IntPtr pseudoConsole)
   internal static void FreeAttributeList(IntPtr attributeList)
   ```
3. Move attribute list creation logic from `ProcessManager.StartAsync`:
   - Size probe with `InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size)`
   - `Marshal.AllocHGlobal(size)`
   - `InitializeProcThreadAttributeList` actual call
   - `UpdateProcThreadAttribute` with `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE`
   - Cleanup: `DeleteProcThreadAttributeList` + `Marshal.FreeHGlobal`
4. Update `StartAsync` to:
   - Call `AttributeListBuilder.CreateAttributeListWithPseudoConsole(_pseudoConsole)`
   - Assign to `startupInfo.lpAttributeList`
   - Call `AttributeListBuilder.FreeAttributeList(...)` in cleanup paths

**CRITICAL:** Preserve exact error handling and cleanup order from original code

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 1.3: Extract attribute list builder

- Created AttributeListBuilder.cs
- Extracted CreateAttributeListWithPseudoConsole method
- Extracted FreeAttributeList method
- Moved unsafe attribute list management logic
- Preserved exact error handling and cleanup order
- All tests pass"
```

**Success Criteria:**
- All tests pass
- Unsafe code isolated
- Process creation still works correctly

## Task 1.4: Extract Startup Info Builder

**Goal:** Isolate STARTUPINFOEX creation

**Steps:**
1. Create file: `caTTY.Core/Terminal/Process/StartupInfoBuilder.cs`
2. Create method:
   ```csharp
   internal static ConPtyNative.STARTUPINFOEX Create()
   ```
3. Move initialization logic:
   ```csharp
   var startupInfo = new STARTUPINFOEX();
   startupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
   return startupInfo;
   ```
4. Update `ProcessManager.StartAsync` to call `StartupInfoBuilder.Create()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 1.4: Extract startup info builder

- Created StartupInfoBuilder.cs
- Extracted STARTUPINFOEX creation logic
- Updated ProcessManager.StartAsync to use builder
- All tests pass"
```

## Task 1.5: Extract Output Pump

**Goal:** Move output reading loop into dedicated class

**Steps:**
1. Create file: `caTTY.Core/Terminal/Process/ConPtyOutputPump.cs`
2. Create static method with signature:
   ```csharp
   internal static async Task ReadOutputAsync(
       IntPtr outputHandle,
       Func<int?> getProcessId,
       Action<byte[], int> onDataReceived,
       Action<string> onProcessError,
       CancellationToken cancellationToken)
   ```
3. Move the **exact** body of `ProcessManager.ReadOutputAsync` into this method
   - Keep loop structure identical
   - Keep `ReadFile` call identical
   - Keep error handling identical
   - Keep `Task.Delay(1, cancellationToken)` identical
4. Update `ProcessManager.ReadOutputAsync` to delegate to `ConPtyOutputPump.ReadOutputAsync`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 1.5: Extract output pump

- Created ConPtyOutputPump.cs
- Extracted ReadOutputAsync method with exact logic
- Preserved loop structure, ReadFile call, error handling
- Updated ProcessManager to delegate to pump
- All tests pass"
```

## Task 1.6: Extract Input Writer

**Goal:** Move write operations into dedicated class

**Steps:**
1. Create file: `caTTY.Core/Terminal/Process/ConPtyInputWriter.cs`
2. Create class with instance methods:
   ```csharp
   internal class ConPtyInputWriter
   {
       public void Write(ReadOnlySpan<byte> data, IntPtr handle, object lockObject)
       public void Write(string text, IntPtr handle, object lockObject)
   }
   ```
3. Move logic from `ProcessManager.Write` methods (preserve locking and disposal checks)
4. Create `ConPtyInputWriter` instance in `ProcessManager`
5. Delegate `IProcessManager.Write` methods to the writer instance

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 1.6: Extract input writer

- Created ConPtyInputWriter.cs
- Extracted Write(ReadOnlySpan<byte>) method
- Extracted Write(string) method
- Preserved locking and disposal checks
- Updated ProcessManager to delegate to writer
- All tests pass"
```

## Task 1.7: Extract Process Cleanup

**Goal:** Isolate cleanup logic

**Steps:**
1. Create file: `caTTY.Core/Terminal/Process/ProcessCleanup.cs`
2. Create static methods:
   ```csharp
   internal static void CleanupProcess(Process? process)
   internal static void CleanupPseudoConsole(IntPtr pseudoConsole)
   internal static void CleanupHandles(IntPtr inputRead, IntPtr inputWrite, IntPtr outputRead, IntPtr outputWrite)
   ```
3. Move cleanup logic from `ProcessManager` methods (preserve exact cleanup order)
4. Update `ProcessManager.Dispose` and `StopAsync` to call cleanup methods

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 1.7: Extract process cleanup

- Created ProcessCleanup.cs
- Extracted CleanupProcess method
- Extracted CleanupPseudoConsole method
- Extracted CleanupHandles method
- Preserved exact cleanup order
- Updated ProcessManager disposal logic
- All tests pass"
```

## Task 1.8: Extract Process Events

**Goal:** Move event raising logic

**Steps:**
1. Create file: `caTTY.Core/Terminal/Process/ProcessEvents.cs`
2. Move event-raising helper methods:
   - `OnProcessExited`
   - `OnDataReceived`
   - `OnProcessError`
3. Keep actual event declarations in `ProcessManager`
4. Update event usage to call helpers

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 1.8: Extract process events

- Created ProcessEvents.cs
- Extracted OnProcessExited helper
- Extracted OnDataReceived helper
- Extracted OnProcessError helper
- Event declarations remain in ProcessManager
- All tests pass"
```

**Phase 1 Complete:** `ProcessManager.cs` should now be a facade ~200-300 LOC orchestrating the Process/ subfolder


---

# Phase 2: SessionManager Refactoring

**Target:** `caTTY.Core/Terminal/SessionManager.cs`
**Strategy:** Extract session lifecycle, switching, and dimension tracking

## Task 2.1: Extract Dimension Tracker

**Goal:** Isolate terminal dimension management

**Steps:**
1. Create folder: `caTTY.Core/Terminal/Sessions/`
2. Create file: `caTTY.Core/Terminal/Sessions/SessionDimensionTracker.cs`
3. Move these methods (preserve exact logic):
   - `UpdateLastKnownTerminalDimensions(int cols, int rows)`
   - `GetDefaultLaunchOptionsSnapshot()`
   - `UpdateDefaultLaunchOptions(ProcessLaunchOptions launchOptions)`
   - `CloneLaunchOptions(ProcessLaunchOptions options)`
4. Create instance in `SessionManager`, delegate calls

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.1: Extract session dimension tracker

- Created caTTY.Core/Terminal/Sessions/ folder
- Created SessionDimensionTracker.cs
- Extracted UpdateLastKnownTerminalDimensions method
- Extracted GetDefaultLaunchOptionsSnapshot method
- Extracted UpdateDefaultLaunchOptions method
- Extracted CloneLaunchOptions method
- SessionManager delegates to tracker
- All tests pass"
```

## Task 2.2: Extract Terminal Session Factory

**Goal:** Isolate TerminalSession creation and wiring

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/TerminalSessionFactory.cs`
2. Extract session creation logic from `CreateSessionAsync`:
   - `TerminalEmulator` instantiation
   - `ProcessManager` creation
   - Event subscription
   - `TerminalSession` construction
3. Keep exact event subscription order
4. Update `CreateSessionAsync` to call factory

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.2: Extract terminal session factory

- Created TerminalSessionFactory.cs
- Extracted TerminalEmulator instantiation logic
- Extracted ProcessManager creation logic
- Extracted event subscription logic
- Extracted TerminalSession construction
- Preserved exact event subscription order
- All tests pass"
```

## Task 2.3: Extract Session Creator

**Goal:** Move session creation logic

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/SessionCreator.cs`
2. Move `CreateSessionAsync` body into `SessionCreator.CreateSessionAsync`
3. Keep lock acquisition in `SessionManager`
4. Pass locked state as parameters
5. Preserve exact validation and error handling

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.3: Extract session creator

- Created SessionCreator.cs
- Extracted CreateSessionAsync logic
- Lock acquisition remains in SessionManager
- Preserved exact validation and error handling
- All tests pass"
```

## Task 2.4: Extract Session Switcher

**Goal:** Move session switching logic

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/SessionSwitcher.cs`
2. Move methods:
   - `SwitchToSession(Guid sessionId)`
   - `SwitchToNextSession()`
   - `SwitchToPreviousSession()`
3. Preserve exact active session tracking and event raising

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.4: Extract session switcher

- Created SessionSwitcher.cs
- Extracted SwitchToSession method
- Extracted SwitchToNextSession method
- Extracted SwitchToPreviousSession method
- Preserved active session tracking and events
- All tests pass"
```

## Task 2.5: Extract Session Closer

**Goal:** Move session closing logic

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/SessionCloser.cs`
2. Move `CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken)`
3. Preserve exact cleanup order and active session switching logic

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.5: Extract session closer

- Created SessionCloser.cs
- Extracted CloseSessionAsync method
- Preserved cleanup order and session switching
- All tests pass"
```

## Task 2.6: Extract Session Restarter

**Goal:** Move session restart logic

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/SessionRestarter.cs`
2. Move `RestartSessionAsync(Guid sessionId, ProcessLaunchOptions?, CancellationToken)`
3. Preserve exact restart sequence

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.6: Extract session restarter

- Created SessionRestarter.cs
- Extracted RestartSessionAsync method
- Preserved exact restart sequence
- All tests pass"
```

## Task 2.7: Extract Session Event Bridge

**Goal:** Move event handlers

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/SessionEventBridge.cs`
2. Move event handler methods:
   - `OnSessionStateChanged`
   - `OnSessionTitleChanged`
   - `OnSessionProcessExited`
3. Keep subscription points unchanged

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.7: Extract session event bridge

- Created SessionEventBridge.cs
- Extracted OnSessionStateChanged handler
- Extracted OnSessionTitleChanged handler
- Extracted OnSessionProcessExited handler
- Subscription points unchanged
- All tests pass"
```

## Task 2.8: Extract Session Logging

**Goal:** Move logging helpers

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/SessionLogging.cs`
2. Move:
   - `LogSessionLifecycleEvent`
   - `IsDebugLoggingEnabled`
3. Keep format strings identical

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.8: Extract session logging

- Created SessionLogging.cs
- Extracted LogSessionLifecycleEvent method
- Extracted IsDebugLoggingEnabled method
- Format strings preserved identically
- All tests pass"
```

**Phase 2 Complete:** `SessionManager.cs` should be facade ~200-300 LOC

---

# Phase 3: TerminalParserHandlers Refactoring

**Target:** `caTTY.Core/Terminal/TerminalParserHandlers.cs`
**Strategy:** Extract message type handlers

## Task 3.1: Extract SGR Handler

**Goal:** Isolate SGR handling

**Steps:**
1. Create folder: `caTTY.Core/Terminal/ParserHandlers/`
2. Create file: `caTTY.Core/Terminal/ParserHandlers/SgrHandler.cs`
3. Move methods:
   - `HandleSgrSequence(SgrSequence sequence)`
   - `TraceSgrSequence(...)`
   - `FormatColor(Color? color)`
   - `ExtractSgrParameters(string rawSequence)`
4. Keep `TerminalParserHandlers.HandleSgr` as delegator

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.1: Extract SGR handler

- Created caTTY.Core/Terminal/ParserHandlers/ folder
- Created SgrHandler.cs
- Extracted HandleSgrSequence method
- Extracted TraceSgrSequence method
- Extracted FormatColor method
- Extracted ExtractSgrParameters method
- TerminalParserHandlers.HandleSgr delegates to handler
- All tests pass"
```

## Task 3.2: Extract DCS Handler

**Goal:** Isolate DCS handling

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/DcsHandler.cs`
2. Move methods:
   - `HandleDecrqss(DcsMessage message)`
   - `ExtractDecrqssPayload(string raw)`
   - `GenerateSgrStateResponse()`
3. Keep `HandleDcs` as delegator

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.2: Extract DCS handler

- Created DcsHandler.cs
- Extracted HandleDecrqss method
- Extracted ExtractDecrqssPayload method
- Extracted GenerateSgrStateResponse method
- HandleDcs delegates to handler
- All tests pass"
```

## Task 3.3: Extract OSC Handler

**Goal:** Isolate OSC handling

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/OscHandler.cs`
2. Move methods:
   - `HandleOsc(OscMessage message)`
   - `HandleXtermOsc(XtermOscMessage message)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.3: Extract OSC handler

- Created OscHandler.cs
- Extracted HandleOsc method
- Extracted HandleXtermOsc method
- All tests pass"
```

## Task 3.4: Extract CSI Dispatcher

**Goal:** Create CSI routing infrastructure

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiDispatcher.cs`
2. Move `HandleCsi(CsiMessage message)` switch statement
3. Keep case bodies inline initially (will extract in next tasks)

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.4: Extract CSI dispatcher

- Created CsiDispatcher.cs
- Extracted HandleCsi switch statement
- Case bodies remain inline for now
- All tests pass"
```

## Task 3.5: Extract CSI Cursor Handler

**Goal:** Extract cursor-related CSI cases

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiCursorHandler.cs`
2. Extract cursor movement cases from CSI switch
3. Update dispatcher to delegate cursor cases

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.5: Extract CSI cursor handler

- Created CsiCursorHandler.cs
- Extracted cursor movement CSI cases
- Updated CsiDispatcher to delegate cursor cases
- All tests pass"
```

## Task 3.6: Extract CSI Erase Handler

**Goal:** Extract erase-related CSI cases

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiEraseHandler.cs`
2. Extract erase cases (ED, EL, etc.)

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.6: Extract CSI erase handler

- Created CsiEraseHandler.cs
- Extracted erase-related CSI cases (ED, EL, etc.)
- Updated CsiDispatcher to delegate erase cases
- All tests pass"
```

## Task 3.7: Extract CSI Scroll Handler

**Goal:** Extract scroll-related CSI cases

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiScrollHandler.cs`
2. Extract scroll region, scroll up/down cases

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.7: Extract CSI scroll handler

- Created CsiScrollHandler.cs
- Extracted scroll region CSI cases
- Extracted scroll up/down CSI cases
- All tests pass"
```

## Task 3.8: Extract CSI Insert/Delete Handler

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiInsertDeleteHandler.cs`
2. Extract insert/delete lines/chars cases

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.8: Extract CSI insert/delete handler

- Created CsiInsertDeleteHandler.cs
- Extracted insert/delete lines CSI cases
- Extracted insert/delete chars CSI cases
- All tests pass"
```

## Task 3.9: Extract CSI DEC Mode Handler

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiDecModeHandler.cs`
2. Extract DEC mode setting cases

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.9: Extract CSI DEC mode handler

- Created CsiDecModeHandler.cs
- Extracted DEC mode setting CSI cases
- All tests pass"
```

## Task 3.10: Extract CSI Device Query Handler

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiDeviceQueryHandler.cs`
2. Extract device status, cursor position query cases

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.10: Extract CSI device query handler

- Created CsiDeviceQueryHandler.cs
- Extracted device status CSI cases
- Extracted cursor position query cases
- All tests pass"
```

## Task 3.11: Extract CSI Window Manipulation Handler

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiWindowManipulationHandler.cs`
2. Extract window ops cases

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.11: Extract CSI window manipulation handler

- Created CsiWindowManipulationHandler.cs
- Extracted window manipulation CSI cases
- All tests pass"
```

## Task 3.12: Extract C0 and ESC Handlers

**Goal:** Extract simple control handlers

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/C0Handler.cs`
2. Move: `HandleBell`, `HandleBackspace`, `HandleTab`, `HandleLineFeed`, `HandleFormFeed`, `HandleCarriageReturn`, `HandleShiftIn`, `HandleShiftOut`, `HandleNormalByte`
3. Create file: `caTTY.Core/Terminal/ParserHandlers/EscHandler.cs`
4. Move: `HandleEsc(EscMessage message)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.12: Extract C0 and ESC handlers

- Created C0Handler.cs
- Extracted HandleBell, HandleBackspace, HandleTab
- Extracted HandleLineFeed, HandleFormFeed, HandleCarriageReturn
- Extracted HandleShiftIn, HandleShiftOut, HandleNormalByte
- Created EscHandler.cs
- Extracted HandleEsc method
- All tests pass"
```

**Phase 3 Complete:** `TerminalParserHandlers.cs` should be facade ~100-150 LOC
