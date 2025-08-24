using AttnSoft.BarcodeHook.RawInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace AttnSoft.BarcodeHook
{
    public class KeyboardHookService
    {
        public static IKeyboardHook GetService()
        {
#if !NETFRAMEWORK
            // 判断是否是 Linux
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("当前运行在 Linux 上");
                return LinuxRawDeviceInput.Instance;
            }
#endif
            return RawDeviceInput.Instance;

        }
    }
}
