using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace AttnSoft.BarcodeHook
{
    internal class LinuxApi
    {
        internal static class Libc
        {
            const string libc = "libc";

            public enum Error
            {
                OK = 0,
                EPERM = 1,
                EINTR = 4,
                EIO = 5,
                ENXIO = 6,
                EBADF = 9,
                EAGAIN = 11,
                EACCES = 13,
                EBUSY = 16,
                ENODEV = 19,
                EINVAL = 22
            }

            [Flags]
            public enum Oflag
            {
                RDONLY = 0x000,
                WRONLY = 0x001,
                RDWR = 0x002,
                CREAT = 0x040,
                EXCL = 0x080,
                NOCTTY = 0x100,
                TRUNC = 0x200,
                APPEND = 0x400,
                NONBLOCK = 0x800
            }

            [Flags]
            public enum Pollev : short
            {
                IN = 0x01,
                PRI = 0x02,
                OUT = 0x04,
                ERR = 0x08,
                HUP = 0x10,
                NVAL = 0x20
            }

            public struct Pollfd
            {
                public int fd;
                public Pollev events;
                public Pollev revents;
            }

            public static int Retry(Func<int> sysfunc)
            {
                while (true)
                {
                    int ret = sysfunc(); var error = (Error)Marshal.GetLastWin32Error();
                    if (ret >= 0 || error != Error.EINTR) { return ret; }
                }
            }

            public static IntPtr Retry(Func<IntPtr> sysfunc)
            {
                while (true)
                {
                    IntPtr ret = sysfunc(); var error = (Error)Marshal.GetLastWin32Error();
                    if ((long)ret >= 0 || error != Error.EINTR) { return ret; }
                }
            }

            public static bool uname(out string sysname, out Version release)
            {
                string releaseStr; release = null;
                if (!uname(out sysname, out releaseStr)) { return false; }
                releaseStr = new string(releaseStr.Trim().TakeWhile(ch => (ch >= '0' && ch <= '9') || ch == '.').ToArray());
                release = new Version(releaseStr);
                return true;
            }

            public static bool uname(out string sysname, out string release)
            {
                string syscallPath = "Mono.Unix.Native.Syscall, Mono.Posix, PublicKeyToken=0738eb9f132ed756";
                var syscall = Type.GetType(syscallPath);
                if (syscall != null)
                {
                    var unameArgs = new object[1];
                    int unameRet = (int)syscall.InvokeMember("uname",
                                                             BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, null, null, unameArgs,
                                                             CultureInfo.InvariantCulture);
                    if (unameRet >= 0)
                    {
                        var uname = unameArgs[0];
                        Func<string, string> getMember = s => (string)uname.GetType().InvokeMember(s,
                                                                                                   BindingFlags.GetField, null, uname, new object[0],
                                                                                                   CultureInfo.InvariantCulture);
                        sysname = getMember("sysname"); release = getMember("release");
                        return true;
                    }
                }

                try
                {
                    if (File.Exists("/proc/sys/kernel/ostype") && File.Exists("/proc/sys/kernel/osrelease"))
                    {
                        sysname = File.ReadAllText("/proc/sys/kernel/ostype").TrimEnd('\n');
                        release = File.ReadAllText("/proc/sys/kernel/osrelease").TrimEnd('\n');
                        if (sysname != "" && release != "") { return true; }
                    }
                }
                catch
                {

                }

                sysname = null; release = null;
                return false;
            }

            [DllImport(libc, SetLastError = true)]
            public static extern int open([MarshalAs(UnmanagedType.LPStr)] string filename,Oflag oflag);

            [DllImport(libc, SetLastError = true)]
            public static extern int close(int filedes);

            [DllImport(libc, SetLastError = true)]
            public static extern IntPtr read(int filedes, IntPtr buffer, UIntPtr size);

            [DllImport(libc, SetLastError = true)]
            public static extern IntPtr write(int filedes, IntPtr buffer, UIntPtr size);

            [DllImport(libc, SetLastError = true)]
            public static extern int poll([In, Out] Pollfd[] fds, uint nfds, int timeout);

            public static bool TryParseHex(string hex, out int result)
            {
                return int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
            }

            public static bool TryParseVersion(string version, out int major, out int minor)
            {
                major = 0; minor = 0; if (version == null) { return false; }
                string[] parts = version.Split(new[] { '.' }, 2); if (parts.Length != 2) { return false; }
                return int.TryParse(parts[0], out major) && int.TryParse(parts[1], out minor);
            }

            #region ioctl
            // TODO: Linux changes these up depending on platform. Eventually we'll handle it.
            //       For now, x86 and ARM are safe with this definition.
            public const int IOC_NONE = 0;
            public const int IOC_WRITE = 1;
            public const int IOC_READ = 2;
            public const int IOC_NRBITS = 8;
            public const int IOC_TYPEBITS = 8;
            public const int IOC_SIZEBITS = 14;
            public const int IOC_DIRBITS = 2;
            public const int IOC_NRSHIFT = 0;
            public const int IOC_TYPESHIFT = IOC_NRSHIFT + IOC_NRBITS;
            public const int IOC_SIZESHIFT = IOC_TYPESHIFT + IOC_TYPEBITS;
            public const int IOC_DIRSHIFT = IOC_SIZESHIFT + IOC_SIZEBITS;

            public static UIntPtr IOC(int dir, int type, int nr, int size)
            {
                // Make sure to cast this to uint. We do NOT want this casted from int...
                uint value = (uint)dir << IOC_DIRSHIFT | (uint)type << IOC_TYPESHIFT | (uint)nr << IOC_NRSHIFT | (uint)size << IOC_SIZESHIFT;
                return (UIntPtr)value;
            }

            public static UIntPtr IOW(int type, int nr, int size)
            {
                return IOC(IOC_WRITE, type, nr, size);
            }

            public static UIntPtr IOR(int type, int nr, int size)
            {
                return IOC(IOC_READ, type, nr, size);
            }

            public static UIntPtr IOWR(int type, int nr, int size)
            {
                return IOC(IOC_WRITE | IOC_READ, type, nr, size);
            }

            #region hidraw
            public const int HID_MAX_DESCRIPTOR_SIZE = 4096;
            public static readonly UIntPtr HIDIOCGRDESCSIZE = IOR((byte)'H', 1, 4);
            public static readonly UIntPtr HIDIOCGRDESC = IOR((byte)'H', 2, Marshal.SizeOf(typeof(hidraw_report_descriptor)));
            public static UIntPtr HIDIOCSFEATURE(int length) { return IOWR((byte)'H', 6, length); }
            public static UIntPtr HIDIOCGFEATURE(int length) { return IOWR((byte)'H', 7, length); }

            public struct hidraw_report_descriptor
            {
                public uint size;

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = HID_MAX_DESCRIPTOR_SIZE)]
                public byte[] value;
            }

            [DllImport(libc, SetLastError = true)]
            public static extern int ioctl(int filedes, UIntPtr command, out uint value);

            [DllImport(libc, SetLastError = true)]
            public static extern int ioctl(int filedes, UIntPtr command, ref hidraw_report_descriptor value);

            [DllImport(libc, SetLastError = true)]
            public static extern int ioctl(int filedes, UIntPtr command, IntPtr value);

            [DllImport(libc, SetLastError = true)]
            public static extern int ioctl(int filedes, UIntPtr command, ref termios value);

            [DllImport(libc, SetLastError = true)]
            public static extern int ioctl(int filedes, UIntPtr command);
            #endregion
            #endregion

            #region termios
            public static readonly UIntPtr TIOCEXCL = (UIntPtr)0x540c;
            public static readonly UIntPtr TIOCNXCL = (UIntPtr)0x540d;

            public static readonly UIntPtr TCGETS2 = IOR((byte)'T', 0x2a, Marshal.SizeOf(typeof(termios)));
            public static readonly UIntPtr TCSETS2 = IOW((byte)'T', 0x2b, Marshal.SizeOf(typeof(termios)));
            public static readonly UIntPtr TCSETSW2 = IOW((byte)'T', 0x2c, Marshal.SizeOf(typeof(termios)));
            public static readonly UIntPtr TCSETSF2 = IOW((byte)'T', 0x2d, Marshal.SizeOf(typeof(termios)));

            // See /usr/include/asm-generic/termbits.h
            public const int VTIME = 5;
            public const int VMIN = 6;

            public const uint IGNBRK = 0x0001;
            public const uint BRKINT = 0x0002;
            public const uint PARMRK = 0x0008;
            public const uint ISTRIP = 0x0020;
            public const uint INLCR = 0x0040;
            public const uint IGNCR = 0x0080;
            public const uint ICRNL = 0x0100;
            public const uint IXON = 0x0400;

            public const uint OPOST = 0x0001;

            public const uint CBAUD = 0x100f;
            public const uint BOTHER = 0x1000;

            public const uint CSIZE = 0x0030;
            public const uint CS7 = 0x0020;
            public const uint CS8 = 0x0030;
            public const uint CSTOPB = 0x0040;
            public const uint CREAD = 0x0080;
            public const uint PARENB = 0x0100;
            public const uint PARODD = 0x0200;
            public const uint CLOCAL = 0x0800;
            public const uint CRTSCTS = 0x80000000u;

            public const uint ECHO = 0x0008;
            public const uint ECHONL = 0x0040;
            public const uint ICANON = 0x0002;
            public const uint ISIG = 0x0001;
            public const uint IEXTEN = 0x8000;

            public const int TCIFLUSH = 0;

            public const int TCSANOW = 0;

            public unsafe struct termios // termios2
            {
                public uint c_iflag;
                public uint c_oflag;
                public uint c_cflag;
                public uint c_lflag;
                public byte c_line;
                public fixed byte c_cc[19];
                public uint c_ispeed;
                public uint c_ospeed;
            }

            public static void cfmakeraw(ref termios termios)
            {
                // See https://linux.die.net/man/3/cfmakeraw "Raw mode" heading.
                termios.c_iflag &= ~(IGNBRK | BRKINT | PARMRK | ISTRIP | INLCR | IGNCR | ICRNL | IXON);
                termios.c_oflag &= ~OPOST;
                termios.c_lflag &= ~(ECHO | ECHONL | ICANON | ISIG | IEXTEN);
                termios.c_cflag &= ~(CSIZE | PARENB);
                termios.c_cflag |= CS8;
            }

            public static int cfsetspeed(ref termios termios, uint speed)
            {
                termios.c_cflag &= ~CBAUD;
                termios.c_cflag |= BOTHER;
                termios.c_ispeed = speed;
                termios.c_ospeed = speed;
                return 0;
            }

            public static int tcgetattr(int filedes, out termios termios)
            {
                termios = new termios();
                return ioctl(filedes, TCGETS2, ref termios);
            }

            public static int tcsetattr(int filedes, int actions, ref termios termios)
            {
                Debug.Assert(actions == TCSANOW);
                return ioctl(filedes, TCSETS2, ref termios);
            }

            [DllImport(libc, SetLastError = true)]
            public static extern int tcdrain(int filedes);

            [DllImport(libc, SetLastError = true)]
            public static extern int tcflush(int filedes, int action);
            #endregion
        }

        public static class Libudev
        {
            const string libudev = "libudev.so";
            [DllImport(libudev, EntryPoint = "udev_new")]
            internal static extern IntPtr udev_new();
            [DllImport(libudev, EntryPoint = "udev_ref")]
            internal static extern IntPtr udev_ref(IntPtr udev);
            [DllImport(libudev, EntryPoint = "udev_unref")]
            internal static extern void udev_unref(IntPtr udev);
            [DllImport(libudev, EntryPoint = "udev_monitor_new_from_netlink")]
            internal static extern IntPtr udev_monitor_new_from_netlink(IntPtr udev,[MarshalAs(UnmanagedType.LPStr)] string name);
            [DllImport(libudev, EntryPoint = "udev_monitor_unref")]
            internal static extern void udev_monitor_unref(IntPtr monitor);
            [DllImport(libudev, EntryPoint = "udev_monitor_filter_add_match_subsystem_devtype")]
            internal static extern int udev_monitor_filter_add_match_subsystem_devtype(IntPtr monitor,
    [MarshalAs(UnmanagedType.LPStr)] string subsystem,
    [MarshalAs(UnmanagedType.LPStr)] string devtype);
            [DllImport(libudev, EntryPoint = "udev_monitor_enable_receiving")]
            internal static extern int udev_monitor_enable_receiving(IntPtr monitor);
            [DllImport(libudev, EntryPoint = "udev_monitor_get_fd")]
            internal static extern int udev_monitor_get_fd(IntPtr monitor);
            [DllImport(libudev, EntryPoint = "udev_monitor_receive_device")]
            internal static extern IntPtr udev_monitor_receive_device(IntPtr monitor);
            [DllImport(libudev, EntryPoint = "udev_enumerate_new")]
            internal static extern IntPtr udev_enumerate_new(IntPtr udev);
            [DllImport(libudev, EntryPoint = "udev_enumerate_ref")]
            internal static extern IntPtr udev_enumerate_ref(IntPtr enumerate);
            [DllImport(libudev, EntryPoint = "udev_enumerate_unref")]
            internal static extern void udev_enumerate_unref(IntPtr enumerate);
            [DllImport(libudev, EntryPoint = "udev_enumerate_add_match_subsystem")]
            internal static extern int udev_enumerate_add_match_subsystem(IntPtr enumerate,
                [MarshalAs(UnmanagedType.LPStr)] string subsystem);
            [DllImport(libudev, EntryPoint = "udev_enumerate_scan_devices")]
            internal static extern int udev_enumerate_scan_devices(IntPtr enumerate);
            [DllImport(libudev, EntryPoint = "udev_enumerate_get_list_entry")]
            internal static extern IntPtr udev_enumerate_get_list_entry(IntPtr enumerate);
            [DllImport(libudev, EntryPoint = "udev_list_entry_get_next")]
            internal static extern IntPtr udev_list_entry_get_next(IntPtr entry);

            [DllImport(libudev, EntryPoint = "udev_list_entry_get_name")]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
            internal static extern string udev_list_entry_get_name(IntPtr entry);

            [DllImport(libudev, EntryPoint = "udev_device_new_from_syspath")]
            internal static extern IntPtr udev_device_new_from_syspath(IntPtr udev,[MarshalAs(UnmanagedType.LPStr)] string syspath);

            [DllImport(libudev, EntryPoint = "udev_device_ref")]
            internal static extern IntPtr udev_device_ref(IntPtr device);

            [DllImport(libudev, EntryPoint = "udev_device_unref")]
            internal static extern void udev_device_unref(IntPtr device);

            [DllImport(libudev, EntryPoint = "udev_device_get_action")]
            /// <summary>
            /// Get device action 获取USB设备插拔动作,返回值: add,remove,bind,unbind
            /// </summary>
            /// <param name="udev_device"></param>
            /// <returns></returns>
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
            internal static extern string udev_device_get_action(IntPtr device);

            [DllImport(libudev, EntryPoint = "udev_device_get_devnode")]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
            internal static extern string udev_device_get_devnode(IntPtr device);

            [DllImport(libudev, EntryPoint = "udev_device_get_devtype")]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
            internal static extern string udev_device_get_devtype(IntPtr device);

            [DllImport(libudev, EntryPoint = "udev_device_get_sysname")]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
            internal static extern string udev_device_get_sysname(IntPtr device);
            
            [DllImport(libudev, EntryPoint = "udev_device_get_syspath")]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
            internal static extern string udev_device_get_syspath(IntPtr device);

            [DllImport(libudev, EntryPoint = "udev_device_get_subsystem")]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
            internal static extern string udev_device_get_subsystem(IntPtr device);
            
            [DllImport(libudev, EntryPoint = "udev_device_get_parent_with_subsystem_devtype")]
            internal static extern IntPtr udev_device_get_parent_with_subsystem_devtype(IntPtr device,
    [MarshalAs(UnmanagedType.LPStr)] string subsystem, [MarshalAs(UnmanagedType.LPStr)] string devtype);

            [DllImport(libudev, EntryPoint = "udev_device_get_sysattr_value")]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
            internal static extern string udev_device_get_sysattr_value(IntPtr device,
    [MarshalAs(UnmanagedType.LPStr)] string sysattr);

            [DllImport(libudev, EntryPoint = "udev_device_get_property_value")]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
            internal static extern string udev_device_get_property_value(IntPtr device,[MarshalAs(UnmanagedType.LPStr)] string sysattr);


            [DllImport(libudev, EntryPoint = "udev_device_get_is_initialized")]
            internal static extern int udev_device_get_is_initialized(IntPtr device);

        }

        public static class Libinput
        {
            const string libinput = "libinput.so.10";
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int OpenDeviceDelegate(string path, int flags, IntPtr user_data);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void CloseDeviceDelegate(int fd, IntPtr user_data);


            [StructLayout(LayoutKind.Sequential)]
            public struct libinput_interface
            {
                /**
                 * Open the device at the given path with the flags provided and
                 * return the fd.
                 *
                 * @param path The device path to open
                 * @param flags Flags as defined by open(2)
                 * @param user_data The user_data provided in
                 * libinput_udev_create_context()
                 *
                 * @return The file descriptor, or a negative errno on failure.
                 */
                public IntPtr open_restricted;
                // int (*open_restricted)(const char * path, int flags, void *user_data);
                /**
                 * Close the file descriptor.
                 *
                 * @param fd The file descriptor to close
                 * @param user_data The user_data provided in
                 * libinput_udev_create_context()
                 */
                public IntPtr close_restricted;
                // void (*close_restricted)(int fd, void *user_data);

                //public IntPtr log;               // 日志回调
            }
            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr libinput_udev_create_context(ref libinput_interface libinputInterface, IntPtr user_data, IntPtr udev);
            
            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern int libinput_udev_assign_seat(IntPtr libinput,[MarshalAs(UnmanagedType.LPStr)] string seat);

            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern void libinput_unref(IntPtr libinput);

            // 获取事件 fd，用于高效等待）
            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern int libinput_get_fd(IntPtr libinput);

            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern int libinput_dispatch(IntPtr libinput);

            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr libinput_get_event(IntPtr libinput);

            public enum EventType
            {
                None = 0,
                DeviceAdded,
                DeviceRemoved,

                KeyboardKey = 300,

                PointerMotion = 400,
                PointerMotionAbsolute,
                PointerButton,
                PointerAxis,
                PointerScrollWheel,
                PointerScrollFinger,
                PointerScrollContinuous,

                TouchDown = 500,
                TouchUp,
                TouchMotion,
                TouchCancel,
                TouchFrame,
                TabletToolAxis = 600,
                TabletToolProximity,
                TabletToolTip,
                TabletToolButton,
                TabletPadButton = 700,
                TabletPadRing,
                TabletPadStrip,
                TabletPadKey,

                GestureSwipeBegin = 800,
                GestureSwipeUpdate,
                GestureSwipeEnd,
                GesturePinchBegin,
                GesturePinchUpdate,
                GesturePinchEnd,
                GestureHoldBegin,
                GestureHoldEnd,
                SwitchToggle = 900,
            }
            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern EventType libinput_next_event_type(IntPtr libinput);
            

            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern void libinput_event_destroy(IntPtr inputEvent);

            /// <summary>
            ///  Event type for events returned by libinput_event_get_type().
            /// </summary>
            public enum libinput_event_type
            {
                /**
                 * This is not a real event type, and is only used to tell the user that
                 * no new event is available in the queue. See
                 * libinput_next_event_type().
                 */
                LIBINPUT_EVENT_NONE = 0,

                /**
                 * Signals that a device has been added to the context. The device will
                 * not be read until the next time the user calls libinput_dispatch()
                 * and data is available.
                 *
                 * This allows setting up initial device configuration before any events
                 * are created.
                 */
                LIBINPUT_EVENT_DEVICE_ADDED,

                /**
                 * Signals that a device has been removed. No more events from the
                 * associated device will be in the queue or be queued after this event.
                 */
                LIBINPUT_EVENT_DEVICE_REMOVED,

                LIBINPUT_EVENT_KEYBOARD_KEY = 300,

                LIBINPUT_EVENT_POINTER_MOTION = 400,
                LIBINPUT_EVENT_POINTER_MOTION_ABSOLUTE,
                LIBINPUT_EVENT_POINTER_BUTTON,
                LIBINPUT_EVENT_POINTER_AXIS,

                LIBINPUT_EVENT_TOUCH_DOWN = 500,
                LIBINPUT_EVENT_TOUCH_UP,
                LIBINPUT_EVENT_TOUCH_MOTION,
                LIBINPUT_EVENT_TOUCH_CANCEL,
                /**
                 * Signals the end of a set of touchpoints at one device sample
                 * time. This event has no coordinate information attached.
                 */
                LIBINPUT_EVENT_TOUCH_FRAME,

                /**
                 * One or more axes have changed state on a device with the @ref
                 * LIBINPUT_DEVICE_CAP_TABLET_TOOL capability. This event is only sent
                 * when the tool is in proximity, see @ref
                 * LIBINPUT_EVENT_TABLET_TOOL_PROXIMITY for details.
                 *
                 * The proximity event contains the initial state of the axis as the
                 * tool comes into proximity. An event of type @ref
                 * LIBINPUT_EVENT_TABLET_TOOL_AXIS is only sent when an axis value
                 * changes from this initial state. It is possible for a tool to
                 * enter and leave proximity without sending an event of type @ref
                 * LIBINPUT_EVENT_TABLET_TOOL_AXIS.
                 *
                 * An event of type @ref LIBINPUT_EVENT_TABLET_TOOL_AXIS is sent
                 * when the tip state does not change. See the documentation for
                 * @ref LIBINPUT_EVENT_TABLET_TOOL_TIP for more details.
                 *
                 * @since 1.2
                 */
                LIBINPUT_EVENT_TABLET_TOOL_AXIS = 600,
                /**
                 * Signals that a tool has come in or out of proximity of a device with
                 * the @ref LIBINPUT_DEVICE_CAP_TABLET_TOOL capability.
                 *
                 * Proximity events contain each of the current values for each axis,
                 * and these values may be extracted from them in the same way they are
                 * with @ref LIBINPUT_EVENT_TABLET_TOOL_AXIS events.
                 *
                 * Some tools may always be in proximity. For these tools, events of
                 * type @ref LIBINPUT_TABLET_TOOL_PROXIMITY_STATE_IN are sent only once after @ref
                 * LIBINPUT_EVENT_DEVICE_ADDED, and events of type @ref
                 * LIBINPUT_TABLET_TOOL_PROXIMITY_STATE_OUT are sent only once before @ref
                 * LIBINPUT_EVENT_DEVICE_REMOVED.
                 *
                 * If the tool that comes into proximity supports x/y coordinates,
                 * libinput guarantees that both x and y are set in the proximity
                 * event.
                 *
                 * When a tool goes out of proximity, the value of every axis should be
                 * assumed to have an undefined state and any buttons that are currently held
                 * down on the stylus are marked as released. Button release events for
                 * each button that was held down on the stylus are sent before the
                 * proximity out event.
                 *
                 * @since 1.2
                 */
                LIBINPUT_EVENT_TABLET_TOOL_PROXIMITY,
                /**
                 * Signals that a tool has come in contact with the surface of a
                 * device with the @ref LIBINPUT_DEVICE_CAP_TABLET_TOOL capability.
                 *
                 * On devices without distance proximity detection, the @ref
                 * LIBINPUT_EVENT_TABLET_TOOL_TIP is sent immediately after @ref
                 * LIBINPUT_EVENT_TABLET_TOOL_PROXIMITY for the tip down event, and
                 * immediately before for the tip up event.
                 *
                 * The decision when a tip touches the surface is device-dependent
                 * and may be derived from pressure data or other means. If the tip
                 * state is changed by axes changing state, the
                 * @ref LIBINPUT_EVENT_TABLET_TOOL_TIP event includes the changed
                 * axes and no additional axis event is sent for this state change.
                 * In other words, a caller must look at both @ref
                 * LIBINPUT_EVENT_TABLET_TOOL_AXIS and @ref
                 * LIBINPUT_EVENT_TABLET_TOOL_TIP events to know the current state
                 * of the axes.
                 *
                 * If a button state change occurs at the same time as a tip state
                 * change, the order of events is device-dependent.
                 *
                 * @since 1.2
                 */
                LIBINPUT_EVENT_TABLET_TOOL_TIP,
                /**
                 * Signals that a tool has changed a logical button state on a
                 * device with the @ref LIBINPUT_DEVICE_CAP_TABLET_TOOL capability.
                 *
                 * Button state changes occur on their own and do not include axis
                 * state changes. If button and axis state changes occur within the
                 * same logical hardware event, the order of the @ref
                 * LIBINPUT_EVENT_TABLET_TOOL_BUTTON and @ref
                 * LIBINPUT_EVENT_TABLET_TOOL_AXIS event is device-specific.
                 *
                 * This event is not to be confused with the button events emitted
                 * by the tablet pad. See @ref LIBINPUT_EVENT_TABLET_PAD_BUTTON.
                 *
                 * @see LIBINPUT_EVENT_TABLET_PAD_BUTTON
                 *
                 * @since 1.2
                 */
                LIBINPUT_EVENT_TABLET_TOOL_BUTTON,

                /**
                 * A button pressed on a device with the @ref
                 * LIBINPUT_DEVICE_CAP_TABLET_PAD capability.
                 *
                 * A button differs from @ref LIBINPUT_EVENT_TABLET_PAD_KEY in that
                 * buttons are sequentially indexed from 0 and do not carry any
                 * other information.  Keys have a specific functionality assigned
                 * to them. The key code thus carries a semantic meaning, a button
                 * number does not.
                 *
                 * This event is not to be confused with the button events emitted
                 * by tools on a tablet (@ref LIBINPUT_EVENT_TABLET_TOOL_BUTTON).
                 *
                 * @since 1.3
                 */
                LIBINPUT_EVENT_TABLET_PAD_BUTTON = 700,
                /**
                 * A status change on a tablet ring with the @ref
                 * LIBINPUT_DEVICE_CAP_TABLET_PAD capability.
                 *
                 * @since 1.3
                 */
                LIBINPUT_EVENT_TABLET_PAD_RING,

                /**
                 * A status change on a strip on a device with the @ref
                 * LIBINPUT_DEVICE_CAP_TABLET_PAD capability.
                 *
                 * @since 1.3
                 */
                LIBINPUT_EVENT_TABLET_PAD_STRIP,

                /**
                 * A key pressed on a device with the @ref
                 * LIBINPUT_DEVICE_CAP_TABLET_PAD capability.
                 *
                 * A key differs from @ref LIBINPUT_EVENT_TABLET_PAD_BUTTON in that
                 * keys have a specific functionality assigned to them (buttons are
                 * sequentially ordered). The key code thus carries a semantic
                 * meaning, a button number does not.
                 *
                 * @since 1.15
                 */
                LIBINPUT_EVENT_TABLET_PAD_KEY,

                LIBINPUT_EVENT_GESTURE_SWIPE_BEGIN = 800,
                LIBINPUT_EVENT_GESTURE_SWIPE_UPDATE,
                LIBINPUT_EVENT_GESTURE_SWIPE_END,
                LIBINPUT_EVENT_GESTURE_PINCH_BEGIN,
                LIBINPUT_EVENT_GESTURE_PINCH_UPDATE,
                LIBINPUT_EVENT_GESTURE_PINCH_END,

                /**
                 * @since 1.7
                 */
                LIBINPUT_EVENT_SWITCH_TOGGLE = 900,
            };
            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern libinput_event_type libinput_event_get_type(IntPtr inputEvent);


            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr libinput_event_get_device(IntPtr inputEvent);

            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr libinput_event_get_keyboard_event(IntPtr inputEvent);

            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern uint libinput_event_keyboard_get_key(IntPtr keyEvent);
            // 按键状态枚举
            public enum libinput_key_state
            {
                LIBINPUT_KEY_STATE_RELEASED = 0,
                LIBINPUT_KEY_STATE_PRESSED
            }
            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern libinput_key_state libinput_event_keyboard_get_key_state(IntPtr keyEvent);
            

            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
            public static extern string libinput_device_get_name(IntPtr device);

            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
            public static extern string libinput_device_get_sysname(IntPtr device);

            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern uint libinput_device_get_id_vendor(IntPtr device);


            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern uint libinput_device_get_id_product(IntPtr device);

            /// <summary>
            /// 返回 udev device handle
            /// </summary>
            /// <param name="device"></param>
            /// <returns></returns>
            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr libinput_device_get_udev_device(IntPtr device);

            /**
 * @ingroup device
 *
 * Capabilities on a device. A device may have one or more capabilities
 * at a time, capabilities remain static for the lifetime of the device.
 */
            public enum libinput_device_capability
            {
                LIBINPUT_DEVICE_CAP_KEYBOARD = 0,
                LIBINPUT_DEVICE_CAP_POINTER = 1,
                LIBINPUT_DEVICE_CAP_TOUCH = 2,
                LIBINPUT_DEVICE_CAP_TABLET_TOOL = 3,
                LIBINPUT_DEVICE_CAP_TABLET_PAD = 4,
                LIBINPUT_DEVICE_CAP_GESTURE = 5,
                LIBINPUT_DEVICE_CAP_SWITCH = 6,
            };
            [DllImport(libinput, CallingConvention = CallingConvention.Cdecl)]
            public static extern int libinput_device_has_capability(IntPtr device, libinput_device_capability capability);
            
        }

    }
}
