
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.NetworkInformation;
using System.Collections.Concurrent;
using System.Diagnostics;


namespace BarcodeApp
{
    /// <summary>
    /// This class contains 'wraps' the Win32 APIs and constants.
    /// It also provides some helper functions that go at operating system level.
    /// </summary>
    public static class Win32
    {
        /// <summary>
        /// Performs bootstrap operations.
        /// </summary>
        public static void Initialize() { }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint FormatMessage(
          uint dwFlags,         // source and processing options
          IntPtr lpSource,        // message source
          int dwMessageId,      // message identifier
          uint dwLanguageId,    // language identifier
          StringBuilder lpBuffer, // message buffer
          int nSize,            // maximum size of message buffer
          IntPtr Arguments        // array of message inserts
          );

        /// <summary>
        /// Retrieve the error message of the last Win32 error.
        /// </summary>
        /// <returns>The error message for last error.</returns>
        public static string FormatMessage()
        {
            return FormatMessage(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Retrieve the error message of the given Win32 error
        /// </summary>
        /// <param name="dwMessageId">The Win32 error code</param>
        /// <returns>The error description for the given error code</returns>
        public static string FormatMessage(int dwMessageId)
        {
            const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
            const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
            StringBuilder msg = new StringBuilder(300);
            if (FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                              IntPtr.Zero,
                              dwMessageId,
                              0,
                              msg,
                              msg.Capacity,
                              IntPtr.Zero) > 0)
            {
                while (msg.Length > 0 && msg[msg.Length - 1] <= ' ')
                {
                    msg.Length--;
                }

                return msg.ToString();
            }

            return string.Format("Win32 error {0}", dwMessageId);
        }

        #region Flush Mouse messages queue
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParameter;
            public IntPtr LParameter;
            public uint Time;
            public System.Drawing.Point Location;
        }

        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out NativeMessage message, IntPtr handle, uint filterMin, uint filterMax, uint flags);
        private const uint WM_MOUSEFIRST = 0x0200;
        private const uint WM_MOUSELAST = 0x020D;
        private const uint WM_KEYFIRST = 0x0100;
        private const uint WM_KEYLAST = 0x0109;
        private const int PM_REMOVE = 0x0001;

        /// <summary>
        /// Flush all pending mouse events
        /// </summary>
        public static void FlushMouseMessages()
        {
            // Repeat until PeekMessage returns false.
            while (PeekMessage(out _, IntPtr.Zero, WM_MOUSEFIRST, WM_MOUSELAST, PM_REMOVE))
            {
                ;
            }
        }

        /// <summary>
        /// Flush all pending keyboard events
        /// </summary>
        public static void FlushKeyboardMessages()
        {
            // Repeat until PeekMessage returns false.
            while (PeekMessage(out _, IntPtr.Zero, WM_KEYFIRST, WM_KEYLAST, PM_REMOVE))
            {
                ;
            }
        }

        #endregion

        #region Privileges handling constants, structures and functions
        private const int SE_PRIVILEGE_ENABLED = 0x0002;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// TOKEN_PRIVILEGES is a quite complex 'C' structure that does not apply well to
        /// the marshalling made by PInvoke.
        /// In order to simplify it, TOKEN_PRIVILEGES_AND1LUID, simplifies the following 'C' structures
        /// <code>
        /// typedef struct _LUID
        /// {
        ///   DWORD LowPart;  LONG HighPart;
        ///  } LUID, *PLUID;
        /// typedef struct _LUID_AND_ATTRIBUTES
        /// {
        ///   LUID Luid;  DWORD Attributes;
        /// } LUID_AND_ATTRIBUTES, *PLUID_AND_ATTRIBUTES;
        /// typedef struct _TOKEN_PRIVILEGES
        /// {
        ///  DWORD PrivilegeCount;  LUID_AND_ATTRIBUTES Privileges[1];
        /// } TOKEN_PRIVILEGES, *PTOKEN_PRIVILEGES;
        /// </code>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TOKEN_PRIVILEGES_AND1LUID
        {
            /// <summary>
            /// The number of privileges present in the structure: always 1
            /// </summary>
            public uint PrivilegeCount;

            /// <summary>
            /// The permission UID
            /// </summary>
            public ulong LUID;

            /// <summary>
            /// The permission attribute
            /// </summary>
            public uint Attribute;
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref ulong lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
                                   ref TOKEN_PRIVILEGES_AND1LUID NewState,
                                   UInt32 BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        /// <summary>
        /// Enable the privileges to the current process.
        /// </summary>
        /// <param name="privilegeName">The name of the privilege</param>
        /// <returns>Enable successful</returns>
        private static void SetPrivileges(string privilegeName)
        {
            TOKEN_PRIVILEGES_AND1LUID tkp = new TOKEN_PRIVILEGES_AND1LUID();

            if (OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var hToken))
            {
                if (LookupPrivilegeValue(null, privilegeName, ref tkp.LUID))
                {
                    tkp.PrivilegeCount = 1;
                    tkp.Attribute = SE_PRIVILEGE_ENABLED;

                    if (AdjustTokenPrivileges(hToken, false, ref tkp, 0, IntPtr.Zero, IntPtr.Zero))
                    {
                    }
                }
            }
        }

