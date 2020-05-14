using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncer.Camera
{
    public class VirtualCamera : Camera
    {
        public override double MinExposure => throw new NotImplementedException();
        public override double MaxExposure => throw new NotImplementedException();
        public override double ExposureTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override PixelSize PixelSize { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }
        public override bool IsStarted { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }

        public override event CameraEventHandler OnCameraEvent;
        public override event CameraPreviewFrameHandler OnCameraPreviewEvent;

        public override bool Close()
        {
            throw new NotImplementedException();
        }

        public override CameraMode GetCameraMode()
        {
            throw new NotImplementedException();
        }

        public override Resolution GetPreviewResolution()
        {
            throw new NotImplementedException();
        }

        public override List<Resolution> GetPreviewResolutions()
        {
            throw new NotImplementedException();
        }

        public override Resolution GetResolution()
        {
            throw new NotImplementedException();
        }

        public override List<Resolution> GetResolutions()
        {
            throw new NotImplementedException();
        }

        public override bool Init()
        {
            throw new NotImplementedException();
        }

        public override bool Reset()
        {
            throw new NotImplementedException();
        }

        public override bool SetCameraMode(CameraMode cameraMode)
        {
            throw new NotImplementedException();
        }

        public override bool SetPreviewResolution(Resolution resolution)
        {
            throw new NotImplementedException();
        }

        public override bool SetResolution(Resolution resolution)
        {
            throw new NotImplementedException();
        }

        public override bool Start()
        {
            throw new NotImplementedException();
        }

        public override bool StartPreview()
        {
            throw new NotImplementedException();
        }

        public override bool Stop()
        {
            throw new NotImplementedException();
        }

        public override bool StopPreview()
        {
            throw new NotImplementedException();
        }

        public override Frame TakeImage()
        {
            throw new NotImplementedException();
        }

        public override Task<Frame> TakeImageAsync()
        {
            throw new NotImplementedException();
        }
    }
}
