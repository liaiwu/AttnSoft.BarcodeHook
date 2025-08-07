using System;

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
    }
}