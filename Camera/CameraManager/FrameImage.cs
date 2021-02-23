using OpenCvSharp;
using Ncer.Camera;
using System;

namespace Ncer.Camera
{
    /// <summary>
    /// 构建自相机Frame对象的Image
    /// </summary>
    public class FrameImage : CommonImage
    {
        private Frame frame;

        public Frame Frame { get => frame; set => frame = value; }

        public double Exposure { get; set; }

        public ExpoState ExpoState { get; set; }


        /// <summary>
        /// 从Frame创建FrameImage
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="copy"></param>
        /// <returns></returns>
        public static FrameImage CreateImageFromFrame(Frame frame, bool copy = true,bool depthExtend=false)
        {
            if (frame == null) return null;
            FrameImage image = new FrameImage();
            if (copy)
            {
                image.frame = (Frame)frame.Clone();
            }
            else
            {
                image.frame = frame;
            }
            MatType matType = MatType.CV_8UC1;
            int depth = 8;
            switch (frame.PixelFormat)
            {
                case PixelFormat.Mono8:
                    break;
                case PixelFormat.Mono10:
                    matType = MatType.CV_16UC1;
                    depth = 10;
                    break;
                case PixelFormat.Mono12:
                    matType = MatType.CV_16UC1;
                    depth = 12;
                    break;
                case PixelFormat.Mono14:
                    matType = MatType.CV_16UC1;
                    depth = 14;
                    break;
                case PixelFormat.Mono16:
                    matType = MatType.CV_16UC1;
                    depth = 16;
                    break;
                case PixelFormat.RGB8:
                    matType = MatType.CV_8UC3;
                    break;
                default:
                    break;
            }
            Mat mat = new Mat(image.frame.Height, image.frame.Width, matType, image.frame.Data);
            image.Mat = mat;
            if (depthExtend && image.Depth - depth>0)
            {
                image.Mat *= (int)(Math.Pow(2, image.Depth - depth));
                image.DataDepth = image.Depth;
            }
            else
            {
                image.DataDepth = depth;
            }
            image.Exposure = frame.ExposureTime;
            image.UserData.Add("Exposure", frame.ExposureTime);
            return image;
        }

    }

}
