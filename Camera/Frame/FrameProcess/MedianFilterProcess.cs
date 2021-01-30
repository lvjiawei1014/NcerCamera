using Ncer.ImageToolkit;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncer.Camera.FrameFrocess
{
    public class MedianBlurProcess : IFrameProcess
    {
        public int Ksize { get; set; } = 3;

        public void Process(MatImage image)
        {
            Cv2.MedianBlur(image.Mat, image.Mat, Ksize);
        }
    }
}
