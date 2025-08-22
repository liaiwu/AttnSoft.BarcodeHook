using System;

namespace AttnSoft.BarcodeHook.RawInput
{
    public class RawDevice
    {
        public nint DeviceHandle { get; }
        public string DeviceName { get; set; }
        public string? DevicePath { get; set; }
        public string DeviceId { get; set; }

        internal RawDevice(nint hDevice)
        {
            DeviceHandle = hDevice;
            DeviceName = "";
            DeviceId = WinApiHelper.FormatIntPtr(hDevice);
        }
        public override string ToString()
        {
            return $"{DeviceName} {DeviceId}".Trim();
        }
    }
}