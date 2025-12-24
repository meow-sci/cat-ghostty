@echo off
echo Testing caTTY Terminal Escape Sequences
echo =======================================
echo.

echo Test 1: Basic text output
echo Hello World!
echo.

echo Test 2: Cursor movement (if supported)
echo Moving cursor up and writing text...
echo [2A[1B[Cursor moved]
echo.

echo Test 3: Clear operations (if supported)
echo This line will test clearing...
echo [K
echo Line cleared to end
echo.

echo Test 4: UTF-8 characters
echo Testing UTF-8: Hello World piñata
echo.

echo Test 5: Tab characters
echo Tab	Tab	Tab	End
echo.

echo Test 6: Control characters
echo Bell test: 
echo Backspace test: ABC XY
echo.

echo Escape sequence testing complete!
echo If you can see this and text displays correctly,
echo then basic terminal functionality is working.
pause