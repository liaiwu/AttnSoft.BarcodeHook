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
#if NETSTANDARD1_0_OR_GREATER
        IReadOnlyDictionary<IntPtr, RawDevice> Devices { get; }
#else
        ConcurrentDictionary<IntPtr, RawDevice> Devices { get; }
#endif
        event Action<DeviceEvent>? DeviceAction;
        event Action<KeyboardDeviceMsg>? KeyPressAction;
    }
}
