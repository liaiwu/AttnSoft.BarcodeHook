using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AttnSoft.BarcodeHook
{
    using static WinApi.User32;
    /// <summary>
    /// Delegate defining the method used to subsribe to the operating system keyboard events
    /// </summary>
    /// <param name="messageRaised">The System.Windows.Forms.Message object containing the
    /// necessary information about the key event</param>
    /// <returns>The subscriber should return true if the key was processed and should not be
    /// forwarded to the rest of the application</returns>
    internal delegate bool ManageKeyboardHookEvent(KeyboardMsg messageRaised);
    //internal delegate bool ManageKeyboardHookEvent(Message messageRaised);

    /// <summary>
    /// Structure used with keyboard hooks to manage low-level keyboard events
    /// </summary>
    internal struct EventMsg
    {
#pragma warning disable 0649 // The following fields are not explicitely assigned but decoded through interop so the warnings need to be suppressed
        /// <summary>
        /// A virtual-key code. The code must be a value in the range 1 to 254
        /// </summary>
        public Int32 vkCode;

        /// <summary>
        /// A hardware scan code for the key
        /// </summary>
        public Int32 scanCode;

        /// <summary>
        /// The extended-key flag, event-injected flags, context code, and transition-state flag
        /// </summary>
        public Int32 flags;

        /// <summary>
        /// The time stamp for this message, equivalent to what GetMessageTime would return for this message
        /// </summary>
        public Int32 time;

        /// <summary>
        /// Additional information associated with the message.
        /// </summary>
        public IntPtr dwExtraInfo;
#pragma warning disable 0649
    }

    /// <summary>
    /// This static class is used to hook the keyboard events, operating-system wise. That is, if the hook is active, all the
    /// key pressed even when the application doesn't have the focus are processed here.
    /// Parts of the software which want to intercept the keys should register here with the appropriate method which requires a
    /// delegate to be passed so that the event is raised to all the subscribers, until one processes the key received (returning
    /// true to the event); when this happens the subsequent subscribers will not receive the event notification
    /// </summary>
    internal class KeyboardHookHandler : IDisposable
    {


        private const int WH_KEYBOARD_LL = 13;



        /// <summary>
        /// The delegate used for the hook. Stored in a variable in order to avoid garbage collection
        /// </summary>
        /// DP2DO: don't know if this is still needed, since I made KeyboardHookCallback static
        private WinApi.User32.HookKeyboardProc hookCallback;

        internal event ManageKeyboardHookEvent? KeyboardHookEvent;
        /// <summary>
        /// The hook id.
        /// </summary>
        static private IntPtr keyboardHookID = IntPtr.Zero;

        int refCount = 0;

        internal static KeyboardHookHandler Instance=new KeyboardHookHandler();

        private KeyboardHookHandler()
        {
            hookCallback = KeyboardHookCallback;
        }

        /// <summary>
        /// Unhooks keyboard to restore standard system function keys management
        /// </summary>
        public void UnHookKeyboard()
        {
            lock (hookCallback)
            {
                Interlocked.Decrement(ref refCount);

                if (refCount <= 0 && keyboardHookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(keyboardHookID);
                    keyboardHookID = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Initialize the hook that intercept dangerous Windows keys
        /// </summary>
        public bool HookKeyboard()
        {
            lock (hookCallback)
            {
                if (keyboardHookID == IntPtr.Zero)
                {
                    var lpModuleName = Process.GetCurrentProcess().MainModule?.ModuleName;
                    IntPtr moduleHandle = WinApi.GetModuleHandle(lpModuleName);
                    if (moduleHandle == IntPtr.Zero)
                    {
                        Console.WriteLine("It was not possible to retrieve a moduleHandle: {0}", WinApiHelper.FormatMessage());
                        return false;
                    }
                    else
                    {
                        keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, hookCallback, moduleHandle, 0);
                        if (keyboardHookID == IntPtr.Zero)
                        {
                            Console.WriteLine("It was not possible to install the keyboard hook: {0}", WinApiHelper.FormatMessage());
                            return false;
                        }
                    }
                }
                Interlocked.Increment(ref refCount);
                return true;
            }
        }
        static IntPtr handledOk = (IntPtr)1;
        /// <summary>
        /// Intecept the keypresses
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam">The key event</param>
        /// <param name="lParam">The pointer to the structure that contains the virtual key value.</param>
        /// <returns></returns>
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                EventMsg? keyMsg = (EventMsg?)Marshal.PtrToStructure(lParam, typeof(EventMsg));
                if (keyMsg != null)
                {
                    KeyboardMsg msg = new KeyboardMsg()
                    {
                        Msg = (uint)wParam,
                        ScanCode = keyMsg.Value.scanCode,
                        VkCode = keyMsg.Value.vkCode
                    };
                    if (null != KeyboardHookEvent)
                    {
                        if (KeyboardHookEvent(msg))
                        {
                            return handledOk;
                        }
                    }
                }
            }
            return CallNextHookEx(keyboardHookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (keyboardHookID != IntPtr.Zero)
            {
                keyboardHookID = IntPtr.Zero;
                UnhookWindowsHookEx(keyboardHookID);
                refCount = 0;
            }
        }

    }
}
