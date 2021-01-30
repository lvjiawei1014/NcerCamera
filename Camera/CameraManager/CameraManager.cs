using OpenCvSharp;
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Ncer.Camera;
using System.Collections.Generic;
using log4net;
using System.Diagnostics;
using Ncer.Camera.FrameFrocess;

namespace Ncer.Camera
{
    /// <summary>
    /// 相机控制类
    /// 图像拍摄模块 提供相公功能整合 自动曝光 图像预处理等
    /// 不再处理暗电流 暗电流由 亮度计模块处理
    /// </summary>
    public class CameraManager
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CameraManager));

        #region 事件
        public delegate void CameraPreviewFrameHandler(object sender, FrameImage frameImage);
        public delegate void CameraDiscardedFrameHandler(object sender, FrameImage frameImage);
        public delegate void CameraEventHandler(object sender, CameraEventArgs cameraEventArgs);
        public event CameraEventHandler OnCameraEvent;
        public event CameraPreviewFrameHandler OnPreviewFrame;
        public event CameraDiscardedFrameHandler OnDiscardedFrame;


        #endregion
        #region 成员
        private int frameToSkip = 0;
        private int frameToSkipAfterExpoSet = 2;
        private Camera camera;
        private bool isConnected;
        private bool flipH;
        private bool flipV;
        private bool isTaking;
        private bool autoExpo = false;
        private int maxAutoExpo = 15;
        private AutoExpoController autoExpoController;


        #endregion
        #region 属性
        public bool CancelTakeFlag { get; set; }
        public bool IsTakeCaneled { get; set; }
        public double PreviewModeGain { get; set; } = 1;
        public double TakeModeGain { get; set; } = 1;
        public bool AutoExpo { get => autoExpo; set => autoExpo = value; }
        public int MaxAutoExpo { get => maxAutoExpo; set => maxAutoExpo = value; }
        public Camera Camera { get => camera; set => camera = value; }
        public bool IsConnected { get => isConnected; set => isConnected = value; }
        public int FrameToSkip { get => frameToSkip; set => frameToSkip = value; }
        public int FrameToSkipAfterExpoSet { get => frameToSkipAfterExpoSet; set => frameToSkipAfterExpoSet = value; }
        public bool FlipH { get => flipH; set => flipH = value; }
        public bool FlipV { get => flipV; set => flipV = value; }
        public bool FlipHDefault { get; set; }
        public bool FlipVDefault { get; set; } = true;

        public bool FlipVToCamera { get => FlipVDefault ? !FlipV : FlipV; }
        public bool FlipHToCamera { get => FlipHDefault ? !FlipH : FlipH; }

        public Rotation Rotation { get; set; } = Rotation.Clockwise0;

        /// <summary>
        /// 图像预处理
        /// </summary>
        public IFrameProcess Pretreatment { get; set; }

        public double VirtualGain { get; set; } = 1;
        public double VirtualGainPreview { get; set; } = 1;
        ///// <summary>
        ///// 暗电流数据 为畸变校正之前的数据
        ///// </summary>
        //public List<FrameImage> DarkCurrents { get => darkCurrents; set => darkCurrents = value; }
        ///// <summary>
        ///// 需要采集的暗点流的曝光时间
        ///// </summary>
        //public List<double> DarkCurrentExpos { get => darkCurrentExpos; set => darkCurrentExpos = value; }

        /// <summary>
        /// 是否准备好拍摄
        /// </summary>
        public bool IsPrepared
        {
            get => isConnected && (!IsBusy);
        }
        /// <summary>
        /// 是否正在预览
        /// </summary>
        public bool IsPreviewing
        {
            get => camera == null ? false : camera.GetCameraMode() == CameraMode.Preview;
        }

        /// <summary>
        /// 是否正在拍摄
        /// </summary>
        public bool IsTaking
        {
            get => isTaking; set
            {
                isTaking = value;
            }
        }
        /// <summary>
        /// 是否忙碌
        /// </summary>
        public bool IsBusy
        {
            get => isTaking;
        }
        public AutoExpoController AutoExpoController { get => autoExpoController; set => autoExpoController = value; }



        #endregion

        public CameraManager()
        {
            this.autoExpoController = new AutoExpoController();
        }

        #region 相机参数
        public bool SetExpoureTime(double ms)
        {
            try
            {
                camera.ExposureTime = ms;
                FrameToSkip = FrameToSkipAfterExpoSet;
                log.Info("set exposure to" + ms);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public double GetExpoureTime()
        {
            try
            {
                return camera.ExposureTime;
            }
            catch (Exception ex)
            {
                return 0;
            }

            return 0;
        }


        #endregion
        #region 相机控制
        public bool StartPreview()
        {
            this.AutoExpoController.Reset();
            if (!this.camera.SetGain(this.PreviewModeGain)){
                return false;
            }
            if (this.camera.GetCameraMode() == CameraMode.Preview)
            {
                return false;
            }
            var ret = this.camera.StartPreview();
            return ret;

        }

        public bool StopPreview()
        {
            if (this.camera != null && this.camera.GetCameraMode() == CameraMode.Preview)
            {
                var ret = this.camera.StopPreview();
                return ret;
            }
            return false;
        }
        #endregion
        #region 图像拍摄
        /// <summary>
        /// 图像旋转和翻转
        /// </summary>
        /// <param name="frameImage"></param>
        private void FlipAndRotation(FrameImage frameImage)
        {
            bool flipV = FlipVToCamera;
            bool flipH = FlipHToCamera;
            bool transport = false;
            switch (Rotation)
            {
                case Rotation.Clockwise0:
                    break;
                case Rotation.Clockwise90:
                    transport = true;
                    flipH = !flipH;
                    break;
                case Rotation.Clockwise180:
                    flipH = !flipH;
                    flipV = !flipV;
                    break;
                case Rotation.Clockwise270:
                    transport = true;
                    flipV = !flipV;
                    break;
                default:
                    break;
            }
            //转置
            if (transport)
            {
                Cv2.Transpose(frameImage.Mat, frameImage.Mat);
            }

            //翻转
            if (flipH || flipV)
            {
                FlipMode flipMode = FlipMode.XY;
                if (flipH && (!flipV))
                {
                    flipMode = FlipMode.Y;
                }
                else if ((!flipH) && (flipV))
                {
                    flipMode = FlipMode.X;

                }
                Cv2.Flip(frameImage.Mat, frameImage.Mat, flipMode);
            }

        }
        /// <summary>
        /// 图像预处理
        /// </summary>
        /// <param name="frameImage"></param>
        private void PretreatmentFrame(FrameImage frameImage)
        {
            if (Pretreatment != null)//图像预处理
            {
                try
                {
                    Pretreatment.Process(frameImage);
                }
                catch (Exception ex)
                {
                    throw new Exception("图像预处理异常!");
                }
            }
        }



        /// <summary>
        /// 采集一张图像,有准确的曝光时间,即跳过指定帧数 toup相机 
        /// Apogee相机不需要 跳过一定帧数
        /// </summary>
        /// <returns></returns>
        private async Task<FrameImage> TakeImageWithExactExpoAsync()
        {

            FrameImage frameImage = null;
            do
            {
                if (CancelTakeFlag)
                {
                    return null;
                }
                if (frameImage != null)
                {
                    this.OnDiscardedFrame?.Invoke(this, frameImage);
                }
                //
                GC.Collect();
                var frame = await camera.TakeImageAsync();
                if (frame == null) throw new Exception("Fail to take image!");
                frame.ExposureTime = camera.ExposureTime;
                frameImage = FrameImage.CreateImageFromFrame(frame, false);
                //旋转和翻转
                FlipAndRotation(frameImage);
                PretreatmentFrame(frameImage);

                if (this.VirtualGain != 1)
                {
                    frameImage.Mat *= this.VirtualGain;
                }
                frameImage.SetUserData<double>("gain", VirtualGain);//向图像中添加模拟增益系数
                AutoExpoController.SignalLevelHelper.ComputeSignalLevel(frameImage);


                if (FrameToSkip > 0)
                {
                    FrameToSkip--;
                }

            } while (FrameToSkip > 0);

            return frameImage;
        }

        /// <summary>
        /// 获得一张图像
        /// </summary>
        /// <returns></returns>
        public async Task<FrameImage> TakeImageAsync()
        {
            CancelTakeFlag = false;
            IsTakeCaneled = false;
            IsTaking = true;
            OnCameraStateChanged();
            if (!this.camera.SetGain(this.TakeModeGain))
            {
                throw new Exception("Fail to set camera gain");
            }
            ExpoState expoState=ExpoState.None;
            FrameImage frameImage = null;
            if (autoExpo)
            {
                int count = 0;
                do
                {
                    if (frameImage != null) { this.OnDiscardedFrame?.Invoke(this, frameImage); }
                    if (count >= this.MaxAutoExpo ) { break; }
                    frameImage = await this.TakeImageWithExactExpoAsync();
                    count++;
                    //var expo = AutoExpoController.GetNextValue(camera.ExposureTime, frameImage, out autoExpoFinish ,out autoExpoFail);
                    expoState = AutoExpoController.GetNextValue(frameImage,out var expo, out var gain);
                    Debug.WriteLine(expoState.ToString());
                    Debug.WriteLine(expo);
                    Debug.WriteLine(gain);
                    if (expoState == ExpoState.Adjusting)
                    {
                        this.SetExpoureTime(expo);
                        this.VirtualGain = gain;
                    }
                } while (expoState == ExpoState.Adjusting);
            }
            else
            {
                frameImage = await this.TakeImageWithExactExpoAsync();
                //AutoExpoController.SignalLevelHelper.ComputeSignalLevel(frameImage);
            }
            IsTaking = false;
            OnCameraStateChanged();
            frameImage.ExpoState = expoState;
            return frameImage;
        }

        /// <summary>
        /// 采集一张原始图像 不减去暗电流,不自动曝光,不设置增益
        /// </summary>
        /// <returns></returns>
        public async Task<FrameImage> TakeImageRawAsync()
        {
            CancelTakeFlag = false;
            IsTakeCaneled = false;
            IsTaking = true;
            OnCameraStateChanged();
            var image = await this.TakeImageWithExactExpoAsync();
            IsTaking = false;
            OnCameraStateChanged();
            return image;
        }

        /// <summary>
        /// 取消拍摄
        /// </summary>
        public void CancelTake()
        {
            CancelTakeFlag = true;
        }


        #endregion
        #region 暗电流
        ///// <summary>
        ///// 减去暗电流 数据类型为原始数据类型
        ///// </summary>
        ///// <param name="image"></param>
        ///// <returns></returns>
        //public FrameImage SubtractDarkCurrent(FrameImage image)
        //{
        //    var expo = image.Exposure;
        //    var dark = GetDarkCurrentMat(expo);
        //    if (dark == null) return image;
        //    Cv2.Subtract(image.Mat, dark, image.Mat);
        //    return image;
        //}

        ///// <summary>
        ///// 插值得到 暗电流矩阵
        ///// </summary>
        ///// <param name="expo"></param>
        ///// <returns></returns>
        //private Mat GetDarkCurrentMat(double expo)
        //{
        //    if (this.DarkCurrents == null || DarkCurrents.Count <= 1)
        //    {
        //        return null;
        //    }
        //    if (DarkCurrents.Count == 0)
        //    {
        //        return DarkCurrents[0].Mat;
        //    }
        //    if (expo <= DarkCurrents[0].Exposure)
        //    {
        //        return darkCurrents[0].Mat;
        //    }
        //    if (expo >= DarkCurrents[DarkCurrents.Count-1].Exposure)
        //    {
        //        return DarkCurrents[DarkCurrents.Count - 1].Mat;
        //    }
        //    var low = DarkCurrents[0];
        //    var high = DarkCurrents[1];
        //    for (int i = 1; i < DarkCurrents.Count - 1; i++)
        //    {
        //        if (expo > DarkCurrents[i].Exposure)
        //        {
        //            low = DarkCurrents[i];
        //            high = DarkCurrents[i + 1];
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }
        //    Mat tmp = new Mat();
        //    var alpha = (high.Exposure - expo) / (high.Exposure - low.Exposure);
        //    var beta = 1 - alpha;
        //    Cv2.AddWeighted(low.Mat, alpha, high.Mat, beta, 0, tmp);
        //    return tmp;

        //}

        #endregion
        #region 相机状态管理
        public bool ConnectCamera(CameraName cameraModel)
        {
            if (camera != null)
            {
                return false;
            }
            try
            {
                switch (cameraModel)
                {
                    case CameraName.Apogee:
                        this.camera = new ApogeeCamera();
                        this.camera.OnCameraPreviewEvent += Camera_OnCameraPreviewEvent;
                        this.camera.OnCameraEvent += Camera_OnCameraEvent;
                        if (!this.camera.Init())
                        {
                            this.isConnected = false;
                            return false;
                        }
                        this.isConnected = true;
                        break;
                    case CameraName.Toupcam:
                        this.camera = new ToupCamera();
                        this.camera.OnCameraPreviewEvent += Camera_OnCameraPreviewEvent;
                        this.camera.OnCameraEvent += Camera_OnCameraEvent;
                        if (!this.camera.Init())
                        {
                            this.isConnected = false;
                            return false;
                        }
                        this.isConnected = true;
                        break;
                    default:
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
                return false;
            }

        }

        //public async Task<FrameImage> TakeDarkAsync(double time)
        //{
        //    camera.ExposureTime = time;
        //    var frame = await camera.TakeImageAsync();
        //    this.dark = FrameImage.CreateImageFromFrame(frame);
        //    return this.dark;
        //}


        public void CloseCamera()
        {
            if (this.camera != null)
            {

                this.camera.Close();
            }
            isConnected = false;
            camera = null;
        }

        public bool StartCamera()
        {
            try
            {
                this.camera.Start();
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        public bool StopCamera()
        {
            try
            {
                this.camera.Stop();
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        public void Reset()
        {
            try
            {
                camera.Stop();
                camera.Close();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
            finally
            {
                camera = null;
                isConnected = false;
            }
        }

        

        #endregion
        #region 相机事件回调
        private void Camera_OnCameraEvent(object sender, CameraEventArgs cameraEventArgs)
        {
            this.OnCameraEvent?.Invoke(this, cameraEventArgs);
        }

        private void Camera_OnCameraPreviewEvent(object sender, Frame frame)
        {

            FrameImage frameImage = FrameImage.CreateImageFromFrame(frame, false);
            //旋转和翻转
            FlipAndRotation(frameImage);
            PretreatmentFrame(frameImage);
            //数字增益
            frameImage.SetUserData<double>("gain", this.VirtualGainPreview);
            frameImage.Mat *= this.VirtualGainPreview;
            autoExpoController.SignalLevelHelper.ComputeSignalLevel(frameImage);//信号水平
            
            if (this.FrameToSkip > 0) FrameToSkip--;
            this.OnPreviewFrame?.Invoke(this, frameImage);
        }

        #endregion

        public void OnCameraStateChanged()
        {
            //OnPropertyChanged(nameof(IsPrepared));
            //OnPropertyChanged(nameof(IsPreviewing));
            //OnPropertyChanged(nameof(IsConnected));
            //OnPropertyChanged(nameof(IsBusy));
        }
    }


    public enum CameraName
    {
        Apogee = 0,
        Toupcam = 1,
    }

    public enum Rotation
    {
        Clockwise0,
        Clockwise90,
        Clockwise180,
        Clockwise270,
    }




}
