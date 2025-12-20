#pragma once

// Public C ABI entrypoint for hosting the Dear ImGui GLFW+Vulkan sample.
//
// Intended for consumption from other languages (e.g. C# via P/Invoke).
// The function blocks until the window is closed.

#if defined(_WIN32)
  #define IMGUI_TESTAPP_API extern "C" __declspec(dllexport)
#elif defined(__GNUC__) || defined(__clang__)
  #define IMGUI_TESTAPP_API extern "C" __attribute__((visibility("default")))
#else
  #define IMGUI_TESTAPP_API extern "C"
#endif

IMGUI_TESTAPP_API int imgui_testapp_run(void);
