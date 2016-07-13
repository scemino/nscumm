using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
using OpenTK.Platform.Android;

using Android.Content;
using Android.Util;
using Android.Views.InputMethods;
using Android.Text;

namespace NScumm.Droid
{
    internal class EditableSurfaceView : AndroidGameView
    {
        static readonly float[] _vertexBuffer = {
             0, 0, 0,  // bottom left corner
             0,  1, 0,  // top left corner
             1,  1, 0,  // top right corner
             1, 0, 0}; // bottom right corner

        static readonly float[] _texCoord = {
            0, 0,
            0, 1,
            1, 1,
            1, 0 };

        static readonly ushort[] _indices = {
            1, 0, 3,     // first triangle (bottom left - top left - top right)
            3, 2, 1 };

        private bool _setViewport = true;
        private int _program;
        private int _mtrxhandle, _mPositionHandle, _textureHandle, _texCoordIn;

        private int vertexbuffer, textureID, texCoordBuffer;
        private Matrix4 mtrxProjection;

        // HACK: OK this ugly but temporarly, right ?
        public  byte[] _color;
        public byte[] _pixelsCursor;
        public int _width;
        public int _height;

        public EditableSurfaceView(Context context) : base(context)
        {
            KeepScreenOn = true;
            Focusable = true;
            FocusableInTouchMode = true;

            // Do not set context on render frame as we will be rendering
            // on separate thread and thus Android will not set GL context
            // behind our back
            AutoSetContextOnRenderFrame = false;

            // Render on separate thread. This gains us
            // fluent rendering. Be careful to not use GL calls on UI thread.
            // OnRenderFrame is called from rendering thread, so do all
            // the GL calls there
            RenderOnUIThread = false;

            Resize += delegate
            {
                //text.SetupProjection(Width, Height);
                _setViewport = true;
            };
        }



        public override IInputConnection OnCreateInputConnection(EditorInfo outAttrs)
        {
            outAttrs.InitialCapsMode = 0;
            outAttrs.InitialSelEnd = outAttrs.InitialSelStart = -1;
            outAttrs.InputType = InputTypes.ClassText |
                InputTypes.TextVariationNormal |
                InputTypes.TextFlagAutoComplete;
            outAttrs.ImeOptions = ImeFlags.NoExtractUi;

            return new MyInputConnection(this);
        }

        public override bool OnCheckIsTextEditor()
        {
            return false;
        }

        // This gets called when the drawing surface is ready
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Run the render loop
            Run();
        }

        protected override void OnContextSet(EventArgs e)
        {
            base.OnContextSet(e);
            Console.WriteLine("OpenGL version: {0} GLSL version: {1}", GL.GetString(StringName.Version), GL.GetString(StringName.ShadingLanguageVersion));

            GL.Viewport(0, 0, Width, Height);

            LoadShaders();

            //// vertexbuffer
            //GL.GenBuffers(1, out vertexbuffer);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, vertexbuffer);
            //GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(_vertexBuffer.Length * sizeof(float)), _vertexBuffer, BufferUsage.StaticDraw);

            //// texCoordBuffer
            //GL.GenBuffers(1, out texCoordBuffer);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordBuffer);
            //GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(_texCoord.Length * sizeof(float)), _texCoord, BufferUsage.StaticDraw);

            // textureID
            GL.GenTextures(1, out textureID);
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            //var bmp2 = Android.Graphics.BitmapFactory.DecodeResource(Resources, Resource.Drawable.scummvm);
            //Android.Opengl.GLUtils.TexImage2D((int)All.Texture2D, 0, bmp2, 0);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)All.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)All.ClampToEdge);
        }

        // This method is called everytime the context needs
        // to be recreated. Use it to set any egl-specific settings
        // prior to context creation
        //
        // In this particular case, we demonstrate how to set
        // the graphics mode and fallback in case the device doesn't
        // support the defaults
        protected override void CreateFrameBuffer()
        {
            ContextRenderingApi = GLVersion.ES2;

            // The default GraphicsMode that is set consists of (16, 16, 0, 0, 2, false)
            try
            {
                Log.Verbose("GLTemplateES20", "Loading with default settings");

                // If you don't call this, the context won't be created
                base.CreateFrameBuffer();
                return;
            }
            catch (Exception ex)
            {
                Log.Verbose("GLTemplateES20", "{0}", ex);
            }

            // This is a graphics setting that sets everything to the lowest mode possible so
            // the device returns a reliable graphics setting.
            try
            {
                Log.Verbose("GLTemplateES20", "Loading with custom Android settings (low mode)");
                GraphicsMode = new AndroidGraphicsMode(0, 0, 0, 0, 0, false);

                // If you don't call this, the context won't be created
                base.CreateFrameBuffer();
                return;
            }
            catch (Exception ex)
            {
                Log.Verbose("GLTemplateES20", "{0}", ex);
            }
            throw new Exception("Can't load egl, aborting");
        }

        // This gets called on each frame render
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            // You only need to call this if you have delegates
            // registered that you want to have called
            base.OnRenderFrame(e);

