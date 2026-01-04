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