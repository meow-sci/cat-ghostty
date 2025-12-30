#!/bin/bash

# Test script for enhanced SGR underline styles
# Tests single, double, curly, dotted, and dashed underlines

echo "=== Enhanced SGR Underline Styles Test ==="
echo ""

echo "Testing individual underline styles:"
echo ""

# Single underline (standard SGR 4)
echo -e "Single underline: \033[4mThis text has single underline\033[24m"

# Double underline (SGR 21)
echo -e "Double underline: \033[21mThis text has double underline\033[24m"

# Enhanced underline styles using CSI > 4 ; n m format
echo -e "Curly underline:  \033[>4;3mThis text has curly underline\033[>4;0m"
echo -e "Dotted underline: \033[>4;4mThis text has dotted underline\033[>4;0m"
echo -e "Dashed underline: \033[>4;5mThis text has dashed underline\033[>4;0m"

echo ""
echo "Testing underline styles with colon separator (SGR 4:n):"
echo ""

# Alternative format using colon separators
echo -e "Single (4:1):     \033[4:1mThis text has single underline\033[4:0m"
echo -e "Double (4:2):     \033[4:2mThis text has double underline\033[4:0m"
echo -e "Curly (4:3):      \033[4:3mThis text has curly underline\033[4:0m"
echo -e "Dotted (4:4):     \033[4:4mThis text has dotted underline\033[4:0m"
echo -e "Dashed (4:5):     \033[4:5mThis text has dashed underline\033[4:0m"

echo ""
echo "Testing all underline styles in a single line:"
echo ""

# All styles in one echo command
echo -e "\033[4:1mSingle\033[4:0m \033[4:2mDouble\033[4:0m \033[4:3mCurly\033[4:0m \033[4:4mDotted\033[4:0m \033[4:5mDashed\033[4:0m underlines"

echo ""
echo "Testing underline styles with colors:"
echo ""

# Colored underlines
echo -e "\033[31;4:1mRed single\033[0m \033[32;4:2mGreen double\033[0m \033[34;4:3mBlue curly\033[0m \033[35;4:4mMagenta dotted\033[0m \033[36;4:5mCyan dashed\033[0m"

echo ""
echo "Testing underline color (SGR 58) - simplified:"
echo ""

# Test SGR 58 with simpler sequences (separate underline and color commands)
echo -e "\033[4m\033[58;2;255;0;0mText with red underline\033[0m"
echo -e "\033[21m\033[58;2;0;255;0mText with green double underline\033[0m"
echo -e "\033[4:3m\033[58;2;0;0;255mText with blue curly underline\033[0m"
echo -e "\033[4:4m\033[58;2;255;255;0mText with yellow dotted underline\033[0m"
echo -e "\033[4:5m\033[58;2;255;0;255mText with magenta dashed underline\033[0m"

echo ""
echo "Testing underline color (SGR 58) - combined sequences:"
echo ""

# Underline with specific color (SGR 58;2;r;g;b for RGB underline color)
echo -e "\033[4;58;2;255;0;0mText with red underline\033[0m"
echo -e "\033[21;58;2;0;255;0mText with green double underline\033[0m"
echo -e "\033[4:3;58;2;0;0;255mText with blue curly underline\033[0m"
echo -e "\033[4:4;58;2;255;255;0mText with yellow dotted underline\033[0m"
echo -e "\033[4:5;58;2;255;0;255mText with magenta dashed underline\033[0m"

echo ""
echo "=== Test Complete ==="
echo ""
echo "If you see different underline styles above, the implementation is working!"
echo "Single should be a straight line, double should be two lines,"
echo "curly should be wavy, dotted should have gaps, and dashed should have longer segments."