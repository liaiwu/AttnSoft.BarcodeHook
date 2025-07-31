using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AttnSoft.BarcodeHook
{
    internal class Win32
    {

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
    }
}
