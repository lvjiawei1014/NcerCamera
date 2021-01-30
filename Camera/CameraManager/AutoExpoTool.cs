using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Ncer.Camera;
using System.Windows;
using System.Runtime.CompilerServices;

namespace Ncer.Camera
{
    /// <summary>
    /// 自动曝光状态
    /// </summary>
    public enum ExpoState
    {
        None,
        Adjusting,
        Suitable,
        Overflow,
        TooLow,
    }
    public class AutoExpoController
    {
        private double maxExpo = 2000;
        private double minExpo = 1;
        private double maxGain = 60;

        private double maxLevel = 0.95;
        private double minLevel = 0.70;
        private double targetLevel = 0.85;
        private OpenCvSharp.Rect expoRegion;
        private SignalLevelHelper signalLevelHelper=new SignalLevelHelper();


        public bool hasEstimateValue = false;
        public bool expand = false;

        public double MaxExpo { get => maxExpo; set => maxExpo = value; }
        public double MinExpo { get => minExpo; set => minExpo = value; }
        public double MaxLevel { get => maxLevel; set => maxLevel = value; }
        public double MinLevel { get => minLevel; set => minLevel = value; }
        public double TargetLevel { get => targetLevel; set => targetLevel = value; }
        public OpenCvSharp.Rect ExpoRegion { get => expoRegion; set => expoRegion = value; }
        public SignalLevelHelper SignalLevelHelper { get => signalLevelHelper; set => signalLevelHelper = value; }
        /// <summary>
        /// 最大测试增益
        /// </summary>
        public double TestMaxGain { get => maxGain; set => maxGain = value; }
        /// <summary>
        /// 最大预览增益
        /// </summary>
        public double PreviewMaxGain { get; set; } = 60;

        public double GetNextValue(double lastExpo, FrameImage src, out bool finish,out bool fail)
        {
            double level = signalLevelHelper.ComputeSignalLevel(src);
            double next = 100;
            if (level >= MinLevel && level <= MaxLevel)
            {
                finish = true;
            }
            else
            {
                finish = false;
                if((level<minLevel && lastExpo == this.maxExpo )||( level>maxExpo &&lastExpo==this.minLevel))
                {
                    fail = true;
                    return lastExpo;
                }
            }
            if (hasEstimateValue)
            {
                if (level >= 0.99)
                {
                    next = 0.7 * lastExpo;
                }
                else
                {
                    next = (lastExpo * Math.Min(8, (TargetLevel / level)));
                }
            }
            else
            {
                if (level >= 0.99)
                {
                    next = lastExpo * 0.4;
                }
                else
                {
                    hasEstimateValue = true;
                    next = (lastExpo * Math.Min(8, (TargetLevel / level)));
                }
            }
            fail = false;
            return Math.Max(Math.Min(next, MaxExpo), MinExpo);

        }
        /// <summary>
        /// 更新 曝光时间 增益
        /// </summary>
        /// <param name="src"></param>
        /// <param name="gain"></param>
        /// <param name="finish"></param>
        /// <param name="fail"></param>
        /// <returns></returns>
        public ExpoState GetNextValue(FrameImage src,out double nextExpo,out double gain)
        {
            var lastExpo = src.Exposure;
            src.TryGetUserData<double>("gain", out var lastGain);
            double level = signalLevelHelper.ComputeSignalLevel(src);
            nextExpo = 100;

            //check 
            if (level >= MinLevel && level <= MaxLevel)//ok
            {
                nextExpo = lastExpo;
                gain = lastGain;
                return ExpoState.Suitable;
            }
            if ((level > maxExpo && lastExpo == this.minExpo))//to high
            {
                nextExpo = lastExpo;
                gain = lastGain;
                return ExpoState.Overflow;
            }
            if (expand)//一设置增益
            {
                if(level < MinLevel)//信号水平依旧不足
                {
                    if (lastGain >= TestMaxGain)
                    {
                        nextExpo = lastExpo;
                        gain = lastGain;
                        return ExpoState.TooLow;
                    }
                    nextExpo = lastExpo;
                    gain = Math.Min(TestMaxGain, lastGain * targetLevel / level);
                    return ExpoState.Adjusting;

                }
                else // 信号水平过大
                {
                    gain = Math.Max(1,lastGain * targetLevel / level);
                    if (gain == 1)
                    {
                        expand = false; // 取消增益
                    }
                    nextExpo = lastExpo;
                    return ExpoState.Adjusting;
                }
            }
            else//未设置增益
            {
                if(level<minLevel&& lastExpo >= this.maxExpo)//需要设置增益
                {
                    expand = true;
                    gain = Math.Min(TestMaxGain, lastGain * targetLevel / level);
                    nextExpo = lastExpo;
                    return ExpoState.Adjusting;
                }
                else// 不需要设置增益
                {
                    //continue adjust expo
                    if (hasEstimateValue)
                    {
                        if (level >= 0.99)
                        {
                            nextExpo = 0.7 * lastExpo;
                        }
                        else
                        {
                            nextExpo = (lastExpo * Math.Min(8, (TargetLevel / level)));
                        }
                    }
                    else
                    {
                        if (level >= 0.99)
                        {
                            nextExpo = lastExpo * 0.4;
                        }
                        else
                        {
                            hasEstimateValue = true;
                            nextExpo = (lastExpo * Math.Min(8, (TargetLevel / level)));
                        }
                    }

                    nextExpo = Math.Max(Math.Min(nextExpo, MaxExpo), MinExpo);
                    gain = lastGain;
                    return ExpoState.Adjusting;
                }
            }



        }
        public double GetNextValue(double lastValue, double level)
        {
            double next = 100;
            if (hasEstimateValue)
            {
                next = (lastValue * (TargetLevel / level));
            }
            else
            {
                if (level > MaxLevel)
                {
                    next = lastValue / 2;
                }
                else
                {
                    hasEstimateValue = true;
                    next = (lastValue * (TargetLevel / level));
                }
            }

            return Math.Max(Math.Min(next, MaxExpo), MinExpo);

        }



