using AttnSoft.BarcodeHook.RawInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttnSoft.BarcodeHook
{
    public class KeyboardMsg
    {
        public uint Msg { get; set; }//系统消息
        public int VkCode { get; set; }
        public int ScanCode { get; set; }
    }
    public class KeyboardDeviceMsg: KeyboardMsg
    {
        public RawDevice KeyboardDevice { get; private set; }
        public KeyboardDeviceMsg(RawDevice device)
        {
            KeyboardDevice=device;
        }
    }
}
