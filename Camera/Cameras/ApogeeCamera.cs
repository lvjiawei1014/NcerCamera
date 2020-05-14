using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APOGEELib;
using System.Threading;

namespace Ncer.Camera
{
    public class ApogeeCamera : Camera
    {
        public override event CameraEventHandler OnCameraEvent;
        public override event CameraPreviewFrameHandler OnCameraPreviewEvent;

        #region 成员
        private Camera2Class camera;
        private CameraMode cameraMode;
        private Frame frame;

        private Thread imagingThread;
        private bool isStart;
        private bool isStoppingPreview;
        private double expoTime;


        #endregion
        #region 属性
        public Camera2Class Camera { get => camera; set => camera = value; }
        public override PixelSize PixelSize { get; protected set; }
        public Frame Frame { get => frame; set => frame = value; }

        public override double MinExposure
        {
            get
            {
                try
                {
                    return camera.MinExposure;
                }
                catch (Exception)
                {
                    return -1;
                }
            }
        }

        public override double MaxExposure
        {
            get
            {
                try
                {
                    return camera.MaxExposure;
                }
                catch (Exception)
                {
                    return -1;
                }
            }
        }

        public override bool IsStarted { get => isStart; protected set => isStart = value; }

        public override double ExposureTime
        {
            get => expoTime; set
            {
                if (value < this.MinExposure)
                {
                    expoTime = this.MinExposure;
                    this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.OperateError, Message = "Exposure time is smaller than the min value" });
                }
                else if(value>this.MaxExposure)
                {
                    expoTime = this.MaxExposure;
                    this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.OperateError, Message = "Exposure time is bigger than the max value" });
                }
                else
                {
                    this.expoTime = value;
                }
            }
        }

        #endregion
        #region 状态管理

        /// <summary>
        /// 初始化相机
        /// </summary>
        public override bool Init()
        {
            try
            {
                //创建实例
                this.camera = new Camera2Class();
                //初始化连接
                this.camera.Init(Apn_Interface.Apn_Interface_USB, 0, 0, 0);
                this.ExposureMode = ExposureMode.Manual;
                this.PixelFormat = PixelFormat.Mono16;
                this.camera.CameraMode = Apn_CameraMode.Apn_CameraMode_Normal;
                this.CameraStatus = CameraStatus.Idle;
                //获取分辨率
                this.resolution = new Resolution(camera.ImagingColumns, camera.ImagingRows);
                //获取像素参数
                this.PixelSize = new PixelSize(camera.PixelSizeX, camera.PixelSizeY);
                //获取相机信息
                this.CameraModel = camera.CameraModel;
                this.CameraSerialNumber = camera.CameraSerialNumber;
                this.ExposureTime = camera.MinExposure;

                //分配缓存帧
                this.frame = new Frame(this.resolution.width, this.resolution.height, PixelFormat.Mono16);
                this.frame.Allocate();
            }
            catch (Exception)
            {
                this.OnCameraEvent?.Invoke(this, new CameraEventArgs()
                {
                    CameraEvent = CameraEvent.CameraError,
                    Message = "Init Error"
                });
                return false;
            }
            return true;
        }

        /// <summary>
        /// 关闭相机
        /// </summary>
        public override bool Close()
        {
            try
            {
                if (this.IsStarted)
                {
                    this.Stop();
                }
                if (camera == null)
                {
                    return false;
                }
                this.camera.Close();
            }
            catch (Exception)
            {
                this.OnCameraEvent?.Invoke(this, new CameraEventArgs()
                {
                    CameraEvent = CameraEvent.CameraError,
                    Message = "Close Error"
                });
                return false;
            }
            return true;
        }
        /// <summary>
        /// 重置相机 未完成
        /// </summary>
        public override bool Reset()
        {
            try
            {
                this.Stop();
                camera.ResetSystem();
                
            }
            catch (Exception)
            {

                this.OnCameraEvent?.Invoke(this, new CameraEventArgs()
                {
                    CameraEvent = CameraEvent.CameraError,
                    Message = "Reset Error"
                });
                return false;
            }
            return true;
        }

        #endregion
        #region 设定
        public override CameraMode GetCameraMode()
        {
            return this.cameraMode;
        }

        public override Resolution GetResolution()
        {
            var row = this.camera.ImagingRows;
            var col = this.camera.ImagingColumns;
            return new Resolution(col, row);
        }

        public override List<Resolution> GetResolutions()
        {
            try
            {
                return new List<Resolution>() { new Resolution(camera.ImagingColumns, camera.ImagingRows) };

            }
            catch (Exception)
            {
                return null;
            }
        }


        public override bool SetCameraMode(CameraMode cameraMode)
        {
            this.cameraMode = cameraMode;
            switch (cameraMode)
            {
                case CameraMode.Unknow:
                    break;
                case CameraMode.Single:
                    camera.CameraMode = Apn_CameraMode.Apn_CameraMode_Normal;
                    break;
                case CameraMode.Continuous:
                    camera.CameraMode = Apn_CameraMode.Apn_CameraMode_Normal;
                    break;
                case CameraMode.Preview:
                    break;
                default:
                    break;
            }
            return true;
        }

        public override bool SetResolution( Resolution resolution)
        {
            return true;
        }

        #endregion
        #region 图像获取

        private void ImaingTask()
        {
            while (true)
            {
                if (this.cameraMode == CameraMode.Preview)
                {
                    var frame = this.TakeImage();
                    this.OnCameraPreviewEvent?.Invoke(this, frame);
                }
                if (this.isStoppingPreview)
                {
                    this.CameraStatus = CameraStatus.Idle;
                    this.isStoppingPreview = false;
                    this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.PreviewFinish });
                }
                
                System.Threading.Thread.Sleep(1);
            }
            
        }

        private Frame NextFrame()
        {
            try
            {
                var expo = this.ExposureTime;
                int interval = Math.Max(1, Math.Min(100, (int)(expo / 10)));
                Stopwatch sw = new Stopwatch();
                sw.Start();
                this.camera.Expose(expo / 1000.0, false);//
                DateTime startTime = DateTime.Now;
                if (expo > 500)
                {
                    System.Threading.Thread.Sleep((int)(expo - 500));
                }
                while (DateTime.Now.Subtract(startTime).TotalMilliseconds < 2 * this.ExposureTime)
                {
                    if (camera.ImagingStatus == Apn_Status.Apn_Status_ImageReady)
                    {

                        System.Console.WriteLine("new frame:" + sw.ElapsedMilliseconds + "ms");
                        Frame frame = this.frame;
                        frame.ExposureTime = expo;
                        camera.GetImage(frame.Data.ToInt32());
                        return frame;
                    }
                    System.Console.WriteLine("wait frame");
                    System.Threading.Thread.Sleep(interval);
                }
                System.Console.WriteLine("wait frame timeout");
                this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.ImagingError });
                return null;
            }
            catch (Exception ex)
            {
                this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.ImagingError, Message = ex.Message });
                return null;
            }
        }

        public override async Task<Frame> TakeImageAsync()
        {
            var f=await Task.Run<Frame>(() =>
            {
                try
                {
                    this.cameraMode = CameraMode.Single;
                    var expo = this.ExposureTime;
                    int interval = Math.Max(1, Math.Min(100, (int)(expo / 10)));
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    this.camera.Expose(expo / 1000.0, false);//
                    DateTime startTime = DateTime.Now;
                    if (expo > 500)
                    {
                        System.Threading.Thread.Sleep((int)(expo - 500));
                    }
                    while (DateTime.Now.Subtract(startTime).TotalMilliseconds < 2 * this.ExposureTime)
                    {
                        if (camera.ImagingStatus == Apn_Status.Apn_Status_ImageReady)
                        {

                            System.Console.WriteLine("new frame:" + sw.ElapsedMilliseconds + "ms");
                            Frame frame = this.frame;
                            frame.ExposureTime = expo;
                            camera.GetImage(frame.Data.ToInt32());
                            return frame;
                        }
                        System.Console.WriteLine("wait frame");
                        System.Threading.Thread.Sleep(interval);
                    }
                    System.Console.WriteLine("wait frame timeout");
                    this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.ImagingError });
                    return null;
                }
                catch (Exception ex)
                {
                    this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.ImagingError, Message = ex.Message });
                    return null;
                }
            });
            return f;
        }


        public override Frame TakeImage()
        {
            try
            {
                var expo = this.ExposureTime;
                int interval = Math.Max(1, Math.Min(100, (int)(expo / 10)));
                Stopwatch sw = new Stopwatch();
                sw.Start();
                this.camera.Expose(expo / 1000.0, false);//
                DateTime startTime = DateTime.Now;
                if (expo > 500)
                {
                    System.Threading.Thread.Sleep((int)(expo - 500));
                }
                while (DateTime.Now.Subtract(startTime).TotalMilliseconds < 2 * this.ExposureTime)
                {
                    if (camera.ImagingStatus == Apn_Status.Apn_Status_ImageReady)
                    {

                        System.Console.WriteLine("new frame:"+sw.ElapsedMilliseconds+"ms");
                        Frame frame = this.frame;
                        frame.ExposureTime = expo;
                        camera.GetImage(frame.Data.ToInt32());
                        return frame;
                    }
                    System.Console.WriteLine("wait frame");
                    System.Threading.Thread.Sleep(interval);
                }
                System.Console.WriteLine("wait frame timeout");
                this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.ImagingError });
                return null;
            }
            catch (Exception ex)
            {
                this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.ImagingError, Message = ex.Message });
                return null;
            }

        }


        #endregion
        #region Cooler
        /// <summary>
        /// 获取当前温度
        /// </summary>
        /// <returns></returns>
        public double GetTemperature()
        {
            try
            {
                return camera.TempCCD;
            }
            catch (Exception)
            {
                return double.NaN;
            }
        }
        #endregion

        public override bool StartPreview()
        {
            try
            {
                if (this.CameraStatus != CameraStatus.Idle)
                {
                    throw new Exception("camera is unready");
                }
                this.CameraStatus = CameraStatus.Busy;
                this.cameraMode = CameraMode.Preview;

            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
            finally
            {

            }

            return true;
        }

        public override bool StopPreview()
        {
            try
            {
                if (this.cameraMode != CameraMode.Preview)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
            finally
            {
                this.cameraMode = CameraMode.Single;
                this.isStoppingPreview = true;
            }
            return true;
        }

        public override bool Start()
        {
            try
            {
                if (isStart)
                {
                    throw new Exception("Camera alreay started;");
                }
                imagingThread = new Thread(this.ImaingTask);
                imagingThread.IsBackground = false;
                imagingThread.Start();
                IsStarted = true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
                this.OnCameraEvent?.Invoke(this, new CameraEventArgs() { CameraEvent = CameraEvent.CameraError,
                    Message = "Fail to start:"+ex });
                isStart = false;
                return false;
            }
            return true;
            
        }

        public override bool Stop()
        {
            bool result = false;
            try
            {
                if (this.cameraMode == CameraMode.Preview)
                {
                    this.StopPreview();
                }
                imagingThread?.Abort();
                result = true;
                
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
            finally
            {
                imagingThread = null;
                isStart = false;
            }
            return result;
        }

        public override Resolution GetPreviewResolution()
        {
            throw new NotImplementedException();
        }

        public override bool SetPreviewResolution(Resolution resolution)
        {
            throw new NotImplementedException();
        }

        public override List<Resolution> GetPreviewResolutions()
        {
            throw new NotImplementedException();
        }
    }
}
