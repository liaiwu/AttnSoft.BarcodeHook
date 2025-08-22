using AttnSoft.BarcodeHook.RawInput;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttnSoft.BarcodeHook
{
    public interface IKeyboardHook
    {
        List<RawDevice> GetDeviceList();
        event Action<DeviceEvent>? DeviceAction;
        event Action<KeyboardDeviceMsg>? KeyPressAction;
    }
}
