using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace AttnSoft.BarcodeHook.RawInput
{
    using static WinApi.Kernel32;
    using static WinApi.User32;
    using static WinApiHelper;
    internal class RawDeviceInput : IKeyboardHook,IDisposable
    {
        private const string WindowClass = "Input Hidden Window Class";
        private const string WindowName = "Input Hidden Window Name";
        private const int BufferSize = 1024;

#if NETSTANDARD1_0_OR_GREATER
        public IReadOnlyDictionary<IntPtr, RawDevice> Devices => _devices;
#else
        public ConcurrentDictionary<IntPtr, RawDevice> Devices => _devices;
#endif
        public event Action<DeviceEvent>? DeviceAction;
        public event Action<KeyboardDeviceMsg>? KeyPressAction;

        private readonly IRawDeviceFactory _factory;
        private readonly WndProc _wndProc;
        private readonly Thread _wndThread;
        private IntPtr _hWnd;
        private IntPtr _buffer;
        private int _rawInputHeaderSize;
        private ConcurrentDictionary<IntPtr, RawDevice> _devices;
        public static RawDeviceInput Instance = new RawDeviceInput();
        private RawDeviceInput()
        {
            _factory = new RawDeviceFactory();
            _wndProc = WndProc;
            _buffer = Marshal.AllocHGlobal(BufferSize);
            _rawInputHeaderSize = Marshal.SizeOf(typeof(RAWINPUTHEADER));
            _devices = new ConcurrentDictionary<IntPtr, RawDevice>();
            Initialize();
            _wndThread = new Thread(ThreadFunc) { IsBackground =true};
            _wndThread.Start();
        }
        /// <summary>
        /// 创建模拟窗口监听输入设备消息
        /// </summary>
        private void Initialize()
        {
            IntPtr hInstance = GetModuleHandleW(null);

            WNDCLASSEX wc = new WNDCLASSEX();
            wc.cbSize = Marshal.SizeOf(typeof(WNDCLASSEX));
            wc.lpfnWndProc = _wndProc;
            wc.hInstance = hInstance;
            wc.lpszClassName = WindowClass;
            ushort atom = RegisterClassEx(ref wc);
            if (atom == 0)
            {
                Console.WriteLine($"Cannot register window class {WinApi.FormatMessage()}.");
                return;
            }
            _hWnd = CreateWindowExW(
                dwExStyle: 0,
                lpClassName: new IntPtr(atom),
                lpWindowName: WindowName,
                dwStyle: WindowStyles.WS_POPUP,
                x: 0,
                y: 0,
                nWidth: 100,
                nHeight: 100,
                hWndParent: IntPtr.Zero,
                hMenu: IntPtr.Zero,
                hInstance: hInstance,
                lpParam: IntPtr.Zero);
            if (_hWnd == IntPtr.Zero)
            {
                Console.WriteLine($"Cannot create window {WinApi.FormatMessage()}.");
                return;
            }
            RAWINPUTDEVICE[] devices = new RAWINPUTDEVICE[1];
            devices[0].usUsagePage = RawInputDeviceUsagePage.HID_USAGE_PAGE_GENERIC;
            devices[0].usUsage = RawInputDeviceUsage.HID_USAGE_GENERIC_KEYBOARD;
            devices[0].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK | RawInputDeviceFlags.RIDEV_DEVNOTIFY;
            devices[0].hwndTarget = _hWnd;
            if (!RegisterRawInputDevices(devices, 1, Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
            {
                Console.WriteLine($"Cannot register raw keyboard input {WinApi.FormatMessage()}.");
            }
        }
        private void ThreadFunc()
        {
            while (GetMessage(out MSG msg, _hWnd, 0, 0))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }            
        }
        private IntPtr WndProc(IntPtr hWnd, WindowsMessage msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WindowsMessage.WM_INPUT:
                    ProcessWmInputMessage(lParam);
                    break;

                case WindowsMessage.WM_INPUT_DEVICE_CHANGE:
                    ProcessWmInputDeviceChangeMessage(wParam, lParam);
                    break;

                case WindowsMessage.WM_DESTROY:
                    PostQuitMessage(0);
                    break;
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void ProcessWmInputMessage(IntPtr lParam)
        {
            IntPtr buffer = _buffer;
            int size = BufferSize;
            if (GetRawInputData(lParam, GetRawInputDataCommand.RID_INPUT, buffer, ref size, _rawInputHeaderSize) == -1)
            {
                Console.WriteLine($"GetRawInputData error {WinApi.FormatMessage()}.");
            }
            else
            {
                RAWINPUT? keydata = (RAWINPUT?)Marshal.PtrToStructure(buffer, typeof(RAWINPUT));
                //RAWINPUT data = Marshal.PtrToStructure<RAWINPUT>(buffer);
                if (keydata != null && keydata.Value.header.dwType == RawInputType.RIM_TYPEKEYBOARD)
                {
                    var deviceId = keydata.Value.header.hDevice;
                    if (_devices.TryGetValue(deviceId, out var device))
                    {
                        RAWKEYBOARD keyData = keydata.Value.keyboard;
                        //Console.WriteLine($"MakeCode:{keyData.MakeCode},Message:{keyData.Message},VKey:{keyData.VKey},Flags:{keyData.Flags}");
                        Console.WriteLine($"{device.ToString()}");
                        KeyboardDeviceMsg msg = new KeyboardDeviceMsg(device);
                        msg.VkCode = keyData.VKey;
                        msg.ScanCode = keyData.MakeCode;
                        msg.Msg = keyData.Message;
                        KeyPressAction?.Invoke(msg);
                    }
                }
            }
            ;
        }
        const int GIDC_ARRIVAL = 1;
        const int GIDC_REMOVAL = 2;
        private void ProcessWmInputDeviceChangeMessage(IntPtr wParam, IntPtr lParam)
        {
            if (wParam.ToInt64() == GIDC_ARRIVAL)//插入新设备
            {
                var device = _factory.FromHDevice(lParam);
                if (null != device)
                {
                    _devices.TryAdd(lParam, device);
                    DeviceAction?.Invoke(new DeviceEvent(device, true));
                } 
            }
            if (wParam.ToInt64() == GIDC_REMOVAL)//移除设备
            {
                lock (_devices)
                {
                    if (_devices.TryRemove(lParam, out var device))
                    {
                        DeviceAction?.Invoke(new DeviceEvent(device, false));
                    }
                    else
                    {
                        Console.WriteLine($"Device removed, but not present in the dictionary.");
                    }
                }
            }
        }
        public void Dispose()
        {
            _factory?.Dispose();
            if (_hWnd != IntPtr.Zero)
            {
                SendMessage(_hWnd, WindowsMessage.WM_DESTROY, IntPtr.Zero, IntPtr.Zero);
                _wndThread.Join();
                _hWnd = IntPtr.Zero;
            }
            if (_buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_buffer);
            }
        }
    }
}