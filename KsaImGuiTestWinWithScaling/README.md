This project is janky and doesn't have a proper fix for BRUTAL ImGui on windows with DPI scaling.

It was a WIP attempt to debug and find a solution but abandoned.

`KsaImGuiTestWinNoScaling` sets up viewports with no scaling to avoid any mouse positioning issues with scaling.
For me, this is good enough for the standalone test application development for now.

