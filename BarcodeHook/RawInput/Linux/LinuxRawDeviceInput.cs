
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace AttnSoft.BarcodeHook.RawInput
{
    using static AttnSoft.BarcodeHook.LinuxApi.Libc;
    using static AttnSoft.BarcodeHook.LinuxApi.Libinput;
    internal class LinuxRawDeviceInput : IKeyboardHook, IDisposable
    {
        public event Action<DeviceEvent>? DeviceAction;
        public event Action<KeyboardDeviceMsg>? KeyPressAction;
        private readonly Thread _wndThread;

        private ConcurrentDictionary<IntPtr, RawDevice> _devices = new ConcurrentDictionary<IntPtr, RawDevice>();
        public static LinuxRawDeviceInput Instance = new LinuxRawDeviceInput();
        private LinuxRawDeviceInput()
        {
            Initialize();
            _wndThread = new Thread(RunInput) { IsBackground = true };
            _wndThread.Start();
        }
        /// <summary>
        /// ��ȡ���������豸
        /// </summary>
        /// <returns></returns>
        public List<RawDevice> GetDeviceList()
        {
            return _devices.Values.ToList();
        }
        /// <summary>
        /// ����ģ�ⴰ�ڼ��������豸��Ϣ
        /// </summary>
        private void Initialize()
        {
            Console.WriteLine($"Initialize...");
            //GetAllDevice();
        }

        #region ����豸�䶯(����input ��ʵ��)
        protected void RunInput()
        {
            IntPtr li= IntPtr.Zero;
            IntPtr udev = LinuxApi.Libudev.udev_new();
            RunAssert(udev == IntPtr.Zero, "udev_new failed.");

            OpenDeviceDelegate openDelegate = (string path, int flags, IntPtr userData) => LinuxApi.Libc.open(path, (Oflag)flags);
            CloseDeviceDelegate closeDelegate = (int fd, IntPtr userData) => LinuxApi.Libc.close(fd);

            var interface1 = new libinput_interface
            {
                open_restricted = Marshal.GetFunctionPointerForDelegate(openDelegate),
                close_restricted = Marshal.GetFunctionPointerForDelegate(closeDelegate),
            };
            try
            {
                // ���� libinput �����ģ����� udev��
                li = libinput_udev_create_context(ref interface1, IntPtr.Zero, udev);
                if (li == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to create libinput context");
                    LinuxApi.Libudev.udev_unref(udev);
                    return;
                }
                //  �󶨵� seat0
                if (libinput_udev_assign_seat(li, "seat0") != 0)
                {
                    Console.WriteLine("Failed to assign seat0");
                    libinput_unref(li);
                    LinuxApi.Libudev.udev_unref(udev);
                    return;
                }
                // �����¼�������豸ö��
                libinput_dispatch(li);
                HandleDeviceEvent(ref li);
            }catch(Exception e)
            {
                Console.WriteLine($"RunInput error:{e.Message}");
                Console.WriteLine($"���鵱ǰ�û��Ƿ���input��,���Բο���������û���ӵ�input��:(��Ҫע��������)");
                Console.WriteLine($"sudo usermod -aG input $USER");
            }

            if (li == IntPtr.Zero || udev == IntPtr.Zero)
            {
                return;
            }
            //Console.WriteLine("RunInput....");
            try
            {
                // �¼�ѭ��������ԭ������ poll() �ȴ��¼������� Thread.Sleep��
                int fd = libinput_get_fd(li);
                if (fd == -1)
                {
                    Console.WriteLine("Failed to get libinput fd");
                    return;
                }
                var pollFds = new Pollfd[1];
                pollFds[0].fd = fd;
                pollFds[0].events = Pollev.IN;

                while (true)
                {
                    // �� poll �ȴ��¼�������������ʱ -1 ��ʾ���޵ȴ���
                    int pollResult = poll(pollFds, 1, -1);
                    if (pollResult > 0 && (pollFds[0].revents & Pollev.IN) != 0)
                    {
                        // �ַ��¼������ں˶�ȡ������
                        libinput_dispatch(li);
                        HandleDeviceEvent(ref li);
                    }
                }
            }
            finally
            {
                // ������Դ
                if (li != IntPtr.Zero)
                {
                    libinput_unref(li);
                }
                if (udev != IntPtr.Zero)
                {
                    LinuxApi.Libudev.udev_unref(udev);
                }
            }

        }
        #endregion

        private void HandleDeviceEvent(ref IntPtr li)
        {
            // �����ȡ�¼�������
            IntPtr eventHandle;
            while ((eventHandle = libinput_get_event(li)) != IntPtr.Zero)
            {
                var type = libinput_event_get_type(eventHandle);
                var inputDevice = libinput_event_get_device(eventHandle);
                switch (type)
                {
                    case libinput_event_type.LIBINPUT_EVENT_DEVICE_ADDED:
                        //Console.WriteLine("device added");
                        if (!_devices.TryGetValue(inputDevice, out var newDevice))
                        {
                            newDevice = TryCreate(inputDevice);
                            if (newDevice != null)
                            {
                                _devices.TryAdd(inputDevice, newDevice);
                                DeviceAction?.Invoke(new DeviceEvent(newDevice, true));
                            }
                        }
                        break;
                    case libinput_event_type.LIBINPUT_EVENT_DEVICE_REMOVED:
                        //Console.WriteLine("device removed");
                        if (_devices.TryGetValue(inputDevice, out var removeDevice))
                        {
                            DeviceAction?.Invoke(new DeviceEvent(removeDevice, false));
                        }
                        break;
                    case libinput_event_type.LIBINPUT_EVENT_KEYBOARD_KEY:
                        if (_devices.TryGetValue(inputDevice, out var device))
                        {
                            var keyEvent = libinput_event_get_keyboard_event(eventHandle);
                            var key = libinput_event_keyboard_get_key(keyEvent);
                            var keyState = libinput_event_keyboard_get_key_state(keyEvent);
                            Console.WriteLine($"{device.DeviceId}:{key}-{keyState}");
                            // ���� KeyboardMsg
                            var keyboardMsg = new KeyboardDeviceMsg(device)
                            {
                                ScanCode =(int) key, // Linux ������
                                VkCode = (int)key,   // VkCode �� ScanCode ��ͬ
                                Msg = (uint)(keyState == libinput_key_state.LIBINPUT_KEY_STATE_PRESSED ? WinApi.WM_KEYDOWN : WinApi.WM_KEYUP) // WM_KEYDOWN �� WM_KEYUP
                            };
                            // ���� KeyboardKey ʵ��
                            KeyPressAction?.Invoke(keyboardMsg);
                        }
                        break;

                }

                libinput_event_destroy(eventHandle);  // �ͷ��¼�
            }
        }
        internal static RawDevice? TryCreate(IntPtr inputDevice)
        {
            if (IntPtr.Zero == inputDevice) return null;
            var has_capability = libinput_device_has_capability(inputDevice, libinput_device_capability.LIBINPUT_DEVICE_CAP_KEYBOARD);
            if (has_capability == 0) return null;//���Ǽ���

            var deviceName = libinput_device_get_name(inputDevice) ?? "";
            var pid = libinput_device_get_id_product(inputDevice);
            var vid = libinput_device_get_id_vendor(inputDevice);
            var udevDevice = libinput_device_get_udev_device(inputDevice);
            string devnode = LinuxApi.Libudev.udev_device_get_devnode(udevDevice);
            //string syspat = LinuxApi.Libudev.udev_device_get_syspath(udevDevice);
            //Console.WriteLine($"syspat:{syspat}");
            LinuxApi.Libudev.udev_device_unref(udevDevice);
            var newDevice = new RawDevice(inputDevice) { DevicePath = devnode, DeviceName = deviceName };
            string deviceId = $"{devnode}/VID:{vid.ToString("X")}&PID_{pid.ToString("X")}";
            //Console.WriteLine($"deviceId:{deviceId},has_capability:{has_capability}");
            newDevice.DeviceId = deviceId;
            return newDevice;
        }
        internal static RawDevice? TryCreate(IntPtr udev, IntPtr device)
        {
            if (IntPtr.Zero != udev && device != IntPtr.Zero)
            {
                try
                {
                    Console.WriteLine($"TryCreate:");
                    string devnode = LinuxApi.Libudev.udev_device_get_devnode(device);
                    string deviceSubsys = LinuxApi.Libudev.udev_device_get_subsystem(device);
                    string deviceType = LinuxApi.Libudev.udev_device_get_devtype(device);
                    Console.WriteLine($"devnode:{devnode}");
                    Console.WriteLine($"deviceSubsys:{deviceSubsys}");
                    Console.WriteLine($"deviceType:{deviceType}");
                    string name = LinuxApi.Libudev.udev_device_get_property_value(device, "NAME");
                    Console.WriteLine($"name:{name}");
                    string manufacturerstr = LinuxApi.Libudev.udev_device_get_sysattr_value(device, "manufacturer");
                    Console.WriteLine($"manufacturerstr:{manufacturerstr}");
                    string syspat = LinuxApi.Libudev.udev_device_get_syspath(device);
                    Console.WriteLine($"syspat:{syspat}");
                    string idProductstr = LinuxApi.Libudev.udev_device_get_sysattr_value(device, "idProduct");
                    Console.WriteLine($"idProduct: {idProductstr}");
                    string sysname = LinuxApi.Libudev.udev_device_get_sysname(device);
                    Console.WriteLine($"sysname:{sysname}");

                    if (!string.IsNullOrEmpty(sysname) && !sysname.StartsWith("event")) return null;

                    if (syspat != null)
                    {
                        var newDevice = new RawDevice(device) { DevicePath = syspat };
                        IntPtr parent = LinuxApi.Libudev.udev_device_get_parent_with_subsystem_devtype(device, "usb", "usb_device");
                        if (IntPtr.Zero != parent)
                        {
                            Console.WriteLine($"IntPtr.Zero != parent");
                            //string isKeybord = LinuxApi.Libudev.udev_device_get_property_value(parent, "ID_INPUT_KEYBOARD");
                            //Console.WriteLine($"isKeybord:{isKeybord}");
                            devnode = LinuxApi.Libudev.udev_device_get_devnode(parent);
                            Console.WriteLine($"parent devnode:{devnode}");
                            string manufacturer = LinuxApi.Libudev.udev_device_get_sysattr_value(parent, "manufacturer");
                            string productName = LinuxApi.Libudev.udev_device_get_sysattr_value(parent, "product");
                            string serialNumber = LinuxApi.Libudev.udev_device_get_sysattr_value(parent, "serial");
                            string idVendor = LinuxApi.Libudev.udev_device_get_sysattr_value(parent, "idVendor");
                            string idProduct = LinuxApi.Libudev.udev_device_get_sysattr_value(parent, "idProduct");
                            string bcdDevice = LinuxApi.Libudev.udev_device_get_sysattr_value(parent, "bcdDevice");
                            Console.WriteLine($"manufacturer: {manufacturer}");
                            Console.WriteLine($"productName: {productName}");
                            Console.WriteLine($"idVendor: {idVendor}");
                            Console.WriteLine($"idProduct: {idProduct}");
                            int vid, pid, version;
                            if (LinuxApi.Libc.TryParseHex(idVendor, out vid) &&
                                LinuxApi.Libc.TryParseHex(idProduct, out pid) &&
                                LinuxApi.Libc.TryParseHex(bcdDevice, out version))
                            {
                                string deviceId = $"HID_VID_{vid.ToString("X")}&PID_{pid.ToString("X")}&{version}&{serialNumber}";
                                Console.WriteLine($"deviceId:{deviceId}");
                                newDevice.DeviceId = deviceId;
                            }
                        }
                        return newDevice;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            return null;
        }
        static void RunAssert(bool condition, string error)
        {
            if (condition) { throw new InvalidOperationException(error); }
        }

        public void Dispose()
        {
            //// ������Դ
        }
    }
}