        #endregion

        #region SetLocalTime structure and functions
        /// <summary>
        /// The SYSTEMTIME structure represents a date and time using individual members for
        /// the month, day, year, weekday, hour, minute, second, and millisecond.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            /// <summary></summary>
            public ushort wYear;

            /// <summary></summary>
            public ushort wMonth;

            /// <summary></summary>
            public ushort wDayOfWeek;

            /// <summary></summary>
            public ushort wDay;

            /// <summary></summary>
            public ushort wHour;

            /// <summary></summary>
            public ushort wMinute;

            /// <summary></summary>
            public ushort wSecond;

            /// <summary></summary>
            public ushort wMilliseconds;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetLocalTime(ref SYSTEMTIME time);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        /// <summary>
        /// Sets the system date and time.
        /// Before setting it, the privileges for this operation are granted to the process.
        /// </summary>
        /// <param name="dt">The date and time to be assigned to the system.</param>
        /// <returns>The Kernel error if any, 0 if there was no errors</returns>
        public static uint SetDateTime(DateTime dt)
        {
            SYSTEMTIME tm = new SYSTEMTIME();

            tm.wDay = (ushort)dt.Day;
            tm.wMonth = (ushort)dt.Month;
            tm.wYear = (ushort)dt.Year;
            tm.wHour = (ushort)dt.Hour;
            tm.wMinute = (ushort)dt.Minute;
            tm.wSecond = (ushort)dt.Second;
            tm.wMilliseconds = 0;
            SetPrivileges("SeSystemtimePrivilege");
            SetLocalTime(ref tm);
            return GetLastError();
        }

        /// <summary>
        /// Set the computer time zone
        /// </summary>
        /// <param name="timezoneID">The Identifier of the time zone</param>
        /// <returns>The tzutil.exe exit code. -1 if tzutil.exe is not available</returns>
        public static int SetTimeZone(string timezoneID)
        {
            int exitCode = -1;
            // Invoke the tzutil program, that accepts the time zone as
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "tzutil.exe",
                Arguments = "/s \"" + timezoneID + "\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process != null)
            {
                process.WaitForExit();
                exitCode = process.ExitCode;
                TimeZoneInfo.ClearCachedData();
            }

            return exitCode;
        }
        #endregion

        #region Shutdown and reboot

        private const uint EWX_SHUTDOWN = 1;
        private const uint EWX_REBOOT = 2;
        private const uint EWX_FORCE = 4;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ExitWindowsEx(uint uFlags, uint dwReserved);

        /// <summary>
        /// Shutdown the computer.
        /// Before shutting down, the privileges for this operation are granted to the process.
        /// </summary>
        public static void Shutdown()
        {
            SetPrivileges("SeShutdownPrivilege");
            ExitWindowsEx(EWX_SHUTDOWN | EWX_FORCE, 0);
        }

        /// <summary>
        /// Reboot the computer.
        /// Before rebooting, the privileges for this operation are granted to the process.
        /// </summary>
        public static void Reboot()
        {
            SetPrivileges("SeShutdownPrivilege");
            ExitWindowsEx(EWX_REBOOT | EWX_FORCE, 0);
        }
        #endregion

        #region Power Management events

