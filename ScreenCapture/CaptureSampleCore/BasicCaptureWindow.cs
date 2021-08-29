using Composition.WindowsRuntimeHelpers;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace CaptureSampleCore
{
    public class BasicCaptureWindow : IDisposable
    {
        public BasicCapture capturer;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int RowBytes { get; private set; }
        public bool Available
        {
            get
            {
                return (capturer != null) && (capturer.Frames > 0);
            }
        }

        public void Start(object item)
        {
            var device = Direct3D11Helper.CreateDevice();
            capturer = new BasicCapture(device, (GraphicsCaptureItem)item);
            capturer.StartCapture();
        }

        public void Start(IntPtr hwnd)
        {            
            var item = CaptureHelper.CreateItemForWindow(hwnd);
            var device = Direct3D11Helper.CreateDevice();
            capturer = new BasicCapture(device, item);
            capturer.StartCapture();            
        }

        public void StartCaptureMonitor(IntPtr handle)
        {
            var item = CaptureHelper.CreateItemForMonitor(handle);
            var device = Direct3D11Helper.CreateDevice();
            capturer = new BasicCapture(device, item);
            capturer.StartCapture();
        }


        public long GetBitmap(Stream data)
        {
            long result = 0;
            if (!Available)
                return 0;

            var device = capturer.Device;
            var backbuffer = capturer.SwapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0);

            Width = backbuffer.Description.Width;
            Height = backbuffer.Description.Height;

            SharpDX.Direct3D11.Texture2D source_texture = null;

            try
            {
                
                var source_description = new SharpDX.Direct3D11.Texture2DDescription()
                {
                    Width = backbuffer.Description.Width,
                    Height = backbuffer.Description.Height,
                    Format = backbuffer.Description.Format,
                    MipLevels = 1,
                    ArraySize = 1,
                    BindFlags = SharpDX.Direct3D11.BindFlags.None,
                    OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
                    CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Read,
                    SampleDescription = { Count = 1, Quality = 0 },
                    Usage = SharpDX.Direct3D11.ResourceUsage.Staging,
                };

                source_texture = new SharpDX.Direct3D11.Texture2D(device, source_description);
                device.ImmediateContext.CopyResource(backbuffer, source_texture);                
                
                var source_databox = device.ImmediateContext.MapSubresource(source_texture, 0, SharpDX.Direct3D11.MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                var sourcePtr = source_databox.DataPointer;
                var stride = source_databox.RowPitch;
                var buffer = new byte[stride];

                RowBytes = stride;

                for (int y = 0; y < Height; y++)
                {
                    Marshal.Copy(sourcePtr, buffer, 0, stride);
                    data.Write(buffer, 0, stride);

                    sourcePtr = IntPtr.Add(sourcePtr, stride);
                    result += stride;
                }

                device.ImmediateContext.UnmapSubresource(source_texture, 0);

            }
            catch (Exception ex)
            {

                
            }

            
            if (source_texture != null)
                source_texture.Dispose();           
            
            return result;
        }

        public void Stop()
        {
            if (capturer == null)
                return;

            capturer.Dispose();
            capturer = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