        public void Reset()
        {
            hasEstimateValue = false;
            expand = false;

        }


        public static double VedioAutoExpo(Mat src, OpenCvSharp.Rect rect,double maxValue,double targetMaxLevel,double targetMinLevel,double lastValue,double max,double min)
        {
            var l = src[rect].Mean()[0] / maxValue;
            if (l > 0.99)
            {
                return lastValue * 0.4;
            }
            double scale = 1;
            if (l < targetMinLevel)
            {
                scale = targetMinLevel / l;
            }else if (l > targetMaxLevel)
            {
                scale = targetMaxLevel / l;
            }
            else
            {
                //scale = (targetMaxLevel + targetMinLevel) * 0.5 / l;
                scale = 1;
            }
            scale = Math.Min(Math.Max(scale, 0.1), 10);
            var t = scale * lastValue;
            t = Math.Min(Math.Max(min, t), max);
            return t;
        }


        public void VedioAutoExpo(FrameImage src,  OpenCvSharp.Rect rect, double maxValue, double targetMaxLevel, double targetMinLevel, out double nextExpo, out double gain)
        {
            
            double scale = 1;
            src.TryGetUserData<double>("gain", out var lastGain);
            var lastExpo = src.Exposure;


            var tmpRect = new OpenCvSharp.Rect();
            var left = Math.Min(Math.Max(0, rect.X), src.ImageWidth);
            var top = Math.Min(Math.Max(0, rect.Y), src.ImageHeight);
            var right = Math.Min(Math.Max(0, rect.X + rect.Width), src.ImageWidth);
            var bottom = Math.Min(Math.Max(0, rect.Y+rect.Height), src.ImageHeight);
            if(right>left && top < bottom)
            {
                tmpRect = new OpenCvSharp.Rect(left, top, right - left, bottom - top);
            }
            else
            {
                tmpRect = new OpenCvSharp.Rect(0, 0, src.ImageWidth, src.ImageHeight);

            }


            var l = src.Mat[tmpRect].Mean()[0] / maxValue;
            if (l > 0.99)
            {
                scale=0.4;
            }
            if (l < targetMinLevel)
            {
                scale = targetMinLevel / l;
            }
            else if (l > targetMaxLevel)
            {
                scale = targetMaxLevel / l;
            }
            else
            {
                scale = 1;
            }
            scale = Math.Min(Math.Max(scale, 0.1), 10);
            var tmp = scale * lastGain * lastExpo;
            if ( tmp< 100)
            {
                gain = 1;
                nextExpo = Math.Max(minExpo, tmp);
            }
            else if(tmp < Math.Min(100*PreviewMaxGain, 30000))
            {
                gain = tmp / 100;
                nextExpo = 100;
            }
            else if(tmp< Math.Min(200 * PreviewMaxGain, 60000))
            {
                gain = tmp / 200;
                nextExpo = 200;
            }
            else
            {
                gain = PreviewMaxGain;
                nextExpo = Math.Min(MaxExpo, Math.Max(minExpo, tmp / PreviewMaxGain));
            }
        }

    }
}
