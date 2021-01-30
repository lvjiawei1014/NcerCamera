using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ncer.Camera
{
    public abstract class Camera
    {
        public delegate void CameraPreviewFrameHandler(object sender, Frame frame);

        public delegate void CameraChangeHandler(object sender, CameraStateArgs cameraStateArgs);

        public delegate void CameraEventHandler(object sender, CameraEventArgs cameraEventArgs);
        /// <summary>
        /// 相机事件
        /// </summary>
        public abstract event CameraEventHandler OnCameraEvent;
        /// <summary>
        /// 预览模式帧事件
        /// </summary>
        public abstract event CameraPreviewFrameHandler OnCameraPreviewEvent;

        public abstract event CameraChangeHandler OnCameraStateChanged;
        #region 属性
        /// <summary>
        /// 分辨率 单张 抓拍
        /// </summary>
        protected Resolution resolution;
        protected Resolution previewResolution;

        public virtual bool DefaultVFlip { get; set; }
        public virtual bool DefaultHFlip { get; set; }

        public abstract double MinExposure { get; }

        public abstract double MaxExposure { get; }

        public CameraStatus CameraStatus { get; protected set; }

        /// <summary>
        /// 曝光时间
        /// </summary>
        public abstract double ExposureTime { get; set; }
        /// <summary>
        /// 像素格式
        /// </summary>
        public PixelFormat PixelFormat { get; protected set; }
        /// <summary>
        /// 相机模式
        /// </summary>
        public ExposureMode ExposureMode { get; set; }
        /// <summary>
        /// 像素尺寸
        /// </summary>
        public abstract PixelSize PixelSize { get; protected set; }
        /// <summary>
        /// 相机型号
        /// </summary>
        public string CameraModel { get; set; } = "Unkown";
        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = "";
        /// <summary>
        /// 像素位深度
        /// </summary>
        public int Depth
        {
            get
            {
                return (int)(this.PixelFormat) & 0xff;
            }
        }
        /// <summary>
        /// 相机是否启动
        /// </summary>
        public abstract bool IsStarted { get; protected set; }

        public virtual double Temperature { get; } = double.NaN;

        public virtual double TargetTemperature { get; set; } = double.NaN;
        public virtual double BackoffTemperature { get; set; } = double.NaN;


        #endregion
        #region 生命周期 和状态
        /// <summary>
        /// 初始化相机连接
        /// </summary>
        public abstract bool Init();
        /// <summary>
        /// 关闭相机连接
        /// </summary>
        public abstract bool Close();

        public abstract bool Reset();

        public abstract bool Start();

        public abstract bool Stop();


        #endregion
        #region 图像获取
        /// <summary>
        /// 获取一帧图像
        /// </summary>
        /// <returns></returns>
        public abstract Task<Frame> TakeImageAsync();

        public abstract Frame TakeImage();

        public abstract bool StartPreview();

        public abstract bool StopPreview();

        #endregion
        #region 参数获取和设置
        /// <summary>
        /// 设置相机模式
        /// </summary>
        /// <param name="cameraMode"></param>
        public abstract bool SetCameraMode(CameraMode cameraMode);
        /// <summary>
        /// 获取相机模式
        /// </summary>
        /// <returns></returns>
        public abstract CameraMode GetCameraMode();
        /// <summary>
        /// 获取相机支持的分辨率
        /// </summary>
        /// <returns></returns>
        public abstract List<Resolution> GetResolutions();
        public abstract List<Resolution> GetPreviewResolutions();
        /// <summary>
        /// 获取相机当前分辨率
        /// </summary>
        /// <returns></returns>
        public abstract Resolution GetResolution();

        public abstract Resolution GetPreviewResolution();
        /// <summary>
        /// 设置相机分辨率
        /// </summary>
        /// <returns></returns>
        public abstract bool SetResolution(Resolution resolution);
        public abstract bool SetPreviewResolution(Resolution resolution);

        

        public virtual bool SetGain(double gain)
        {
            return true;
        }

        public virtual bool GetGain(ref double gain)
        {
            return true;
        }

        public virtual bool SetVFlip(bool flip)
        {
            return false;
        }

        public virtual bool GetVFlip(out bool flip)
        {
            flip = false;
            return false;
        }

        public virtual bool SetHFlip(bool flip)
        {
            return false;
        }

        public virtual bool GetHFlip(out bool flip)
        {
            flip = false;
            return false;
        }


        #endregion
        #region Cooler
        /// <summary>
        /// 获取当前温度
        /// </summary>
        /// <returns></returns>
        public virtual double GetTemperature()
        {
            return double.NaN;
        }


        #endregion
        #region Cooler

        public virtual bool GetCoolerEnable()
        {
            return true;
        }

        #endregion
        #region Cooler

        public virtual void SetCoolerEnable(bool enable)
        {
            

        }


        #endregion
    }



    public struct CameraStateArgs
    {
        public string Message { get; set; }
    }

    public struct CameraEventArgs
    {
        public CameraEvent CameraEvent { get; set; }
        public string Message { get; set; }
    }

    public class Frame:ICloneable
    {
        private double exposureTime;
        private int width;
        private int height;
        private PixelFormat pixelFormat;
        private IntPtr data=IntPtr.Zero;
        public PixelFormat PixelFormat { get => pixelFormat; set => pixelFormat = value; }
        public int Width { get => width; set => width = value; }
        public int Height { get => height; set => height = value; }
        public double ExposureTime { get => exposureTime; set => exposureTime = value; }
        public IntPtr Data { get => data; set => data = value; }
        public DateTime Time { get; set; }
        public Frame()
        {

        }

        public Frame(int width, int height, PixelFormat pixelFormat)
        {
            this.width = width;
            this.height = height;
            this.pixelFormat = pixelFormat;
        }



        ~Frame()
        {
            this.Release();
        }

        public void Release()
        {
            if (data != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(data);
                data = IntPtr.Zero;
            }
        }

        public void Allocate()
        {
            if (data != IntPtr.Zero)
            {
                this.Release();
            }
            int pixelBytes = 1;
            switch (this.pixelFormat)
            {
                case PixelFormat.Mono8:
                    pixelBytes = 1;break;
                case PixelFormat.Mono10:
                case PixelFormat.Mono12:
                case PixelFormat.Mono14:
                case PixelFormat.Mono16:
                    pixelBytes = 2;
                    break;
                case PixelFormat.RGB8:
                    pixelBytes = 3;
                    break;
                default:
                    break;
            }
            data=Marshal.AllocHGlobal(this.width * this.height * pixelBytes);
        }

        public object Clone()
        {
            Frame frame = new Frame();
            frame.exposureTime = this.exposureTime;
            frame.Height = this.Height;
            frame.width = this.width;
            frame.pixelFormat = this.pixelFormat;
            int pixelBytes = 1;
            switch (this.pixelFormat)
            {
                case PixelFormat.Mono8:
                    pixelBytes = 1; break;
                case PixelFormat.Mono10:
                case PixelFormat.Mono12:
                case PixelFormat.Mono14:
                case PixelFormat.Mono16:
                    pixelBytes = 2;
                    break;
                case PixelFormat.RGB8:
                    pixelBytes = 3;
                    break;
                default:
                    break;
            }
            frame.data = Marshal.AllocHGlobal(this.width * this.height * pixelBytes);
            SystemInvoke.MemCopy(frame.data, this.data, this.width * this.height * pixelBytes);
            return frame;
        }


    }

    /// <summary>
    /// 相机事件枚举
    /// </summary>
    public enum CameraEvent
    {
        
        ConnectionError = 1,
        ImagingError=2,
        CameraError=3,
        PreviewFinish=4,
        PreviewStart=5,
        PreviewError=6,
        SingleFinish=7,
        SingleStart=8,
        SingleError=9,
        OperateError=10,



    }
    /// <summary>
    /// 相机模式枚举
    /// </summary>
    public enum CameraMode
    {
        Unknow=-1,
        Single=0,
        Continuous=1,
        Still=2,
        Preview =3,

    }

    public enum CameraStatus
    {
        Disconnected=0,
        Idle=1,
        Busy=2,
        Error=5
    }

    /// <summary>
    /// 分辨率结构体
    /// </summary>
    public class Resolution
    {
        /// <summary>
        /// 水平像素
        /// </summary>
        public int width;
        /// <summary>
        /// 竖直像素
        /// </summary>
        public int height;

        public override string ToString()
        {
            return this.width.ToString() + "*" + this.height.ToString();
        }
        
        public Resolution(int width,int height)
        {
            this.width = width;
            this.height = height;
        }

        public override bool Equals(object obj)
        {
            if(obj is Resolution)
            {
                var reso = obj as Resolution;
                if(this.width==reso.width && this.height == reso.height)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 像素尺寸
    /// </summary>
    public class PixelSize
    {
        public double pixelSizeX;
        public double pixelSizeY;

        public PixelSize(double PixelSizeX,double PixelSizeY)
        {
            this.pixelSizeX = PixelSizeX;
            this.pixelSizeY = PixelSizeY;
        }
    }


    /// <summary>
    /// 像素格式 
    /// </summary>
    public enum PixelFormat
    {
        Mono8=0x0108,
        Mono10=0x020A,
        Mono12=0x030C,
        Mono14=0x040E,
        Mono16=0x0510,

        RGB8=0x010118,
    }

    public struct PixelFormatStruct
    {
        public string Name { get; set; }
        public int Depth { get; set; }
        public int Channels { get; set; }
        public int PixelBytes { get; set; }
    }


    public static class PixelFormatStructs
    {
        public static PixelFormatStruct Mono8
        {
            get => new PixelFormatStruct() { Name = "Mono8", Depth = 8, Channels = 1, PixelBytes = 1 };
        }
    }



    /// <summary>
    /// 曝光模式
    /// </summary>
    public enum ExposureMode
    {
        Auto=0,
        Manual=1,
    }

    /// <summary>
    /// 位深度
    /// </summary>
    public enum BitDepth
    {
        Bit8=8,
        Bit10=10,
        Bit12=12,
        Bit14=14,
        Bit16=16,
    }


}
