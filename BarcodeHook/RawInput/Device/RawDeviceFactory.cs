using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AttnSoft.BarcodeHook.RawInput
{
    using static WinApi.User32;
    using static WinApiHelper;

    public class RawDeviceFactory : IRawDeviceFactory
    {
        private readonly NativeMemoryBuffer _buffer;

        public RawDeviceFactory()
        {
            _buffer = new NativeMemoryBuffer();
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }

        public RawDevice? FromHDevice(IntPtr hDevice)
        {
            RID_DEVICE_INFO info = default;
            int size = Marshal.SizeOf(typeof(RID_DEVICE_INFO));
            if (GetRawInputDeviceInfoW(hDevice, GetRawDeviceInfoCommand.RIDI_DEVICEINFO, ref info, ref size) < 0)
            {
                Console.WriteLine($"Cannot get raw input device info {WinApi.FormatMessage()}.");
                return null;
            }
            size = 0;
            if (GetRawInputDeviceInfoW(hDevice, GetRawDeviceInfoCommand.RIDI_DEVICENAME, IntPtr.Zero, ref size) < 0)
            {
                Console.WriteLine($"Cannot get raw input device name length {WinApi.FormatMessage()}.");
                return null;
            }

            var sb = new StringBuilder(size);
            if (GetRawInputDeviceInfoW(hDevice, GetRawDeviceInfoCommand.RIDI_DEVICENAME, sb, ref size) < 0)
            {
                Console.WriteLine($"Cannot get raw input device name {WinApi.FormatMessage()}.");
                return null;
            }
            if (info.dwType == RawInputType.RIM_TYPEKEYBOARD)
            {
                string devicePath = sb.ToString();
                return GetKeyboardDevice(hDevice, devicePath);
            }
            return null;
        }
        private RawDevice GetKeyboardDevice(nint hdevice, string devicePath)
        {
            var split = devicePath.Substring(4).Split('#');
            var classCode = split[0];       // HID (Class code)
            var subClassCode = split[1];    // PNP0303 (SubClass code)
            var protocolCode = split[2];    // 3&13c0b0c5&0 (Protocol code)
            //string deviceName = "未知设备";
            //try
            //{
            //    var deviceKey = Registry.LocalMachine.OpenSubKey($"System\\CurrentControlSet\\Enum\\{classCode}\\{subClassCode}\\{protocolCode}");
            //    var deviceDesc = deviceKey?.GetValue("DeviceDesc")?.ToString();
            //    deviceDesc = deviceDesc?.Substring(deviceDesc.IndexOf(';') + 1);
            //    deviceName= deviceDesc?? deviceName;
            //}
            //catch{ }
            string hardwareId = $"{classCode}_{subClassCode}_{protocolCode}";
            return new RawDevice(hdevice) {DeviceId= hardwareId, DevicePath = devicePath };
        }
    }
}