        public const int WM_POWERBROADCAST = 0x0218;

        public static Guid GUID_ACDC_POWER_SOURCE = new Guid("5d3e9a59-e9D5-4b00-a6bd-ff34ff516548");
        public static Guid GUID_BATTERY_PERCENTAGE_REMAINING = new Guid("a7ad8041-b45a-4cae-87a3-eecbb468a9e1");
        public static Guid GUID_CONSOLE_DISPLAY_STATE = new Guid("6fe69556-704a-47a0-8f24-c28d936fda47");
        public static Guid GUID_GLOBAL_USER_PRESENCE = new Guid("786E8A1D-B427-4344-9207-09E70BDCBEA9");
        public static Guid GUID_IDLE_BACKGROUND_TASK = new Guid("515c31d8-f734-163d-a0fd-11a08c91e8f1");
        public static Guid GUID_MONITOR_POWER_ON = new Guid("02731015-4510-4526-99e6-e5a17ebd1aea");
        public static Guid GUID_POWER_SAVING_STATUS = new Guid("E00958C0-C213-4ACE-AC77-FECCED2EEEA5");
        public static Guid GUID_POWERSCHEME_PERSONALITY = new Guid("245d8541-3943-4422-b025-13A784F679B7");
        public static Guid GUID_MIN_POWER_SAVINGS = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        public static Guid GUID_MAX_POWER_SAVINGS = new Guid("a1841308-3541-4fab-bc81-f71556f20b4a");
        public static Guid GUID_TYPICAL_POWER_SAVINGS = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");
        public static Guid GUID_SESSION_DISPLAY_STATUS = new Guid("2B84C20E-AD23-4ddf-93DB-05FFBD7EFCA5");
        public static Guid GUID_SESSION_USER_PRESENCE = new Guid("3C0F4548-C03F-4c4d-B9F2-237EDE686376");
        public static Guid GUID_SYSTEM_AWAYMODE = new Guid("98a7f580-01f7-48aa-9c0f-44352c29e5C0");

        // Win32 decls and defs
        //
        const int PBT_APMQUERYSUSPEND = 0x0000;
        const int PBT_APMQUERYSTANDBY = 0x0001;
        const int PBT_APMQUERYSUSPENDFAILED = 0x0002;
        const int PBT_APMQUERYSTANDBYFAILED = 0x0003;
        const int PBT_APMSUSPEND = 0x0004;
        const int PBT_APMSTANDBY = 0x0005;
        const int PBT_APMRESUMECRITICAL = 0x0006;
        const int PBT_APMRESUMESUSPEND = 0x0007;
        const int PBT_APMRESUMESTANDBY = 0x0008;
        const int PBT_APMBATTERYLOW = 0x0009;
        const int PBT_APMPOWERSTATUSCHANGE = 0x000A; // power status
        const int PBT_APMOEMEVENT = 0x000B;
        const int PBT_APMRESUMEAUTOMATIC = 0x0012;
        public const int PBT_POWERSETTINGCHANGE = 0x8013; // DPPE

        const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
        const int DEVICE_NOTIFY_SERVICE_HANDLE = 0x00000001;

        /// <summary>
        /// The current monitor's display state
        /// </summary>
        public enum MonitorDisplayState : int
        {
            /// <summary>
            /// The display is off.
            /// </summary>
            Off = 0,

            /// <summary>
            /// The display is on.
            /// </summary>
            On = 1,

            /// <summary>
            /// The display is dimmed.
            /// </summary>
            Dimmed = 2
        }

