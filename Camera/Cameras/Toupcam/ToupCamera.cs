using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ncer.Camera;
using ToupTek;

namespace Ncer.Camera
{

    public enum ToupCamera_Status
    {
        Toup_Status_ConnectionError = -3,
        Toup_Status_DataError = -2,
        Toup_Status_PatternError = -1,
        Toup_Status_Idle = 0,
        Toup_Status_Exposing = 1,
        Toup_Status_ImagingActive = 2,
        Toup_Status_ImageReady = 3,
        Toup_Status_Flushing = 4,
        Toup_Status_WaitingOnTrigger = 5,
        Toup_Status_WaitingOnSnap = 6
    }

    public class ToupCamera : Camera
    {
        private ToupCam camera;

        private bool stillSupported;
        private List<Resolution> previewResolutions = new List<Resolution>();
        private List<Resolution> stillResolutions = new List<Resolution>();
        private CameraMode cameraMode;
        private PixelSize pixelSize;
        private bool isStarted;
        private double expoTime=10;
        private double minExpo = 1;
        private double maxExpo = 10000;
        private Frame frame;
        private bool isPreview;
        private ToupCamera_Status status = ToupCamera_Status.Toup_Status_Idle;

        public override bool DefaultHFlip { get; set; } = false;
        public override bool DefaultVFlip { get; set; } = true;

        /// <summary>
        /// ms
        /// </summary>
        public override double MinExposure => this.minExpo/1000;
        /// <summary>
        /// ms
        /// </summary>
        public override double MaxExposure => this.maxExpo/1000;
        /// <summary>
        /// 曝光时间 ms
        /// </summary>
        public override double ExposureTime
        {
            get
            {
                if (camera == null) return double.NaN;
                uint time = 0;
                var ret = camera.get_ExpoTime(out time);
                if (!ret)
                {
                    return double.NaN;
                }
                return (double)time / 1000.0;
            }

            set
            {
                if (camera == null) return;
                bool ret=camera.put_ExpoTime((uint)(value * 1000));
                if (ret)
                {
                    this.expoTime = value;
                }
            }
        }
        /// <summary>
        /// 像素尺寸
        /// </summary>
        public override PixelSize PixelSize { get => this.getPixelSize(); protected set => this.pixelSize=value; }
        /// <summary>
        /// 是否已经启动
        /// </summary>
        public override bool IsStarted { get => this.isStarted; protected set => this.isStarted=value; }
        /// <summary>
        /// 是否支持抓拍
        /// </summary>
        public bool StillSupported { get => stillSupported; set => stillSupported = value; }

        #region  event

        public override event CameraEventHandler OnCameraEvent;
        public override event CameraPreviewFrameHandler OnCameraPreviewEvent;
        public override event CameraChangeHandler OnCameraStateChanged;
        #endregion
        #region private 
        private PixelSize getPixelSize()
        {
            if (camera == null) return null;
            float pW, pH;
            uint index;
            camera.get_eSize(out index);
            var ret = camera.get_PixelSize(index, out pW, out pH);
            this.pixelSize= new PixelSize(pW, pH);
            return this.pixelSize;
        }


