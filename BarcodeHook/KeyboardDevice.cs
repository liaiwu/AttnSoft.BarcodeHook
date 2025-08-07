using AttnSoft.BarcodeHook.RawInput;
using System;

namespace AttnSoft.BarcodeHook
{
    public class KeyboardDevice : RawDevice
    {
        public string? DeviceId { get; set; }
        internal KeyboardDevice(nint hDevice)
            : base(hDevice)
        {
        }
        public override string ToString()
        {
            return $"Keyboard: ID:{DeviceId}";
        }
    }
}