        // This structure is sent when the PBT_POWERSETTINGSCHANGE message is sent.
        // It describes the power setting that has changed and contains data about the change
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public uint DataLength;
            public byte Data;
        }

        [DllImport(@"User32", SetLastError = true, EntryPoint = "RegisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, Int32 Flags);

        /// <summary>
        /// RegisterForPowerNotifications
        /// </summary>
        /// <param name="hwnd"></param>
        public static void RegisterForPowerNotifications(IntPtr hwnd)
        {
            RegisterPowerSettingNotification(hwnd, ref GUID_ACDC_POWER_SOURCE, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_BATTERY_PERCENTAGE_REMAINING, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_CONSOLE_DISPLAY_STATE, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_GLOBAL_USER_PRESENCE, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_IDLE_BACKGROUND_TASK, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_MONITOR_POWER_ON, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_POWER_SAVING_STATUS, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_POWERSCHEME_PERSONALITY, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_MIN_POWER_SAVINGS, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_MAX_POWER_SAVINGS, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_TYPICAL_POWER_SAVINGS, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_SESSION_DISPLAY_STATUS, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_SESSION_USER_PRESENCE, DEVICE_NOTIFY_WINDOW_HANDLE);
            RegisterPowerSettingNotification(hwnd, ref GUID_SYSTEM_AWAYMODE, DEVICE_NOTIFY_WINDOW_HANDLE);
        }

        #endregion

        #region Windows messages constants

        /// <summary>
        /// A window has been activated or deactivated
        /// </summary>
        public const int WM_ACTIVATE = 0x0006;
        // Key message constants

        /// <summary>
        /// The WM_KEYDOWN message is posted to the window with the keyboard focus when a
        /// nonsystem key is pressed. A nonsystem key is a key that is pressed when the
        /// ALT key is not pressed.
        /// </summary>
        public const int WM_KEYDOWN = 0x0100;

        /// <summary>
        /// The WM_SYSKEYDOWN message is posted to the window with the keyboard focus when
        /// the user presses the F10 key (which activates the menu bar) or holds down the
        /// ALT key and then presses another key. It also occurs when no window currently
        /// has the keyboard focus; in this case, the WM_SYSKEYDOWN message is sent to the
        /// active window. The window that receives the message can distinguish between these
        /// two contexts by checking the context code in the lParam parameter
        /// </summary>
        public const int WM_SYSKEYDOWN = 0x0104;

        /// <summary>
        /// This message is posted to the window with the keyboard focus when a nonsystem key
        /// is released. A nonsystem key is a key that is pressed when the ALT key is not
        /// pressed, or a keyboard key that is pressed when a window has the keyboard focus.
        /// </summary>
        public const int WM_KEYUP = 0x0101;

        /// <summary>
        /// This message is posted after a
        /// </summary>
        public const int WM_CHAR = 0x0102;
        // public const int WM_DEADCHAR    = 0x0103;

        /// <summary>
        /// This message is posted to the window with the keyboard focus when the user
        /// releases a key that was pressed while the ALT key was held down.
        /// It also occurs when no window currently has the keyboard focus; in this case,
        /// the WM_SYSKEYUP message is sent to the active window.
        /// The window that receives the message can distinguish between these
        /// two contexts by checking the context code in the lKeyData parameter.
        /// </summary>
        public const int WM_SYSKEYUP = 0x0105;

        // public const int WM_SYSCHAR     = 0x0106;
        // public const int WM_SYSDEADCHAR = 0x0107;
        // public const int WM_UNICHAR     = 0x0109;

        /// <summary>
        /// The WM_LBUTTONDOWN message is posted when the user presses the left mouse button
        /// while the cursor is in the client area of a window. If the mouse is not captured,
        /// the message is posted to the window beneath the cursor. Otherwise, the message
        /// is posted to the window that has captured the mouse.
        /// </summary>
        public const int WM_LBUTTONDOWN = 0x0201;

        /// <summary>
        /// The WM_LBUTTONUP message is posted when the user releases the left mouse button
        /// while the cursor is in the client area of a window.
        /// If the mouse is not captured, the message is posted to the window beneath the cursor.
        /// Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int WM_LBUTTONUP = 0x0202;

        // public const int WM_RBUTTONDOWN = 0x0204;
        // public const int WM_RBUTTONUP   = 0x0205;

        /// <summary>
        /// Left shift key scancode;
        /// </summary>
        public const int VK_SHIFT_LEFT = 0x2A;

        /// <summary>
        /// Left control key scancode
        /// </summary>
        public const int VK_CTRL_LEFT = 0x1D;
        #endregion

        #region RAS routines

        private const string rasapidll = "rasapi32.dll";

        /// <summary>
        /// The RASDIALPARAMS structure contains parameters that are used by RasDial to establish a remote access connection.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct RASDIALPARAMS
        {
            private const int RAS_MaxEntryName = 256;
            private const int RAS_MaxPhoneNumber = 128;
            private const int RAS_MaxCallbackNumber = RAS_MaxPhoneNumber;
            private const int UNLEN = 256;
            private const int PWLEN = 256;
            private const int DNLEN = 15;

            /// <summary>
            /// Specifies the structure size, in bytes.
            /// </summary>
            public int dwSize;

            /// <summary>
            /// Specifies a string that contains the phone-book entry to use to establish the connection.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS_MaxEntryName + 1)]
            public string szEntryName;

            /// <summary>
            ///
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS_MaxPhoneNumber + 1)]
            public string szPhoneNumber;

            /// <summary>
            /// Specifies a string that contains a callback phone number.
            /// An empty string ("") indicates that callback should not be used.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS_MaxCallbackNumber + 1)]
            public string szCallbackNumber;

            /// <summary>
            /// Specifies a string that contains the user's user name.
            /// This string is used to authenticate the user's access to the remote access server.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = UNLEN + 1)]
            public string szUserName;

            /// <summary>
            /// Specifies a string that contains the user's password.
            /// This string is used to authenticate the user's access to the remote access server.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PWLEN + 1)]
            public string szPassword;

            /// <summary>
            /// Specifies a string that contains the domain on which authentication is to occur.
            /// An empty string ("") specifies the domain in which the remote access server is a member.
            /// An asterisk specifies the domain stored in the phone book for the entry.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = DNLEN + 1)]
            public string szDomain;
        }

        /// <summary>
        /// The RasGetErrorString function obtains an error message string for a specified RAS error value.
        /// </summary>
        /// <param name="uErrorValue">Specifies the error value of interest.
        /// These are values returned by one of the RAS functions: those listed in the RasError.h header file.</param>
        /// <param name="lpszErrorString">Pointer to a buffer that receives the error string.</param>
        /// <param name="cBufSize">Specifies the size, in characters, of the buffer pointed to by lpszErrorString.</param>
        /// <returns>If the function succeeds, the return value is zero.</returns>
        [DllImport(rasapidll, CharSet = CharSet.Auto)]
        private extern static uint RasGetErrorString(uint uErrorValue, StringBuilder lpszErrorString, int cBufSize);

        /// <summary>
        /// The RasDial function establishes a RAS connection between a RAS client and a RAS server.
        /// The connection data includes callback and user-authentication information.
        /// </summary>
        /// <param name="lpRasDialExtensions"></param>
        /// <param name="lpszPhonebook"></param>
        /// <param name="lpRasDialParams">Pointer to a RASDIALPARAMS structure that specifies
        /// calling parameters for the RAS connection.
        /// The caller must set the RASDIALPARAMS structure's dwSize member to sizeof(RASDIALPARAMS)
        /// to identify the version of the structure being passed.</param>
        /// <param name="dwNotifierType"></param>
        /// <param name="lpvNotifier"></param>
        /// <param name="lphRasConn">Pointer to a variable of type HRASCONN.
        /// Set the HRASCONN variable to NULL before calling RasDial. If RasDial succeeds,
        /// it stores a handle to the RAS connection into *lphRasConn.</param>
        /// <returns>If the function succeeds, the immediate return value is zero.
        /// In addition, the function stores a handle to the RAS connection
        /// into the variable pointed to by lphRasConn.
        ///
        /// If the function fails, the immediate return value is a nonzero error value,
        /// either from the set listed in the RasError.h header file or ERROR_NOT_ENOUGH_MEMORY.
        /// </returns>
        [DllImport(rasapidll, CharSet = CharSet.Auto)]
        private extern static uint RasDial(
          IntPtr lpRasDialExtensions,
          IntPtr lpszPhonebook,
          ref RASDIALPARAMS lpRasDialParams,
          uint dwNotifierType,
          IntPtr lpvNotifier,
          ref IntPtr lphRasConn
          );

        /// <summary>
        /// The RasHangUp function terminates a remote access connection.
        /// The connection is specified with a RAS connection handle.
        /// The function releases all RASAPI32.DLL resources associated with the handle.
        /// </summary>
        /// <param name="hRasConn">Specifies the remote access connection to terminate.
        /// This is a handle returned from a previous call to RasDial</param>
        /// <returns>If the function succeeds, the return value is zero.
        /// If the function fails, the return value is a nonzero error value
        /// listed in the RasError.h header file, or ERROR_INVALID_HANDLE.
        /// </returns>
        [DllImport(rasapidll, CharSet = CharSet.Auto)]
        public extern static uint RasHangUp(IntPtr hRasConn);

        /// <summary>
        /// The RasDial function establishes a RAS connection between a RAS client and a RAS server.
        /// The connection data includes user-authentication information.
        /// </summary>
        /// <param name="userName">The name of the user authenticated by the remote access server.</param>
        /// <param name="password">The password of the user authenticated in the remote access server.</param>
        /// <param name="phonebookEntry">Specifies a string that contains the phone-book entry to use
        /// to establish the connection.</param>
        /// <param name="errorDescription">In case of error, the code and the description of the error.</param>
        /// <returns>If success, a handle to the RAS connection. In case of failure, IntPtr.Zero is returned and errorDescription contains the error text.</returns>
        public static IntPtr RasDial(string userName, string password, string phonebookEntry, out string errorDescription)
        {
            IntPtr hRasConn = IntPtr.Zero;
            RASDIALPARAMS rasParams = new RASDIALPARAMS();
            rasParams.dwSize = Marshal.SizeOf(typeof(RASDIALPARAMS));
            rasParams.szUserName = userName;
            rasParams.szPassword = password;
            rasParams.szEntryName = phonebookEntry;
            rasParams.szDomain = "*";

            uint rc = RasDial(IntPtr.Zero, IntPtr.Zero, ref rasParams, 0, IntPtr.Zero, ref hRasConn);
            if (rc != 0)
            {
                errorDescription = string.Format("RasDial error {0}\n", rc);
                StringBuilder msg = new StringBuilder(300);
                if (RasGetErrorString(rc, msg, msg.Capacity) == 0)
                {
                    errorDescription += msg.ToString();
                }
                else
                {
                    errorDescription += "Unknown error";
                }

                RasHangUp(hRasConn);
                hRasConn = IntPtr.Zero;
            }
            else
            {
                errorDescription = "";
            }

            return hRasConn;
        }
        #endregion

        #region Port I/O routines

        /// <summary>
        /// The CreateFile function creates or opens a file, directory, physical disk, volume,
        /// console buffer, tape drive, communications resource, mailslot, or pipe.
        /// The function returns a handle that can be used to access the object.
        /// </summary>
        /// <param name="lpFileName">The file name.</param>
        /// <param name="dwDesiredAccess">Access to the object.</param>
        /// <param name="dwShareMode">Sharing mode of the object.</param>
        /// <param name="lpSecurityAttributes"></param>
        /// <param name="dwCreationDisposition"></param>
        /// <param name="dwFlagsAndAttributes"></param>
        /// <param name="hTemplateFile"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
          IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes,
          IntPtr hTemplateFile);

        /// <summary>
        /// This constant is the value returned by CreateFile in case of error.
        /// It can also be used to define a file handler as closed.
        /// </summary>
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        /// <summary>
        /// Opens the file. The function fails if the file does not exist.
        /// </summary>
        private const uint OPEN_EXISTING = 3;

        /// <summary>
        /// Constant used by CreateFile function
        /// </summary>
        private const int FILE_SHARE_READ = 1;

        /// <summary>
        /// Constant used by CreateFile function
        /// </summary>
        private const int FILE_SHARE_WRITE = 2;

        /// <summary>
        /// Open file for input.
        /// </summary>
        private const uint GENERIC_READ = 0x80000000;

        /// <summary>
        /// Open file for output.
        /// </summary>
        private const uint GENERIC_WRITE = 0x40000000;

        /// <summary>
        /// The CloseHandle function closes an open object handle.
        /// </summary>
        /// <param name="hObject">Handle to an open object.</param>
        /// <returns>Successful close</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);


        /// <summary>
        /// This strcuture is used to communicate with the device driver.
        /// It uses only the PortNumber field for the Inp call, and 5 for the Out.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct GENPORT_INPUT
        {
            public uint PortNumber;     // Port # to write to or read from
            public byte data;           // value to be written
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(IntPtr hDevice,
          uint dwIoControlCode,
          ref GENPORT_INPUT lpInBuffer,
          uint nInBufferSize,
          out byte lpOutBuffer,
          uint nOutBufferSize,
          out uint lpBytesReturned,
          IntPtr lpOverlapped
          );

        /// <summary>
        /// Open the device driver 'file' that will be used by instruction PortInp and PortOutp
        /// </summary>
        /// <param name="openWrite">When false, the handle is open for by PortInp (read), when true, by PortOutp (write)</param>
        /// <returns></returns>
        private static IntPtr OpenPortDevice(bool openWrite)
        {
            uint desiredAccess = GENERIC_READ;
            uint shareMode = FILE_SHARE_READ;
            if (openWrite)
            {
                desiredAccess = GENERIC_WRITE;
                shareMode = FILE_SHARE_WRITE;
            }

            IntPtr deviceHandle = CreateFile(
              @"\\.\SniPort",
              desiredAccess,
              shareMode,
              IntPtr.Zero,
              OPEN_EXISTING,
              0,
              IntPtr.Zero);

            if (deviceHandle == INVALID_HANDLE_VALUE)        // Was the device opened?
            {
                throw new ApplicationException("Unable to open the SniPort device.");
            }

            return deviceHandle;
        }

        /// <summary>
        /// Execute the 8 bit Inp instruction on a I/O port.
        /// </summary>
        /// <param name="port">The port offset in the address space of the SniPort device driver.
        /// The first port has offset 0, the second 1 and so on.
        /// The first port value and the number of ports assigned to the SniPort device driver are specified in its registry entries.</param>
        /// <returns>The 8 bit value present at the given port.</returns>
        public static byte PortInp(uint port)
        {
            byte byteValue;
            IntPtr deviceHandle = OpenPortDevice(false);
            try
            {
                const uint IOCTL_GPD_READ_PORT_UCHAR = 0x9C406400;

                GENPORT_INPUT portStruct = new GENPORT_INPUT();
                portStruct.PortNumber = port;

                if (!DeviceIoControl(
                  deviceHandle,               // Handle to device
                  IOCTL_GPD_READ_PORT_UCHAR,  // IO Control code for Read
                  ref portStruct,             // Buffer to driver.
                  4,                          // Length of buffer in bytes: use only the port part
                  out byteValue,              // Buffer from driver.
                  1,                          // Length of buffer in bytes.
                  out _,                      // Bytes placed in DataBuffer.
                  IntPtr.Zero                 // NULL means wait till operation completes.
                  ))
                {
                    throw new ApplicationException($"PortInp failed with code {Marshal.GetLastWin32Error()}");
                }
            }
            finally
            {
                CloseHandle(deviceHandle);
            }

            return byteValue;
        }

        /// <summary>
        /// Execute the 8 bit Outp instruction on a I/O port.
        /// </summary>
        /// <param name="port">The port offset in the address space of the SniPort device driver.
        /// The first port has offset 0, the second 1 and so on.
        /// The first port value and the number of ports assigned to the SniPort device driver are specified in its registry entries.</param>
        /// <param name="value">The value sent to the port</param>
        /// <returns>The 8 bit value present at the given port.</returns>
        public static void PortOutp(uint port, uint value)
        {
            IntPtr deviceHandle = OpenPortDevice(true);
            try
            {
                const uint IOCTL_GPD_WRITE_PORT_UCHAR = 0x9C40A440;

                GENPORT_INPUT portStruct = new GENPORT_INPUT();
                portStruct.PortNumber = port;
                portStruct.data = (byte)value;

                if (!DeviceIoControl(
                  deviceHandle,               // Handle to device
                  IOCTL_GPD_WRITE_PORT_UCHAR,  // IO Control code for Read
                  ref portStruct,             // Buffer to driver.
                  5,                          // Length of buffer in bytes: use address and value
                  out _,                      // Buffer from driver.
                  0,                          // Length of buffer in bytes.
                  out _,                      // Bytes placed in DataBuffer.
                  IntPtr.Zero                 // NULL means wait till operation completes.
                  ))
                {
                    throw new ApplicationException($"PortOutp failed with code {Marshal.GetLastWin32Error()}");
                }
            }
            finally
            {
                CloseHandle(deviceHandle);
            }
        }
        #endregion
    }
}
