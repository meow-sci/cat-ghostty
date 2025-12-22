// Minimal ImGui OpenGL Backend for OpenTK
// Based on ImGui OpenGL3 backend

using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;

namespace ImGuiDpiExample;

class ImGuiController : IDisposable
{
    private int _vbo, _vao, _ebo;
    private int _shader;
    private int _fontTexture;
    private int _attribLocTex, _attribLocProjMtx;
    private int _attribLocVtxPos, _attribLocVtxUV, _attribLocVtxColor;
    private int _windowWidth, _windowHeight;
    private bool _frameBegun;
    private IntPtr _context;
    private float _mouseOffsetFactor = 1.0f;
    
    public ImGuiController(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
        
        _context = ImGui.CreateContext();
        ImGui.SetCurrentContext(_context);
        
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        
        CreateDeviceResources();
        SetKeyMappings();
        
        io.Fonts.AddFontDefault();
        RecreateFontDeviceTexture();
    }
    
    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }
    
    // offsetFactor > 1.0 simulates broken DPI (mouse coords will be wrong)
    public void Update(GameWindow window, float deltaTime, float offsetFactor = 1.0f)
    {
        if (_frameBegun) ImGui.Render();
        
        _mouseOffsetFactor = offsetFactor;
        
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(_windowWidth, _windowHeight);
        io.DisplayFramebufferScale = new Vector2(
            (float)window.FramebufferSize.X / _windowWidth,
            (float)window.FramebufferSize.Y / _windowHeight);
        io.DeltaTime = deltaTime;
        
        UpdateInput(window);
        
        _frameBegun = true;
        ImGui.NewFrame();
    }
    
    private void UpdateInput(GameWindow window)
    {
        var io = ImGui.GetIO();
        var mouse = window.MouseState;
        var keyboard = window.KeyboardState;
        
        // Mouse position - THIS IS WHERE DPI ISSUES SHOW UP
        // When _mouseOffsetFactor > 1, we simulate what happens when
        // the app isn't DPI-aware: coords are scaled incorrectly
        float mx = mouse.X;
        float my = mouse.Y;
        
        if (_mouseOffsetFactor != 1.0f)
        {
            // Simulate broken DPI: offset the mouse position
            // This mimics what happens when Windows virtualizes coords
            mx = mx / _mouseOffsetFactor;
            my = my / _mouseOffsetFactor;
        }
        
        io.MousePos = new Vector2(mx, my);
        
        io.MouseDown[0] = mouse.IsButtonDown(MouseButton.Left);
        io.MouseDown[1] = mouse.IsButtonDown(MouseButton.Right);
        io.MouseDown[2] = mouse.IsButtonDown(MouseButton.Middle);
        io.MouseWheel = mouse.ScrollDelta.Y;
        io.MouseWheelH = mouse.ScrollDelta.X;
        
        foreach (Keys key in Enum.GetValues<Keys>())
        {
            if (key == Keys.Unknown) continue;
            io.KeysDown[(int)key] = keyboard.IsKeyDown(key);
        }
        
        io.KeyCtrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        io.KeyShift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        io.KeyAlt = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);
        io.KeySuper = keyboard.IsKeyDown(Keys.LeftSuper) || keyboard.IsKeyDown(Keys.RightSuper);
    }

    public unsafe void Render()
    {
        if (!_frameBegun) return;
        _frameBegun = false;
        
        ImGui.Render();
        var drawData = ImGui.GetDrawData();
        if (drawData.CmdListsCount == 0) return;
        
        var fbWidth = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
        var fbHeight = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
        if (fbWidth <= 0 || fbHeight <= 0) return;
        
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, 
            BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.StencilTest);
        GL.Enable(EnableCap.ScissorTest);
        
        GL.Viewport(0, 0, fbWidth, fbHeight);
        
        float L = drawData.DisplayPos.X;
        float R = drawData.DisplayPos.X + drawData.DisplaySize.X;
        float T = drawData.DisplayPos.Y;
        float B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;
        
        Matrix4x4 ortho = new(
            2.0f / (R - L), 0, 0, 0,
            0, 2.0f / (T - B), 0, 0,
            0, 0, -1, 0,
            (R + L) / (L - R), (T + B) / (B - T), 0, 1);
        
        GL.UseProgram(_shader);
        GL.Uniform1(_attribLocTex, 0);
        GL.UniformMatrix4(_attribLocProjMtx, 1, false, (float*)&ortho);
        GL.BindVertexArray(_vao);
        GL.BindSampler(0, 0);
        
        var clipOff = drawData.DisplayPos;
        var clipScale = drawData.FramebufferScale;
        
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, cmdList.VtxBuffer.Size * sizeof(ImDrawVert),
                cmdList.VtxBuffer.Data, BufferUsageHint.StreamDraw);
            
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, cmdList.IdxBuffer.Size * sizeof(ushort),
                cmdList.IdxBuffer.Data, BufferUsageHint.StreamDraw);
            
            for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                var cmd = cmdList.CmdBuffer[i];
                var clipMin = new Vector2((cmd.ClipRect.X - clipOff.X) * clipScale.X, 
                    (cmd.ClipRect.Y - clipOff.Y) * clipScale.Y);
                var clipMax = new Vector2((cmd.ClipRect.Z - clipOff.X) * clipScale.X, 
                    (cmd.ClipRect.W - clipOff.Y) * clipScale.Y);
                
                if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y) continue;
                
                GL.Scissor((int)clipMin.X, fbHeight - (int)clipMax.Y, 
                    (int)(clipMax.X - clipMin.X), (int)(clipMax.Y - clipMin.Y));
                GL.BindTexture(TextureTarget.Texture2D, (int)cmd.TextureId);
                GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)cmd.ElemCount,
                    DrawElementsType.UnsignedShort, (IntPtr)(cmd.IdxOffset * sizeof(ushort)), (int)cmd.VtxOffset);
            }
        }
        
        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.ScissorTest);
    }

    private void CreateDeviceResources()
    {
        string vertexSource = @"#version 330 core
layout (location = 0) in vec2 Position;
layout (location = 1) in vec2 UV;
layout (location = 2) in vec4 Color;
uniform mat4 ProjMtx;
out vec2 Frag_UV;
out vec4 Frag_Color;
void main() {
    Frag_UV = UV;
    Frag_Color = Color;
    gl_Position = ProjMtx * vec4(Position.xy, 0, 1);
}";
        
        string fragmentSource = @"#version 330 core
in vec2 Frag_UV;
in vec4 Frag_Color;
uniform sampler2D Texture;
layout (location = 0) out vec4 Out_Color;
void main() {
    Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
}";
        
        int vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, vertexSource);
        GL.CompileShader(vs);
        
        int fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, fragmentSource);
        GL.CompileShader(fs);
        
        _shader = GL.CreateProgram();
        GL.AttachShader(_shader, vs);
        GL.AttachShader(_shader, fs);
        GL.LinkProgram(_shader);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
        
        _attribLocTex = GL.GetUniformLocation(_shader, "Texture");
        _attribLocProjMtx = GL.GetUniformLocation(_shader, "ProjMtx");
        _attribLocVtxPos = GL.GetAttribLocation(_shader, "Position");
        _attribLocVtxUV = GL.GetAttribLocation(_shader, "UV");
        _attribLocVtxColor = GL.GetAttribLocation(_shader, "Color");
        
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();
        _vao = GL.GenVertexArray();
        
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        
        int stride = Unsafe.SizeOf<ImDrawVert>();
        GL.EnableVertexAttribArray(_attribLocVtxPos);
        GL.EnableVertexAttribArray(_attribLocVtxUV);
        GL.EnableVertexAttribArray(_attribLocVtxColor);
        GL.VertexAttribPointer(_attribLocVtxPos, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribPointer(_attribLocVtxUV, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.VertexAttribPointer(_attribLocVtxColor, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);
    }
    
    private unsafe void RecreateFontDeviceTexture()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height);
        
        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 
            width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        io.Fonts.SetTexID((IntPtr)_fontTexture);
        io.Fonts.ClearTexData();
    }

    private static void SetKeyMappings()
    {
        var io = ImGui.GetIO();
        io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down;
        io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp;
        io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown;
        io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home;
        io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End;
        io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Backspace;
        io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter;
        io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape;
        io.KeyMap[(int)ImGuiKey.Space] = (int)Keys.Space;
        io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A;
        io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C;
        io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V;
        io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z;
    }
    
    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteTexture(_fontTexture);
        GL.DeleteProgram(_shader);
        ImGui.DestroyContext(_context);
    }
}
