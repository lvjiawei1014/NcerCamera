using Ncer.ImageToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncer.Camera.FrameFrocess
{
    public interface IFrameProcess
    {
        void Process(MatImage image);
    }
}
