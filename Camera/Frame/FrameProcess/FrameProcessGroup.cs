using Ncer.ImageToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncer.Camera.FrameFrocess
{
    public class FrameProcessGroup : IFrameProcess
    {
        public List<IFrameProcess> FrameProcesses { get; } = new List<IFrameProcess>();

        public void Process(MatImage image)
        {
            foreach (var item in FrameProcesses)
            {
                item.Process(image);
            }
        }
    }
}
