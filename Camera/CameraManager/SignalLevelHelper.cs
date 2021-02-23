using Ncer.ImageToolkit;
using System;
namespace Ncer.Camera
{
    public class SignalLevelHelper
    {
        private OpenCvSharp.Rect region;
        public OpenCvSharp.Rect Region { get => region; set => region = value; }

        public double ComputeSignalLevel(CommonImage image)
        {
            double level = 0;
            MatImage tmp = image;
            double min, max;
            OpenCvSharp.Point minPoint, maxPoint;
            if (Region == null || Region.X < 0 || Region.Y < 0 || Region.Width <= 0 || Region.Height <= 0)
            {
                tmp.Mat.MinMaxLoc(out min, out max, out minPoint, out maxPoint);
            }
            else
            {
                tmp.Mat[region].MinMaxLoc(out min, out max, out minPoint, out maxPoint);
            }
            image.Min = min;
            image.Max = max;
            image.MaxPoint = maxPoint;
            image.MinPoint = minPoint;
            level = Math.Min(1, max / Math.Pow(2, image.DataDepth));
            image.SignalLevel = level;
            return level;
        }
    }
}
