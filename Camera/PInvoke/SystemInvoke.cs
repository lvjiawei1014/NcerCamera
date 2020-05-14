using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Ncer.Camera
{
    public static class SystemInvoke
    {
        [DllImport("msvcrt.dll",EntryPoint ="memcpy",CallingConvention =CallingConvention.Cdecl,SetLastError =false)]
        public static extern IntPtr MemCopy(IntPtr dst, IntPtr src, int length);


    }
}
