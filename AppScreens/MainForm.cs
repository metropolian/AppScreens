using CaptureSampleCore;
using Composition.WindowsRuntimeHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AppScreens
{
    public partial class MainForm : Form
    {
        private BasicCaptureWindow capturer;
        private Timer refresh_timer;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            refresh_timer = new Timer();
            refresh_timer.Interval = 100;
            refresh_timer.Tick += Refresh_Timer_Tick;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            ReloadList();
        }

        private void ReloadList()
        {
            comboBox1.Items.Clear();
            ReloadMonitorList();
            ReloadWindowList();

        }

        private void ReloadMonitorList()
        {
            var monitors = WPFCaptureSample.MonitorEnumerationHelper.GetMonitors();
                        
            foreach (var item in monitors)
            {
                comboBox1.Items.Add(new MonitorItem(item));
            }
        }

        private void ReloadWindowList()
        {
            foreach (var p in Process.GetProcesses())
            {
                if (//(!string.IsNullOrWhiteSpace(p.MainWindowTitle)) && 
                    WPFCaptureSample.WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle))
                {
                    comboBox1.Items.Add(new ProcessItem(p));
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = comboBox1.SelectedItem;
            if (item == null)
                return;
            StartCapture(item);
        }

        private void StartCapture(object item)
        {
            if (capturer != null)
            {
                capturer.Stop();
                capturer.Dispose();
                capturer = null;
            }

            try
            {
                capturer = new BasicCaptureWindow();

                if (item is ProcessItem)
                    capturer.Start(((ProcessItem)item).WindowHandle);
                else if (item is MonitorItem)
                    capturer.StartCaptureMonitor(((MonitorItem)item).Handle);
                else
                    capturer.Start(item);

                refresh_timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                capturer = null;
            }
            
        }

        private class ProcessItem
        {
            private Process p;
            public IntPtr WindowHandle
            { 
                get {
                    return p.MainWindowHandle;
                } 
            }

            public ProcessItem(Process p)
            {
                this.p = p;
            }

            public override string ToString()
            {
                return !string.IsNullOrEmpty(p.MainWindowTitle) ? p.MainWindowTitle : p.ProcessName;
            }


        }

        private class MonitorItem
        {
            public WPFCaptureSample.MonitorInfo item;

            public IntPtr Handle
            {
                get
                {
                    return item.Hmon;
                }
            }

            public MonitorItem(WPFCaptureSample.MonitorInfo item)
            {
                this.item = item;
            }

            public override string ToString()
            {
                return item.DeviceName;
            }
        }

        private void Refresh_Timer_Tick(object sender, EventArgs e)
        {
            Refresh_CaptureImage();
        }

        private void Refresh_CaptureImage()
        {
            if (capturer == null)
                return;

            if (pictureBox1.Image != null)
            {
                var image = pictureBox1.Image;
                pictureBox1.Image = null;
                image.Dispose();
            }

            MemoryStream data = new MemoryStream();
            if (capturer.GetBitmap(data) > 0)
            {
                data.Position = 0;

                var width = capturer.Width;
                var height = capturer.Height;
                var rowbytes = capturer.RowBytes;

                var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var bitmap_data = bitmap.LockBits(new Rectangle(0, 0, width, height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);
                var stride = bitmap_data.Stride;

                var buffer = new byte[rowbytes];
                var dest = bitmap_data.Scan0;

                try
                {
                    for (int y = 0; y < height - 1; y++)
                    {
                        data.Read(buffer, 0, rowbytes);
                        Marshal.Copy(buffer, 0, dest, stride);
                        dest = IntPtr.Add(dest, stride);
                    }
                }
                catch (Exception ex)
                {

                }


                bitmap.UnlockBits(bitmap_data);

                pictureBox1.Image = bitmap;
                pictureBox1.Refresh();

            }
            data.Dispose();
        }



        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Refresh_CaptureImage();
        }
    }
}
