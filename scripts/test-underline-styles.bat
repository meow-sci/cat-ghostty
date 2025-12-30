@echo off
REM Test script for enhanced SGR underline styles
REM Tests single, double, curly, dotted, and dashed underlines

echo === Enhanced SGR Underline Styles Test ===
echo.

echo Testing individual underline styles:
echo.

REM Single underline (standard SGR 4)
echo Single underline: [4mThis text has single underline[24m

REM Double underline (SGR 21)
echo Double underline: [21mThis text has double underline[24m

REM Enhanced underline styles using CSI > 4 ; n m format
echo Curly underline:  [>4;3mThis text has curly underline[>4;0m
echo Dotted underline: [>4;4mThis text has dotted underline[>4;0m
echo Dashed underline: [>4;5mThis text has dashed underline[>4;0m

echo.
echo Testing underline styles with colon separator (SGR 4:n):
echo.

REM Alternative format using colon separators
echo Single (4:1):     [4:1mThis text has single underline[4:0m
echo Double (4:2):     [4:2mThis text has double underline[4:0m
echo Curly (4:3):      [4:3mThis text has curly underline[4:0m
echo Dotted (4:4):     [4:4mThis text has dotted underline[4:0m
echo Dashed (4:5):     [4:5mThis text has dashed underline[4:0m

echo.
echo Testing all underline styles in a single line:
echo.

REM All styles in one echo command
echo [4:1mSingle[4:0m [4:2mDouble[4:0m [4:3mCurly[4:0m [4:4mDotted[4:0m [4:5mDashed[4:0m underlines

echo.
echo Testing underline styles with colors:
echo.

REM Colored underlines
echo [31;4:1mRed single[0m [32;4:2mGreen double[0m [34;4:3mBlue curly[0m [35;4:4mMagenta dotted[0m [36;4:5mCyan dashed[0m

echo.
echo Testing underline color (SGR 58) - simplified:
echo.

REM Test SGR 58 with simpler sequences (separate underline and color commands)
echo [4m[58;2;255;0;0mText with red underline[0m
echo [21m[58;2;0;255;0mText with green double underline[0m
echo [4:3m[58;2;0;0;255mText with blue curly underline[0m
echo [4:4m[58;2;255;255;0mText with yellow dotted underline[0m
echo [4:5m[58;2;255;0;255mText with magenta dashed underline[0m

echo.
echo Testing underline color (SGR 58) - combined sequences:
echo.

REM Underline with specific color (SGR 58;2;r;g;b for RGB underline color)
echo [4;58;2;255;0;0mText with red underline[0m
echo [21;58;2;0;255;0mText with green double underline[0m
echo [4:3;58;2;0;0;255mText with blue curly underline[0m
echo [4:4;58;2;255;255;0mText with yellow dotted underline[0m
echo [4:5;58;2;255;0;255mText with magenta dashed underline[0m

echo.
echo === Test Complete ===
echo.
echo If you see different underline styles above, the implementation is working!
echo Single should be a straight line, double should be two lines,
echo curly should be wavy, dotted should have gaps, and dashed should have longer segments.
echo.
echo Note: Windows Command Prompt may not display all ANSI sequences correctly.
echo For best results, test in the caTTY terminal emulator or Windows Terminal.
pause