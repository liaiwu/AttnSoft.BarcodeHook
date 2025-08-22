
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
        //IntPtr udev = IntPtr.Zero;
        //IntPtr li = IntPtr.Zero;

        private ConcurrentDictionary<IntPtr, RawDevice> _devices = new ConcurrentDictionary<IntPtr, RawDevice>();
        public static LinuxRawDeviceInput Instance = new LinuxRawDeviceInput();
        private LinuxRawDeviceInput()
        {
            Initialize();
            _wndThread = new Thread(RunInput) { IsBackground = true };
            _wndThread.Start();
        }
        /// <summary>
        /// 获取所有输入设备
        /// </summary>
        /// <returns></returns>
        public List<RawDevice> GetDeviceList()
        {
            return _devices.Values.ToList();
        }
        /// <summary>
        /// 创建模拟窗口监听输入设备消息
        /// </summary>
        private void Initialize()
        {
            Console.WriteLine($"Initialize...");

       
            //GetAllDevice();
        }
        #region 监控设备变动(基于udev 的实现)
        //protected void Run()
        //{
        //    IntPtr udev = LinuxApi.Libudev.udev_new();
        //    RunAssert(udev == IntPtr.Zero, "HidSharp udev_new failed.");
        //    try
        //    {
        //        IntPtr monitor = LinuxApi.Libudev.udev_monitor_new_from_netlink(udev, "udev");
        //        RunAssert(monitor == IntPtr.Zero, "HidSharp udev_monitor_new_from_netlink failed.");
        //        try
        //        {
        //            int ret;
        //            ret = LinuxApi.Libudev.udev_monitor_filter_add_match_subsystem_devtype(monitor, "hid", null);
        //            RunAssert(ret < 0, "HidSharp udev_monitor_failed_add_match_subsystem_devtype failed.");

        //            ret = LinuxApi.Libudev.udev_monitor_enable_receiving(monitor);
        //            RunAssert(ret < 0, "HidSharp udev_monitor_enable_receiving failed.");

        //            int fd = LinuxApi.Libudev.udev_monitor_get_fd(monitor);
        //            RunAssert(fd < 0, "HidSharp udev_monitor_get_fd failed.");

        //            var pollFds = new LinuxApi.Libc.Pollfd[1];
        //            pollFds[0].fd = fd;
        //            pollFds[0].events = LinuxApi.Libc.Pollev.IN;

        //            while (true)
        //            {
        //                ret = LinuxApi.Libc.Retry(() => LinuxApi.Libc.poll(pollFds, 1, -1));
        //                if (ret < 0) { break; }
        //                Console.WriteLine($"监测到设备变动.fds[0].revents:{pollFds[0].revents}");
        //                if ((pollFds[0].revents & Pollev.IN) == 0)
        //                {
        //                    Console.WriteLine("pollFds[0].revents & POLLIN) == 0");
        //                    continue;
        //                }
        //                if (ret == 1)
        //                {
        //                    if (0 != (pollFds[0].revents & (LinuxApi.Libc.Pollev.ERR | LinuxApi.Libc.Pollev.HUP | LinuxApi.Libc.Pollev.NVAL))) { break; }
        //                    if (0 != (pollFds[0].revents & LinuxApi.Libc.Pollev.IN))
        //                    {

        //                        IntPtr device = LinuxApi.Libudev.udev_monitor_receive_device(monitor);
        //                        if (device != IntPtr.Zero)
        //                        {
        //                            string action= LinuxApi.Libudev.udev_device_get_action(device);
        //                            bool attached = "add" == action || "bind"==action;
        //                            Console.WriteLine($"发现硬件变动:{action},device:{device}");
        //                            if (attached)
        //                            {
        //                                if (!_devices.TryGetValue(device, out RawDevice? rawDevice))
        //                                {
        //                                    rawDevice = TryCreate(udev, device);
        //                                    if (rawDevice != null)
        //                                    {
        //                                        _devices.TryAdd(device, rawDevice);
        //                                        DeviceAction?.Invoke(new DeviceEvent(rawDevice, attached));
        //                                    }
        //                                }
        //                            }
        //                            else
        //                            {

        //                                if (_devices.TryRemove(device, out RawDevice? rawDevice))
        //                                {
        //                                    DeviceAction?.Invoke(new DeviceEvent(rawDevice, attached));
        //                                }
        //                            }

        //                            LinuxApi.Libudev.udev_device_unref(device);
        //                        }
        //                    }
        //                }
        //            }
        //            Console.WriteLine("退出设备监控.");
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine("设备监控异常:"+ e.ToString());
        //        }
        //        finally
        //        {
        //            LinuxApi.Libudev.udev_monitor_unref(monitor);
        //        }
        //    }
        //    finally
        //    {
        //        LinuxApi.Libudev.udev_unref(udev);
        //    }
        //}
        #endregion
        #region 监控设备变动(基于input 的实现)
        protected void RunInput()
        {
            IntPtr udev = LinuxApi.Libudev.udev_new();
            RunAssert(udev == IntPtr.Zero, "udev_new failed.");

            OpenDeviceDelegate openDelegate = (string path, int flags, IntPtr userData) => LinuxApi.Libc.open(path, (Oflag)flags);
            CloseDeviceDelegate closeDelegate = (int fd, IntPtr userData) => LinuxApi.Libc.close(fd);

            var interface1 = new libinput_interface
            {
                open_restricted = Marshal.GetFunctionPointerForDelegate(openDelegate),
                close_restricted = Marshal.GetFunctionPointerForDelegate(closeDelegate),
            };
            // 创建 libinput 上下文（基于 udev）
            var li = libinput_udev_create_context(ref interface1, IntPtr.Zero, udev);
            if (li == IntPtr.Zero)
            {
                Console.WriteLine("Failed to create libinput context");
                LinuxApi.Libudev.udev_unref(udev);
                return;
            }
            //  绑定到 seat0
            if (libinput_udev_assign_seat(li, "seat0") != 0)
            {
                Console.WriteLine("Failed to assign seat0");
                libinput_unref(li);
                LinuxApi.Libudev.udev_unref(udev);
                return;
            }
            // 处理事件，完成设备枚举
            libinput_dispatch(li);
            HandleDeviceEvent(ref li);

            if (li == IntPtr.Zero || udev == IntPtr.Zero)
            {
                return;
            }
            Console.WriteLine("RunInput....");
            try
            {
                // 事件循环（对齐原生：用 poll() 等待事件，而非 Thread.Sleep）
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
                    // 用 poll 等待事件（非阻塞，超时 -1 表示无限等待）
                    int pollResult = poll(pollFds, 1, -1);
                    if (pollResult > 0 && (pollFds[0].revents & Pollev.IN) != 0)
                    {
                        // 分发事件（从内核读取并处理）
                        libinput_dispatch(li);
                        HandleDeviceEvent(ref li);
                    }
                }
            }
            finally
            {
                // 清理资源
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
            // 逐个获取事件并处理
            IntPtr eventHandle;
            while ((eventHandle = libinput_get_event(li)) != IntPtr.Zero)
            {
                var type = libinput_event_get_type(eventHandle);
                var inputDevice = libinput_event_get_device(eventHandle);
                //var deviceName = libinput_device_get_name(inputDevice) ?? "unknown device";
                //var sysname = libinput_device_get_sysname(inputDevice);
                //var pid = libinput_device_get_id_product(inputDevice);
                //var vid = libinput_device_get_id_vendor(inputDevice);
                //libinput_device_get_id_bustype(inputDevice);
                //Console.WriteLine($"type:{type}");
                switch (type)
                {
                    case libinput_event_type.LIBINPUT_EVENT_DEVICE_ADDED:
                        //Console.WriteLine($"deviceName:{deviceName},sysname:{sysname},vid:{vid.ToString("X")},pid:{pid.ToString("X")}");
                        Console.WriteLine("device added");
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
                        Console.WriteLine("device removed");
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
                            // 构造 KeyboardMsg
                            var keyboardMsg = new KeyboardDeviceMsg(device)
                            {
                                ScanCode =(int) key, // Linux 按键码
                                VkCode = (int)key,   // VkCode 与 ScanCode 相同
                                Msg = (uint)(keyState == libinput_key_state.LIBINPUT_KEY_STATE_PRESSED ? WinApi.WM_KEYDOWN : WinApi.WM_KEYUP) // WM_KEYDOWN 或 WM_KEYUP
                            };
                            // 创建 KeyboardKey 实例
                            KeyPressAction?.Invoke(keyboardMsg);
                        }
                        break;

                }

                libinput_event_destroy(eventHandle);  // 释放事件
            }
        }

        private void GetAllDevice()
        {
            string subsystem = "input";// "hid"; //"hidraw";//"input"; 
            IntPtr udev = LinuxApi.Libudev.udev_new();
            if (IntPtr.Zero != udev)
            {
                try
                {
                    IntPtr enumerate = LinuxApi.Libudev.udev_enumerate_new(udev);
                    if (IntPtr.Zero != enumerate)
                    {
                        try
                        {
                            //LinuxApi.Libudev.udev_enumerate_add_match_subsystem(enumerate, subsystem);
                            if (0 == LinuxApi.Libudev.udev_enumerate_add_match_subsystem(enumerate, subsystem) &&
                                0 == LinuxApi.Libudev.udev_enumerate_scan_devices(enumerate))
                            {
                                IntPtr entry;
                                for (entry = LinuxApi.Libudev.udev_enumerate_get_list_entry(enumerate); entry != IntPtr.Zero;
                                     entry = LinuxApi.Libudev.udev_list_entry_get_next(entry))
                                {
                                    string syspath = LinuxApi.Libudev.udev_list_entry_get_name(entry);
                                    Console.WriteLine($"GetAllDevice syspath:{syspath}");
                                    if (syspath != null)
                                    {
                                        IntPtr device = LinuxApi.Libudev.udev_device_new_from_syspath(udev, syspath);
                                        //string devnode = LinuxApi.Libudev.udev_device_get_devnode(device);
                                        //Console.WriteLine($"GetAllDevice devnode:{devnode}");
                                        var newDevice = TryCreate(udev, device);
                                        if (null != newDevice)
                                        {
                                            _devices[device] = newDevice;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Ignore any exceptions.
                            Console.WriteLine("GetAllDevice:" + ex.Message);
                        }
                        finally
                        {
                            LinuxApi.Libudev.udev_enumerate_unref(enumerate);
                        }
                    }
                }
                finally
                {
                    LinuxApi.Libudev.udev_unref(udev);
                }
            }
        }

        internal static RawDevice? TryCreate(IntPtr inputDevice)
        {
            if (IntPtr.Zero == inputDevice) return null;
            var has_capability = libinput_device_has_capability(inputDevice, libinput_device_capability.LIBINPUT_DEVICE_CAP_KEYBOARD);
            if (has_capability == 0) return null;//不是键盘

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
            //// 清理资源
        }
    }
}