        /// <summary>
        /// 尽快返回 运行在相机内部线程 不要在此处改变相机运行状态
        /// </summary>
        /// <param name="ev"></param>
        private void DelegateOnEventCallback(ToupCam.eEVENT ev)
        {
            switch (ev)
            {
                case ToupCam.eEVENT.EVENT_EXPOSURE:
                    OnExposureChanged();
                    break;
                case ToupCam.eEVENT.EVENT_TEMPTINT:
                    break;
                case ToupCam.eEVENT.EVENT_CHROME:
                    break;
                case ToupCam.eEVENT.EVENT_IMAGE:
                    this.OnPireviewImage();
                    break;
                case ToupCam.eEVENT.EVENT_STILLIMAGE:
                    this.OnStillImage();
                    break;
                case ToupCam.eEVENT.EVENT_WBGAIN:
                    break;
                case ToupCam.eEVENT.EVENT_TRIGGERFAIL:
                    break;
                case ToupCam.eEVENT.EVENT_ERROR:
                    break;
                case ToupCam.eEVENT.EVENT_DISCONNECTED:
                    OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.ConnectionError });
                    break;
                case ToupCam.eEVENT.EVENT_TIMEOUT:
                    break;
                default:
                    break;
            }
        }

        private void OnPireviewImage()
        {
            System.Console.WriteLine("EVENT_PRE_IMAGE");
            try
            {
                Frame frame = this.frame;
                uint w, h;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                camera.PullImage(frame.Data, 16, out w, out h);
                frame.ExposureTime = this.expoTime;
                frame.Time = DateTime.Now;
                stopwatch.Stop();
                //System.Console.WriteLine("pullimage:" + stopwatch.ElapsedMilliseconds);

                if (status == ToupCamera_Status.Toup_Status_Exposing)
                {
                    status = ToupCamera_Status.Toup_Status_ImageReady;
                    return;
                }

                if (!this.isPreview)
                {
                    return;
                }
                this.OnCameraPreviewEvent(this, frame);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("OnPireviewImage:"+ex);
            }
            System.Console.WriteLine("EVENT_PRE_IMAGE_FINISH");


        }

        private void OnStillImage()
        {
            System.Console.WriteLine("EVENT_STILLIMAGE");
            this.status = ToupCamera_Status.Toup_Status_ImageReady;
        }

        private void OnExposureChanged()
        {
            camera.get_ExpoTime(out var expo);
            System.Console.WriteLine("EVENT_EXPOSURE:"+expo);
        }



        #endregion
        #region 生命周期与运行状态
        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public override bool Init()
        {
            bool r = false;
            try
            {
                camera = new ToupCam();
                var instances = ToupCam.Enum();
                if (instances.Length <= 0)
                {
                    throw new Exception("No devices");
                }
                ToupCam.Instance instance = instances[0];
                bool result = camera.Open(instance.id);
                if (!result)
                {
                    throw new Exception("Open camera error.");
                }
                int w, h;

                //获取相机信息
                this.CameraModel = instance.displayname;
                this.CameraSerialNumber = camera.SerialNumber;
                //setting
                this.ExposureMode = ExposureMode.Manual;
                this.CameraStatus = CameraStatus.Idle;
                result = camera.put_Option(ToupCam.eOPTION.OPTION_RAW, 1);
                if (!result)
                {
                    throw new Exception("设置raw模式失败");
                }
                result = camera.put_AutoExpoEnable(false);
                if (!result)
                {
                    throw new Exception("设置手动曝光模式失败");
                }
                result = camera.put_Option(ToupCam.eOPTION.OPTION_BITDEPTH, 1);
                if (!result)
                {
                    throw new Exception("设置最高位深度失败");
                }
                if (camera.put_HFlip(DefaultHFlip) == false || camera.put_VFlip(DefaultVFlip) == false)
                {
                    throw new Exception("设置默认翻转失败");
                }

                //still
                this.stillResolutions.Clear();
                var stillResoNum = camera.StillResolutionNumber;
                this.StillSupported = camera.StillResolutionNumber > 0;
                if (stillSupported)
                {
                    for (uint i = 0; i < stillResoNum; i++)
                    {
                        var ret = camera.get_StillResolution(i, out w, out h);
                        if (ret)
                        {
                            this.stillResolutions.Add(new Resolution(w, h));
                        }
                    }
                }
                //preview分辨率 即相机的分辨率
                this.previewResolutions.Clear();
                var resoNum = camera.ResolutionNumber;
                for (uint i = 0; i < resoNum; i++)
                {
                    var ret = camera.get_Resolution(i, out w, out h);
                    if (ret)
                    {
                        this.previewResolutions.Add(new Resolution(w, h));
                    }
                }
                uint resoIndex;
                camera.get_eSize(out resoIndex);
                this.resolution = previewResolutions[(int)resoIndex];
                //pixel format
                this.PixelFormat = PixelFormat.Mono8;
                if ((instance.model.flag & ToupCam.eFLAG.FLAG_BITDEPTH10) == ToupCam.eFLAG.FLAG_BITDEPTH10)
                {
                    this.PixelFormat = PixelFormat.Mono10;
                }
                else if ((instance.model.flag & ToupCam.eFLAG.FLAG_BITDEPTH12) == ToupCam.eFLAG.FLAG_BITDEPTH12)
                {
                    this.PixelFormat = PixelFormat.Mono12;
                }
                else if ((instance.model.flag & ToupCam.eFLAG.FLAG_BITDEPTH14) == ToupCam.eFLAG.FLAG_BITDEPTH14)
                {
                    this.PixelFormat = PixelFormat.Mono14;
                }
                else if ((instance.model.flag & ToupCam.eFLAG.FLAG_BITDEPTH16) == ToupCam.eFLAG.FLAG_BITDEPTH16)
                {
                    this.PixelFormat = PixelFormat.Mono16;
                }
                //expo info
                uint min, max, def;
                var retExpoRange = camera.get_ExpTimeRange(out min, out max, out def);
                this.minExpo = min;
                this.maxExpo = max;
                this.expoTime = min;


                r = true;
            }
            catch (Exception ex)
            {
                r = false;
                //camera = null;
                this.Close();
            }
            finally
            {

            }
            return r;
        }

        public override bool Reset()
        {
            try
            {
                var ret = this.Close();

                ret = this.Init();
                return ret;

            }
            catch (Exception ex)
            {
                return false;
            }
        }
        /// <summary>
        /// 设置相机模式
        /// </summary>
        /// <param name="cameraMode"></param>
        /// <returns></returns>
        public override bool SetCameraMode(CameraMode cameraMode)
        {
            this.cameraMode = cameraMode;
            return true;
        }
        /// <summary>
        /// 关闭相机
        /// </summary>
        /// <returns></returns>
        public override bool Close()
        {
            try
            {
                if (camera != null)
                {
                    camera.Close();
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                camera = null;
                this.IsStarted = false;
            }
            return true;
        }
        /// <summary>
        /// 查询相机模式
        /// </summary>
        /// <returns></returns>
        public override CameraMode GetCameraMode()
        {
            return this.cameraMode;
        }
        /// <summary>
        /// 启动相机
        /// </summary>
        /// <returns></returns>
        public override bool Start()
        {
            if (camera == null) return false;
            try
            {
                var ret = camera.StartPullModeWithCallback(new ToupCam.DelegateEventCallback(this.DelegateOnEventCallback));
                this.IsStarted = ret;
                this.CameraStatus = CameraStatus.Idle;
                status = ToupCamera_Status.Toup_Status_Idle;

                //分配缓存帧
                this.frame = new Frame(this.previewResolution.width, this.previewResolution.height, this.PixelFormat);
                this.frame.Allocate();
                return ret;
            }
            catch (Exception ex)
            {
                this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.CameraError, Message = "fail to start" });
                return false;
            }
            return true;
        }
        /// <summary>
        /// 停止相机
        /// </summary>
        /// <returns></returns>
        public override bool Stop()
        {
            if (camera == null)
            {
                IsStarted = false;
                this.CameraStatus = CameraStatus.Idle;

                return false;
            }
            try
            {
                var ret = camera.Stop();
                if (ret)
                {
                    IsStarted = false;
                }
                return ret;
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        #endregion
        #region 参数获取和配置
        public override bool GetHFlip(out bool flip)
        {
            var ret = camera.get_HFlip(out var flip_);
            if (ret)
            {
                flip = DefaultHFlip ? !flip_ : flip_;
                return true;
            }
            else
            {
                flip = false;
                return false;
            }
        }

        public override bool SetHFlip(bool flip)
        {
            return camera.put_HFlip(DefaultHFlip? !flip:flip);
        }

        public override bool GetVFlip(out bool flip)
        {
            var ret = camera.get_VFlip(out var flip_);
            if (ret)
            {
                flip = DefaultVFlip ? !flip_ : flip_;
                return true;
            }
            else
            {
                flip = false;
                return false;
            }
        }

        public override bool SetVFlip(bool flip)
        {
            return camera.put_VFlip(DefaultVFlip ? !flip : flip);
        }

        public override bool SetGain(double gain)
        {
            return camera.put_ExpoAGain((ushort)(gain * 100));
        }
        public override bool GetGain(ref double gain)
        {
            ushort _gain = 0;
            var ret=camera.get_ExpoAGain(out _gain);
            if (ret)
            {
                gain= (double)_gain;
                return true;
            }
            else
            {
                return false;
            }
        }


        public override Resolution GetPreviewResolution()
        {
            return previewResolution;
        }


        /// <summary>
        /// 设置预览分辨率
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public override bool SetPreviewResolution(Resolution resolution)
        {
            if (IsStarted)
            {
                return false;
            }
            try
            {
                var result = camera.put_Size(resolution.width, resolution.height);
                if (result)
                {
                    this.previewResolution = resolution;
                    return true;
                }
                throw new Exception("Fail to set resolution");
            }
            catch (Exception ex)
            {
                this.OnCameraEvent?.Invoke(this, new CameraEventArgs()
                {
                    CameraEvent = CameraEvent.OperateError,
                    Message = "Fail to set resolution"
                });
                return false;
            }

        }
        /// <summary>
        /// 获取预览分辨率列表
        /// </summary>
        /// <returns></returns>
        public override List<Resolution> GetPreviewResolutions()
        {
            if (camera == null)
            {
                return null;
            }
            return this.previewResolutions;
        }
        /// <summary>
        /// 获取抓拍分辨率
        /// </summary>
        /// <returns></returns>
        public override Resolution GetResolution()
        {
            if (this.camera == null) return null;
            return this.resolution;
        }
        /// <summary>
        /// 获取抓拍分辨率列表
        /// </summary>
        /// <returns></returns>
        public override List<Resolution> GetResolutions()
        {
            if (this.camera == null) return null;
            return stillResolutions;
        }

        /// <summary>
        /// 设置分辨率 设置的是单张和抓拍模式的分辨率
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public override bool SetResolution(Resolution resolution)
        {
            this.resolution = resolution;
            return true;
        }

        #endregion
        #region 图像获取
        public override bool StartPreview()
        {
            this.isPreview = true;
            if (this.CameraStatus != CameraStatus.Idle)
            {
                throw new Exception("camera is unready");
            }
            this.CameraStatus = CameraStatus.Busy;
            this.cameraMode = CameraMode.Preview;
            return true;
        }
        public override bool StopPreview()
        {

            this.cameraMode = CameraMode.Single;
            this.CameraStatus = CameraStatus.Idle;
            this.isPreview = false;
            return true;
        }

        public override Frame TakeImage()
        {
            try
            {
                if (this.camera == null)
                {
                    throw new Exception("No camera connnection");
                }

                int index = -1;
                for (int i = 0; i < this.stillResolutions.Count; i++)
                {
                    if (this.resolution.Equals(this.stillResolutions[i]))
                    {
                        index = i;
                    }
                }
                if (index < 0)
                {
                    throw new Exception("wrong resolution!");
                }
                var expo = this.ExposureTime;
                var waitTime = expo*2 + 5000;
                int interval = Math.Max(1, Math.Min(100, (int)(expo / 10)));
                Stopwatch sw = new Stopwatch();
                sw.Start();
                status = ToupCamera_Status.Toup_Status_Exposing;
                //bool result = camera.Snap((uint)index);//抓取
                DateTime startTime = DateTime.Now;
                if (expo > 500)
                {
                    System.Threading.Thread.Sleep((int)(expo - 500));
                }
                while (DateTime.Now.Subtract(startTime).TotalMilliseconds < waitTime)
                {
                    if (status ==ToupCamera_Status.Toup_Status_ImageReady)
                    {

                        //System.Console.WriteLine("new frame:" + sw.ElapsedMilliseconds + "ms");
                        //Frame frame = new Frame(this.resolution.width,resolution.height,this.PixelFormat);
                        //frame.ExposureTime = expo;
                        //frame.Allocate();
                        //sw.Restart();
                        //uint w, h;
                        //camera.PullStillImage(frame.Data, 16, out w, out h);
                        //System.Console.WriteLine("pull stilll:" + sw.ElapsedMilliseconds + "ms");
                        Frame frame = (Frame)this.frame.Clone();
                        status = ToupCamera_Status.Toup_Status_WaitingOnSnap;
                        return frame;
                    }
                    System.Console.WriteLine("wait frame");
                    System.Threading.Thread.Sleep(interval);
                }
                throw new Exception("wait frame timeout");
                System.Console.WriteLine("wait frame timeout");
                this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.ImagingError });
                return null;

            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
                this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.ImagingError, Message = ex.Message });
                this.Close();
            }
            return null;
        }

        public override async Task<Frame> TakeImageAsync()
        {
            var ret = await Task.Run<Frame>(() =>
            {
                var frame = this.TakeImage();

                return frame;
            }).ConfigureAwait(false);
            return ret;
        }

        #endregion

    }
}
