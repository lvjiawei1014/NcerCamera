using Ncer.ImageToolkit;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncer.Camera.FrameFrocess
{
    /// <summary>
    /// 污点修复 工具u
    /// </summary>
    public class BlotInpaint : IFrameProcess
    {
        private MatImage inpaintMask;

        public MatImage InpaintMask { get => inpaintMask; set => inpaintMask = value; }
        public double RepairRadius { get; set; } = 99;

        public void Process(MatImage image)
        {
            if (inpaintMask == null) return;
            Cv2.Inpaint(image.Mat, inpaintMask.Mat, image.Mat, RepairRadius, InpaintMethod.Telea);
        }

        public BlotInpaint(MatImage region)
        {
            this.inpaintMask = region;
        }
    }
}