            if (_setViewport)
            {
                _setViewport = false;
                GL.Viewport(0, 0, Width, Height);
                Matrix4.CreateOrthographicOffCenter(0, 1, 1, 0, 1, 0, out mtrxProjection);
            }

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.UseProgram(_program);

            _mtrxhandle = GL.GetUniformLocation(_program, "projection");
            _mPositionHandle = GL.GetAttribLocation(_program, "position");
            _textureHandle = GL.GetUniformLocation(_program, "texture");
            _texCoordIn = GL.GetAttribLocation(_program, "texCoordIn");

            GL.UniformMatrix4(_mtrxhandle, false, ref mtrxProjection);
            GL.Uniform1(_textureHandle, 0);

            // Set the active texture unit to texture unit 0.
            GL.ActiveTexture(TextureUnit.Texture0);

            // Bind the texture to this unit.
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, _width,
                          _height, 0, PixelFormat.Rgb,
                          PixelType.UnsignedByte, _color);

            // vertex buffer
            GL.EnableVertexAttribArray(_mPositionHandle);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, vertexbuffer);
            GL.VertexAttribPointer(
               _mPositionHandle,                  // attribute 0. No particular reason for 0, but must match the layout in the shader.
               3,                  // size
                VertexAttribPointerType.Float,           // type
                false,           // normalized?
               0,                  // stride
                _vertexBuffer            // array buffer offset
            );

            // texcoord
            GL.EnableVertexAttribArray(_texCoordIn);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordBuffer);
            GL.VertexAttribPointer(
                _texCoordIn,                  // attribute 0. No particular reason for 0, but must match the layout in the shader.
               2,                  // size
                VertexAttribPointerType.Float,           // type
                false,           // normalized?
               0,                  // stride
                _texCoord            // array buffer offset
            );

            GL.DrawElements(BeginMode.Triangles, _indices.Length, DrawElementsType.UnsignedShort, _indices);

            SwapBuffers();
        }

        internal void LoadShaders()
        {
            LoadShaders(LoadResource("NScumm.Droid.Shaders.Shader.vsh"), LoadResource("NScumm.Droid.Shaders.Shader.fsh"));
        }

        private string LoadResource(string name)
        {
            return new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name)).ReadToEnd();
        }

        private static bool CompileShader(ShaderType type, string src, out int shader)
        {
            shader = GL.CreateShader(type);
            GL.ShaderSource(shader, src);
            GL.CompileShader(shader);

#if DEBUG
            int logLength = 0;
            GL.GetShader(shader, ShaderParameter.InfoLogLength, out logLength);
            if (logLength > 0)
                Console.WriteLine("Shader compile log:\n{0}", GL.GetShaderInfoLog(shader));
#endif

            int status = 0;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
            if (status == 0)
            {
                GL.DeleteShader(shader);
                return false;
            }

            return true;
        }

        private static bool LinkProgram(int prog)
        {
            GL.LinkProgram(prog);

#if DEBUG
            int logLength = 0;
            GL.GetProgram(prog, ProgramParameter.InfoLogLength, out logLength);
            if (logLength > 0)
                Console.WriteLine("Program link log:\n{0}", GL.GetProgramInfoLog(prog));
#endif
            int status = 0;
            GL.GetProgram(prog, ProgramParameter.LinkStatus, out status);
            if (status == 0)
                return false;

            return true;
        }

        private bool LoadShaders(string vertShaderSource, string fragShaderSource)
        {
            Console.WriteLine("load shaders");
            int vertShader, fragShader;

            // Create shader program.
            _program = GL.CreateProgram();

            // Create and compile vertex shader.
            if (!CompileShader(ShaderType.VertexShader, vertShaderSource, out vertShader))
            {
                Console.WriteLine("Failed to compile vertex shader");
                return false;
            }
            // Create and compile fragment shader.
            if (!CompileShader(ShaderType.FragmentShader, fragShaderSource, out fragShader))
            {
                Console.WriteLine("Failed to compile fragment shader");
                return false;
            }

            // Attach vertex shader to program.
            GL.AttachShader(_program, vertShader);

            // Attach fragment shader to program.
            GL.AttachShader(_program, fragShader);

            // Bind attribute locations.
            // This needs to be done prior to linking.
            //GL.BindAttribLocation(program, ATTRIB_VERTEX, "position");
            //GL.BindAttribLocation(program, ATTRIB_NORMAL, "normal");

            // Link program.
            if (!LinkProgram(_program))
            {
                Console.WriteLine("Failed to link program: {0:x}", _program);

                if (vertShader != 0)
                    GL.DeleteShader(vertShader);

                if (fragShader != 0)
                    GL.DeleteShader(fragShader);

                if (_program != 0)
                {
                    GL.DeleteProgram(_program);
                    _program = 0;
                }
                return false;
            }

            // Release vertex and fragment shaders.
            if (vertShader != 0)
            {
                GL.DetachShader(_program, vertShader);
                GL.DeleteShader(vertShader);
            }

            if (fragShader != 0)
            {
                GL.DetachShader(_program, fragShader);
                GL.DeleteShader(fragShader);
            }

            return true;
        }
    }

}


