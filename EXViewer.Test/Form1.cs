using OpenTK;
using GFX = OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EXViewer.Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        #region Program


        public int VertexPositionLocation { get; set; }

        public int VertexTextureLocation { get; set; }

        public int ColorMapUniformLocation { get; set; }

        private int ShaderProgramHandle { get; set; }

        private int VertexShaderHandle { get; set; }

        private int FragmentShaderHandle { get; set; }

        private void InitProgram()
        {
            // Create vertex shader
            var vertexShaderLines = new List<string>();

            vertexShaderLines.Add("#version 120");
            vertexShaderLines.Add("");
            vertexShaderLines.Add("attribute vec3 vertex_position;");
            vertexShaderLines.Add("attribute vec2 vertex_texture;");
            vertexShaderLines.Add("");
            vertexShaderLines.Add("varying vec2 texcoord;");
            vertexShaderLines.Add("");
            vertexShaderLines.Add("void main(void)");
            vertexShaderLines.Add("{");
            vertexShaderLines.Add("     gl_Position = vec4( vertex_position , 1 );");
            vertexShaderLines.Add("     texcoord = vertex_texture;");
            vertexShaderLines.Add("}");

            var vertexShaderCode = string.Join(Environment.NewLine, vertexShaderLines);

            VertexShaderHandle = CreateShader(vertexShaderCode, ShaderType.VertexShader);

            CheckError();

            // Create fragment shader
            var fragmentShaderLines = new List<string>();

            fragmentShaderLines.Add("#version 120");
            fragmentShaderLines.Add("");
            fragmentShaderLines.Add("uniform sampler2D colormap;");
            fragmentShaderLines.Add("");
            fragmentShaderLines.Add("varying vec2 texcoord;");
            fragmentShaderLines.Add("");
            fragmentShaderLines.Add("void main(void)");
            fragmentShaderLines.Add("{");
            fragmentShaderLines.Add("   gl_FragColor = texture2D( colormap, texcoord );");
            fragmentShaderLines.Add("}");

            var fragmentShaderCode = string.Join(Environment.NewLine, fragmentShaderLines);

            FragmentShaderHandle = CreateShader(fragmentShaderCode, ShaderType.FragmentShader);

            CheckError();

            // Create program
            ShaderProgramHandle = GL.CreateProgram();

            // Attach vertex shader to program
            GL.AttachShader(ShaderProgramHandle, VertexShaderHandle);

            CheckError();

            // Attach fragment shader to program
            GL.AttachShader(ShaderProgramHandle, FragmentShaderHandle);

            CheckError();

            // Link program
            GL.LinkProgram(ShaderProgramHandle);

            CheckError();

            VertexPositionLocation = GL.GetAttribLocation(ShaderProgramHandle, "vertex_position");
            VertexTextureLocation = GL.GetAttribLocation(ShaderProgramHandle, "vertex_texture");
            ColorMapUniformLocation = GL.GetUniformLocation(ShaderProgramHandle, "colormap");
        }

        private int CreateShader(string source, ShaderType type)
        {
            int handle = GL.CreateShader(type);
            GL.ShaderSource(handle, source);
            GL.CompileShader(handle);


            int compiled = 0;
            GL.GetShader(handle, ShaderParameter.CompileStatus, out compiled);
            if (compiled == 0)
            {
                GL.DeleteShader(handle);
                throw new InvalidOperationException("Unable to compile shader of type : " + type.ToString());
            }

            return handle;
        }

        #endregion

        private int BackgroundArrayBufferHandle { get; set; }

        private int BackgroundTextureHandle { get; set; }

        private bool IsDrawingBackground { get; set; }

        private Rectangle OriginalRect { get; set; }

        private bool Fullscreen { get; set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            InitProgram();

            InitBackground();

            GL.Viewport(0, 0, this.viewer.Width, this.viewer.Height);

            CheckError();

            this.viewer.Load += viewer_Load;
            this.viewer.Paint += viewer_Paint;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            IsDrawingBackground = true;
            OriginalRect = this.viewer.Bounds;
        }

        private void CheckError()
        {
            ErrorCode ec = GL.GetError();

            if (ec != 0)
            {
                throw new System.Exception(ec.ToString());
            }
        }

        private void InitBackground()
        {
            GL.ClearColor(Color.White);

            // generate BackgroundArrayBufferHandle
            {
                var vertexDatas = new VertexV3fN3fT2f[6];

                float border = 1f;

                float z = 0;

                vertexDatas[0] = new VertexV3fN3fT2f() { Position = new Vector3(border, -border, z), Texture = new Vector2(1, 1) };
                vertexDatas[1] = new VertexV3fN3fT2f() { Position = new Vector3(-border, -border, z), Texture = new Vector2(0, 1) };
                vertexDatas[2] = new VertexV3fN3fT2f() { Position = new Vector3(border, border, z), Texture = new Vector2(1, 0) };

                vertexDatas[3] = new VertexV3fN3fT2f() { Position = new Vector3(-border, border, z), Texture = new Vector2(0, 0) };
                vertexDatas[4] = new VertexV3fN3fT2f() { Position = new Vector3(border, border, z), Texture = new Vector2(1, 0) };
                vertexDatas[5] = new VertexV3fN3fT2f() { Position = new Vector3(-border, -border, z), Texture = new Vector2(0, 1) };

                int handle;

                GL.GenBuffers(1, out handle);
                GL.BindBuffer(BufferTarget.ArrayBuffer, handle);
                GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertexDatas.Length * VertexV3fN3fT2f.SizeInBytes), vertexDatas, BufferUsageHint.DynamicDraw);

                BackgroundArrayBufferHandle = handle;

                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }

            // generate BackgroundTextureHandle
            {
                var bmp = EXViewer.Test.Properties.Resources.t;

                int handle;
                GL.GenTextures(1, out handle);

                CheckError();

                GL.BindTexture(TextureTarget.Texture2D, handle);

                CheckError();

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

                CheckError();

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                CheckError();

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder); // support tiling 

                CheckError();

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder); // support tiling 

                CheckError();

                int width = bmp.Width;
                int height = bmp.Height;

                var bmpData = bmp.LockBits(
                    new Rectangle(0, 0, width, height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    width,
                    height,
                    0,
                    GFX.OpenGL.PixelFormat.Bgra,
                    PixelType.UnsignedByte,
                    bmpData.Scan0);

                bmp.UnlockBits(bmpData);

                CheckError();

                GL.BindTexture(TextureTarget.Texture2D, 0);

                CheckError();

                BackgroundTextureHandle = handle;
            }

        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct VertexV3fN3fT2f
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 Texture;

            public static readonly IntPtr PositionOffset = Marshal.OffsetOf(typeof(VertexV3fN3fT2f), "Position");
            public static readonly IntPtr NormalOffset = Marshal.OffsetOf(typeof(VertexV3fN3fT2f), "Normal");
            public static readonly IntPtr TextureOffset = Marshal.OffsetOf(typeof(VertexV3fN3fT2f), "Texture");
            public static readonly int SizeInBytes = Marshal.SizeOf(typeof(VertexV3fN3fT2f));

            public override string ToString()
            {
                return string.Format("Vertex: {0} - Normal: {1} - Texture: {2}", Position, Normal, Texture);
            }
        }
        
        private void viewer_Paint(object sender, PaintEventArgs e)
        {
            if (this.DesignMode) return;

            this.viewer.MakeCurrent();

            GL.Viewport(0, 0, this.viewer.Width, this.viewer.Height);

            CheckError();

            GL.UseProgram(ShaderProgramHandle);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            CheckError();

            if (IsDrawingBackground)
            {
                GL.ActiveTexture(TextureUnit.Texture0);

                CheckError();

                GL.BindTexture(TextureTarget.Texture2D, BackgroundTextureHandle);

                CheckError();

                GL.Uniform1(ColorMapUniformLocation, 0);

                CheckError();

                GL.EnableVertexAttribArray(VertexPositionLocation);
                GL.EnableVertexAttribArray(VertexTextureLocation);

                CheckError();

                GL.BindBuffer(BufferTarget.ArrayBuffer, BackgroundArrayBufferHandle);

                CheckError();

                GL.VertexAttribPointer(VertexPositionLocation, 3, VertexAttribPointerType.Float, false, VertexV3fN3fT2f.SizeInBytes, VertexV3fN3fT2f.PositionOffset);
                GL.VertexAttribPointer(VertexTextureLocation, 2, VertexAttribPointerType.Float, false, VertexV3fN3fT2f.SizeInBytes, VertexV3fN3fT2f.TextureOffset);

                CheckError();

                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

                CheckError();

                GL.DisableVertexAttribArray(VertexPositionLocation);
                GL.DisableVertexAttribArray(VertexTextureLocation);

                CheckError();

                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                CheckError();

                GL.BindTexture(TextureTarget.Texture2D, 0);

                CheckError();
            }

            this.viewer.SwapBuffers(); 
        }

        private void viewer_Load(object sender, EventArgs e)
        {
            if (this.DesignMode) return;

            InitProgram();

            InitBackground();

            GL.ClearColor(Color.Green);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("clicking");

            if (!Fullscreen)
            {
                this.viewer.Bounds = Rectangle.FromLTRB(0, 40, this.ClientRectangle.Width, this.ClientRectangle.Height);
            }
            else
            {
                this.viewer.Bounds = OriginalRect;
            }

            Thread.Sleep(1000);
            this.viewer.Refresh();

            Fullscreen = !Fullscreen;

            //System.Diagnostics.Debug.WriteLine("clicked");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.IsDrawingBackground = !this.IsDrawingBackground;
            this.viewer.Refresh();
        }

    }
}
