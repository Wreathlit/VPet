using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.DirectWrite;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using VPet_Simulator.Core.New;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// Interaction logic for AnimationCanvas.xaml
    /// </summary>
    public partial class AnimationCanvas : ContentControl, IGraph, IDisposable
    {
        private readonly MonoGameGraphicsDeviceService _graphicsDeviceService = new MonoGameGraphicsDeviceService();
        public GraphicsDevice GraphicsDevice => _graphicsDeviceService.GraphicsDevice;
        public MonoGameGraphicsDeviceService GraphicsDeviceService => _graphicsDeviceService;

        bool IsInDesignMode
        {
            get
            {
                return (bool)DesignerProperties.IsInDesignModeProperty
                            .GetMetadata(typeof(DependencyObject)).DefaultValue;
            }
        }
        private int _instanceCount;
        //
        private readonly GameTime _gameTime = new GameTime();
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private D3DImage _direct3DImage;
        private RenderTarget2D _renderTarget;
        private SharpDX.Direct3D9.Texture _renderTargetD3D9;

        private bool _isInitialized;
        public bool IsDisposed { get; private set; }

        private SpriteBatch _spriteBatch;
        private Texture2D _texture2D;
        private Texture2D _texture2DPrevious;
        private bool isUpdating = false;

        public AnimationCanvas()
        {
            if (IsInDesignMode)
                return;

            InitializeComponent();
            _instanceCount++;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            DataContextChanged += (sender, args) =>
            {

            };

            SizeChanged += (sender, args) =>
            {

            };
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_graphicsDeviceService != null)
            {
                CompositionTarget.Rendering -= OnRender;
                ResetBackBufferReference();
                _graphicsDeviceService.DeviceResetting -= OnGraphicsDeviceServiceDeviceResetting;
            }
        }
        private void Start()
        {
            if (_isInitialized)
                return;

            _direct3DImage = new D3DImage();

            AddChild(new Image { Source = _direct3DImage, Stretch = Stretch.None });

            //_direct3DImage.IsFrontBufferAvailableChanged += OnDirect3DImageIsFrontBufferAvailableChanged;

            _renderTarget = CreateRenderTarget();

            _spriteBatch = new SpriteBatch(GraphicsDevice);

            CompositionTarget.Rendering += OnRender;
            _stopwatch.Start();
            _isInitialized = true;
        }
        private void OnGraphicsDeviceServiceDeviceResetting(object sender, EventArgs e)
        {
            ResetBackBufferReference();
        }
        ~AnimationCanvas()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            _renderTarget?.Dispose();
            _renderTargetD3D9?.Dispose();
            _instanceCount--;

            if (_instanceCount <= 0)
                _graphicsDeviceService?.Dispose();

            IsDisposed = true;
        }

        private bool HandleDeviceReset()
        {
            if (GraphicsDevice == null)
                return false;

            var deviceNeedsReset = false;

            switch (GraphicsDevice.GraphicsDeviceStatus)
            {
                case GraphicsDeviceStatus.Lost:
                    // If the graphics device is lost, we cannot use it at all.
                    return false;

                case GraphicsDeviceStatus.NotReset:
                    // If device is in the not-reset state, we should try to reset it.
                    deviceNeedsReset = true;
                    break;
            }

            if (deviceNeedsReset)
            {
                _graphicsDeviceService.ResetDevice((int)ActualWidth, (int)ActualHeight);
                return false;
            }

            return true;
        }
        private RenderTarget2D CreateRenderTarget()
        {
            var actualWidth = (int)ActualWidth;
            var actualHeight = (int)ActualHeight;

            if (actualWidth == 0 || actualHeight == 0)
                return null;

            if (GraphicsDevice == null)
                return null;

            var renderTarget = new RenderTarget2D(GraphicsDevice, actualWidth, actualHeight,
                false, SurfaceFormat.Bgra32, DepthFormat.Depth24Stencil8, 1,
                RenderTargetUsage.PlatformContents, true);

            var handle = renderTarget.GetSharedHandle();

            if (handle == IntPtr.Zero)
                throw new ArgumentException("Handle could not be retrieved");

            _renderTargetD3D9 = new SharpDX.Direct3D9.Texture(_graphicsDeviceService.Direct3DDevice, renderTarget.Width,
                renderTarget.Height,
                1, SharpDX.Direct3D9.Usage.RenderTarget, SharpDX.Direct3D9.Format.A8R8G8B8,
                SharpDX.Direct3D9.Pool.Default, ref handle);

            using (var surface = _renderTargetD3D9.GetSurfaceLevel(0))
            {
                _direct3DImage.Lock();
                _direct3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                _direct3DImage.Unlock();
            }

            return renderTarget;
        }
        private void SetViewport()
        {
            // Many GraphicsDeviceControl instances can be sharing the same
            // GraphicsDevice. The device backbuffer will be resized to fit the
            // largest of these controls. But what if we are currently drawing
            // a smaller control? To avoid unwanted stretching, we set the
            // viewport to only use the top left portion of the full backbuffer.
            var width = Math.Max(1, (int)ActualWidth);
            var height = Math.Max(1, (int)ActualHeight);
            GraphicsDevice.Viewport = new Viewport(0, 0, width, height);
        }
        private bool CanBeginDraw()
        {
            // If we have no graphics device, we must be running in the designer.
            if (_graphicsDeviceService == null)
                return false;

            if (!_direct3DImage.IsFrontBufferAvailable)
                return false;

            // Make sure the graphics device is big enough, and is not lost.
            if (!HandleDeviceReset())
                return false;

            return true;
        }
        private void OnRender(object sender, EventArgs e)
        {
            _gameTime.ElapsedGameTime = _stopwatch.Elapsed;
            _gameTime.TotalGameTime += _gameTime.ElapsedGameTime;
            _stopwatch.Restart();

            if (CanBeginDraw())
            {
                try
                {
                    _direct3DImage.Lock();

                    if (_renderTarget == null)
                        _renderTarget = CreateRenderTarget();

                    if (_renderTarget != null && _texture2D != null && !isUpdating)
                    {
                        GraphicsDevice.SetRenderTarget(_renderTarget);
                        SetViewport();

                        lock (_texture2D)
                        {
                            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
                            _spriteBatch.Begin();
                            _spriteBatch.Draw(_texture2D, new Vector2(0, 0), _texture2D.Bounds, Microsoft.Xna.Framework.Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                            _spriteBatch.End();
                        }
                        //render here


                        GraphicsDevice.Flush();
                        _direct3DImage.AddDirtyRect(new Int32Rect(0, 0, (int)ActualWidth, (int)ActualHeight));
                    }
                }
                finally
                {
                    _direct3DImage.Unlock();
                    GraphicsDevice.SetRenderTarget(null);
                }
            }
        }
        private void ResetBackBufferReference()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (_renderTarget != null)
            {
                _renderTarget.Dispose();
                _renderTarget = null;
            }

            if (_renderTargetD3D9 != null)
            {
                _renderTargetD3D9.Dispose();
                _renderTargetD3D9 = null;
            }

            _direct3DImage.Lock();
            _direct3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            _direct3DImage.Unlock();
        }
        public void Order(Stream stream)
        {
            isUpdating = true;

            if (_texture2D != null)
            {
                lock (_texture2D)
                {
                    _texture2D.Dispose();
                    _texture2D = Texture2D.FromStream(GraphicsDevice, stream);
                }
            }
            else
            {
                _texture2D = Texture2D.FromStream(GraphicsDevice, stream);
            }

            isUpdating = false;
        }

        void IGraph.Clear()
        {
            _texture2D = null;
        }

        public void OrderTexture(Texture2D texture, bool disposePrevious = false)
        {
            if (_texture2D != null) _texture2DPrevious = _texture2D;
            _texture2D = texture;
            if (disposePrevious && _texture2DPrevious != null && !_texture2DPrevious.IsDisposed) _texture2DPrevious.Dispose();
        }

        public GraphicsDevice GetGraphicsDevice()
        {
            return GraphicsDevice;
        }
    }
}