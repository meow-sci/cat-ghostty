# C# Console Host for `example_imgui_lib` (macOS)

This folder builds a shared library `libexample_imgui_lib.dylib` exporting:

- `int imgui_testapp_run(void);`

A .NET console app can P/Invoke that function to launch the ImGui GLFW+Vulkan demo window.

## 1) Build the dylib

From the repo root:

```bash
cd example_imgui_lib
make clean && make
```

You should get:

- `example_imgui_lib/libexample_imgui_lib.dylib`

## 2) Create a new .NET console project

Pick a directory anywhere (example below creates it next to this folder):

```bash
cd ..
dotnet new console -n ImGuiHost
cd ImGuiHost
```

## 3) Add the P/Invoke file

Copy the sample P/Invoke source into your new project:

```bash
cp ../example_imgui_lib/ImGuiTestAppPInvokeSample.cs ./
```

## 4) Copy the dylib to the .NET output folder

Edit `ImGuiHost.csproj` and add this ItemGroup (so the dylib is next to your built executable):

```xml
<ItemGroup>
  <None Include="../example_imgui_lib/libexample_imgui_lib.dylib" Link="libexample_imgui_lib.dylib">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## 5) Run

```bash
dotnet run
```

You should see a message on the console, and an ImGui window should open. Close the window to return to the console app.

## Notes / Troubleshooting

- **Main thread requirement (macOS + GLFW)**: call `imgui_testapp_run()` from your process main thread. A console app’s `Main()` is the right place.
- **Dynamic loader issues**: the sample uses `DllImport("example_imgui_lib")`, which on macOS resolves to `libexample_imgui_lib.dylib`. Keeping that dylib next to the managed executable is the simplest approach.
- **Dependencies**: the dylib links against GLFW/Vulkan as provided by your system (e.g. via Homebrew). If you built the dylib successfully with `make`, you already have the build-time dependencies.
