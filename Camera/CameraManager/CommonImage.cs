using Ncer.ImageToolkit;
using OpenCvSharp;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Ncer.Camera
{
    /// <summary>
    /// 包含矩阵数据的通用图像类
    /// 包含一些常用信息
    /// </summary>
    public class CommonImage:MatImage
    {
        private string name = "";
        private string path;
        private int depth = 8;
        private double max;
        private double min;
        private double signalLevel;
        private double mean;
        private ImageSource thumb;
        private Histograms histograms;
        private OpenCvSharp.Point maxPoint;
        private OpenCvSharp.Point minPoint;



        public string Path { get => path; set => path = value; }
        /// <summary>
        /// 表示实际存储数据的深度
        /// </summary>
        public int DataDepth { get => depth; set => depth = value; }
        public double Max { get => max; set => max = value; }
        public double Min { get => min; set => min = value; }
        public double SignalLevel { get => signalLevel; set => signalLevel = value; }
        public double Mean { get => mean; set => mean = value; }
        public ImageSource Thumb { get => thumb; set => thumb = value; }
        public string Name { get => name; set => name = value; }
        public Histograms Histograms { get => histograms; set => histograms = value; }
        public OpenCvSharp.Point MaxPoint { get => maxPoint; set => maxPoint = value; }
        public OpenCvSharp.Point MinPoint { get => minPoint; set => minPoint = value; }


        public CommonImage()
        {

        }
        public CommonImage(MatImage matImage)
        {
            this.Mat = matImage.Mat;
            this.Name = matImage.Name;
            this.UserData = matImage.UserData;
            this.DateTime = matImage.DateTime;
        }


        /// <summary>
        /// 计算直方
        /// </summary>
        /// <returns></returns>
        public Histograms CalHist(double step)
        {
            if (Mat == null) return null;
            var type = Mat.Type();

            if(type!= MatType.CV_8U && type!=MatType.CV_16U&&type!= MatType.CV_32F)
            {
                return null;
            }

            var hist = new Histograms() { Step = step };
            int histSize =(int)( Math.Floor(Max / step) + 1);
            hist.End = (histSize) * step;
            Rangef rangef = new Rangef((float)hist.Start, (float)hist.End);
            var mat = new Mat();
            Cv2.CalcHist(new Mat[] { Mat }, new int[] { 0 }, null, mat, 1, new int[] { histSize }, new Rangef[] { rangef });
            hist.Frequencies = new double[histSize];
            for (int i = 0; i < histSize; i++)
            {
                hist.Frequencies[i] = mat.At<float>(i);
            }
            double max, min;//求最大频数
            Cv2.MinMaxIdx(mat, out min, out max);
            hist.MaxFrequency = max;
            this.histograms = hist;
            return hist;

        }

        public void CalMaxMin(Mat mask=null)
        {
            this.Mat.MinMaxLoc(out min, out max, out minPoint, out maxPoint, mask);
            this.mean = this.Mat.Mean(mask)[0];
        }

        public void CreateThumb()
        {
            if (Mat == null) { return; }

            int height = Mat.Height;
            int width = Mat.Width;
            int w = 120, h = 120;
            if (height < width)
            {
                h = (int)(((double)w * height) / width);
            }
            else
            {
                w = (int)(((double)h * width) / height);
            }

            Mat thumbMat =Mat.Resize(new Size(w, h));
            this.thumb = ImageSourceHelper.Mat2ImageSource(thumbMat, this.depth);
        }

        /// <summary>
        /// 计算信号强度,仅对图像原始数据有效
        /// </summary>
        public void CalSignalLevel()
        {
            this.SignalLevel =Math.Min(1, this.max / Math.Pow(2, this.depth));
        }

        public void Process()
        {
            this.CalMaxMin();
            this.CalSignalLevel();
            this.CalHist(100);
            this.CreateThumb();
        }

    }

    public class Histograms
    {
        double start = 0;
        double end = 0;
        double step = 1;

        double[] frequencies;
        double maxFrequency;
        public double Start { get => start; set => start = value; }
        public double End { get => end; set => end = value; }
        public double Step { get => step; set => step = value; }
        public double[] Frequencies { get => frequencies; set => frequencies = value; }
        public double MaxFrequency { get => maxFrequency; set => maxFrequency = value; }
    }



}
