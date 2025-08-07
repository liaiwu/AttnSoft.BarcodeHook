using System;

namespace AttnSoft.BarcodeHook.RawInput
{
    public class RawDevice
    {
        public nint HDevice { get; }
        public string HDeviceStr { get; }
        public string? DevicePath { get; set; }
        public string DeviceId { get; set; }

        internal RawDevice(nint hDevice)
        {
            HDevice = hDevice;
            DeviceId=HDeviceStr = WinApiHelper.FormatIntPtr(hDevice);
        }
        public override string ToString()
        {
            return DeviceId==null ? HDeviceStr: DeviceId;
        }
    }
}