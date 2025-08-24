using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AttnSoft.BarcodeHook
{
    public static class WinApiHelper
    {
        internal static string FormatHidPStatus(WinApi.Hid.HidPStatus status)
        {
            if (Enum.IsDefined(typeof(WinApi.Hid.HidPStatus),status))
            {
                return $"({status} hex={FormatErrorCode((int)status)} dec={(int)status})";
            }
            else
            {
                return $"(hex={FormatErrorCode((int)status)} dec={(int)status})";
            }
        }

        private static string FormatErrorCode(int code)
        {
            return "0x" + code.ToString("X2").PadLeft(8, '0');
        }

        public static string FormatIntPtr(IntPtr ptr)
        {
            return "0x" + ptr.ToInt64().ToString("X2").PadLeft(16, '0');
        }

        public static string FormatInt16(int value)
        {
            return "0x" + value.ToString("X2").PadLeft(4, '0');
        }
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
            if (WinApi.FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
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
    }
}