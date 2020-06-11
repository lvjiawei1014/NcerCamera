using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ncer.Camera;
using OpenCvSharp;
using ImageProcessToolkit;
using System.Diagnostics;

namespace CameraDemo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {

        ApogeeCamera apogeeCamera;
        ToupCamera toupCamera;
        Camera camera;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnInitApogee_Click(object sender, RoutedEventArgs e)
        {
            this.apogeeCamera = new ApogeeCamera();
            try
            {
                apogeeCamera.Init();
                apogeeCamera.OnCameraPreviewEvent += Camera_OnCameraPreviewEvent;
                txtInfo.Text = apogeeCamera.CameraModel+"\r\n";
                txtInfo.Text += apogeeCamera.PixelSize.pixelSizeX.ToString() + "\r\n";
                var reso = apogeeCamera.GetResolution();
                txtInfo.Text += reso.width+"*"+reso.height + "\r\n";
                apogeeCamera.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Camera_OnCameraPreviewEvent(object sender, Ncer.Camera.Frame frame)
        {
            this.Dispatcher.Invoke(() =>
            {
                MatImage image = MatImage.CreateFromPtr(frame.Width, frame.Height, MatType.CV_16UC1, frame.Data);
                var img = Utils.MatToImageSource(image.Mat);
                this.imageMain.Source = img.Clone();

            });
        }

        private async void btnTake_ClickAsync(object sender, RoutedEventArgs e)
        {
            await TakeAsync();

        }




        public async Task TakeAsync()
        {
            GC.Collect();

            Stopwatch sw = new Stopwatch();
            System.Console.WriteLine("take");
            sw.Start();
            camera.ExposureTime = 400;
            var raw = await camera.TakeImageAsync();

            var frame = (Ncer.Camera.Frame)(raw.Clone());

            System.Console.WriteLine("take frame:" + sw.ElapsedMilliseconds + "ms");
            if (frame == null)
            {
                MessageBox.Show("no frame");
                return;
            }
            MatImage image = MatImage.CreateFromPtr(frame.Width, frame.Height, MatType.CV_16SC1, frame.Data);
            System.Console.WriteLine("create mat:" + sw.ElapsedMilliseconds + "ms");

            var img = Utils.MatToImageSource(image.Mat);
            System.Console.WriteLine("take img:" + sw.ElapsedMilliseconds + "ms");


            this.imageMain.Source = img.Clone();
            System.Console.WriteLine("show img:" + sw.ElapsedMilliseconds + "ms");
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            camera.StartPreview();
        }

        private void btnStopPreview_Click(object sender, RoutedEventArgs e)
        {
            camera.StopPreview();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            camera.Reset();
        }

        private void btnInitToup_Click(object sender, RoutedEventArgs e)
        {
            this.camera = new ToupCamera();
            try
            {
                camera.Init();
                camera.OnCameraPreviewEvent += Camera_OnCameraPreviewEvent;
                txtInfo.Text = camera.CameraModel + "\r\n";
                txtInfo.Text += camera.PixelSize.pixelSizeX.ToString() + "\r\n";
                var preReso = camera.GetPreviewResolutions();
                camera.SetPreviewResolution(preReso.First());
                var resos = camera.GetResolutions();
                camera.SetResolution(resos.First());
                var reso = camera.GetResolution();
                txtInfo.Text += reso.width + "*" + reso.height + "\r\n";

                camera.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
