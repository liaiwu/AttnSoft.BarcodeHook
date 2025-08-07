using AttnSoft.BarcodeHook.RawInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttnSoft.BarcodeHook
{
    public class KeyboardHookService
    {
        public static IKeyboardHook GetService()
        {
            return RawDeviceInput.Instance;
        }
    }
}
