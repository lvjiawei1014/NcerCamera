using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraDemo
{
    class Utils
    {
        public static ImageSource MatToImageSource(Mat mat)
        {
            if (mat == null)
            {
                return null;
            }
            int stride = (int)mat.Step();

            var bitmapImage = BitmapImage.Create(mat.Width, mat.Height, 96.0, 96.0, PixelFormats.Gray16, BitmapPalettes.Gray256, mat.Data, stride * mat.Height, stride);
            return bitmapImage;
        }
    }
}
