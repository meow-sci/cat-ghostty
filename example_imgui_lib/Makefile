#
# Cross Platform Makefile
# Compatible with MSYS2/MINGW, Ubuntu 14.04.1 and Mac OS X
#
# You will need GLFW (http://www.glfw.org):
# Linux:
#   apt-get install libglfw-dev
# Mac OS X:
#   brew install glfw
# MSYS2:
#   pacman -S --noconfirm --needed mingw-w64-x86_64-toolchain mingw-w64-x86_64-glfw
#

#CXX = g++
#CXX = clang++

UNAME_S := $(shell uname -s)

# Build a shared library for hosting from other languages.
# - macOS: libexample_imgui_lib.dylib
# - Linux: libexample_imgui_lib.so
ifeq ($(UNAME_S), Darwin)
	LIB = libexample_imgui_lib.dylib
else
	LIB = libexample_imgui_lib.so
endif

EXE =
IMGUI_DIR = ../imgui
SOURCES = main.cpp
SOURCES += $(IMGUI_DIR)/imgui.cpp $(IMGUI_DIR)/imgui_demo.cpp $(IMGUI_DIR)/imgui_draw.cpp $(IMGUI_DIR)/imgui_tables.cpp $(IMGUI_DIR)/imgui_widgets.cpp
SOURCES += $(IMGUI_DIR)/backends/imgui_impl_glfw.cpp $(IMGUI_DIR)/backends/imgui_impl_vulkan.cpp
OBJS = $(addsuffix .o, $(basename $(notdir $(SOURCES))))
LINUX_GL_LIBS = -lGL

CXXFLAGS = -std=c++11 -I$(IMGUI_DIR) -I$(IMGUI_DIR)/backends -fPIC
CXXFLAGS += -g -Wall -Wformat
LIBS =

##---------------------------------------------------------------------
## BUILD FLAGS PER PLATFORM
##---------------------------------------------------------------------

ifeq ($(UNAME_S), Linux) #LINUX
	ECHO_MESSAGE = "Linux"
	LIBS += $(LINUX_GL_LIBS) `pkg-config --static --libs glfw3 vulkan`

	CXXFLAGS += `pkg-config --cflags glfw3 vulkan`
	CFLAGS = $(CXXFLAGS)
endif

ifeq ($(UNAME_S), Darwin) #APPLE
	ECHO_MESSAGE = "Mac OS X"
	LIBS += -framework Cocoa -framework IOKit -framework CoreVideo
	LIBS += `pkg-config --libs glfw3 vulkan`
	LIBS += -L/usr/local/lib -L/opt/local/lib -L/opt/homebrew/lib
	#LIBS += -lglfw3

	LIBS += `pkg-config --cflags glfw3 vulkan`
	CXXFLAGS += -I/usr/local/include -I/opt/local/include -I/opt/homebrew/include
	CFLAGS = $(CXXFLAGS)
endif

ifeq ($(OS), Windows_NT)
	ECHO_MESSAGE = "MinGW"
	LIBS += -lgdi32 -limm32
	LIBS += `pkg-config --libs glfw3 vulkan`

	CXXFLAGS += `pkg-config --cflags glfw3 vulkan`
	CFLAGS = $(CXXFLAGS)
endif

##---------------------------------------------------------------------
## BUILD RULES
##---------------------------------------------------------------------

%.o:%.cpp
	$(CXX) $(CXXFLAGS) -c -o $@ $<

%.o:$(IMGUI_DIR)/%.cpp
	$(CXX) $(CXXFLAGS) -c -o $@ $<

%.o:$(IMGUI_DIR)/backends/%.cpp
	$(CXX) $(CXXFLAGS) -c -o $@ $<

all: $(LIB)
	@echo Build complete for $(ECHO_MESSAGE)

ifeq ($(UNAME_S), Darwin)
$(LIB): $(OBJS)
	$(CXX) -dynamiclib -Wl,-install_name,@rpath/$(LIB) -o $@ $^ $(CXXFLAGS) $(LIBS)
else
$(LIB): $(OBJS)
	$(CXX) -shared -o $@ $^ $(CXXFLAGS) $(LIBS)
endif

clean:
	rm -f $(LIB) $(OBJS)
