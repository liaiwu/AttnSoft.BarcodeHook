using System;

namespace AttnSoft.BarcodeHook.RawInput
{
    public interface IRawDeviceFactory : IDisposable
    {
        RawDevice? FromHDevice(IntPtr hDevice);
    }
}