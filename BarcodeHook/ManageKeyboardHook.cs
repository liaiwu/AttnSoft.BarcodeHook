namespace BarcodeApp
{
    /// <summary>
    /// Delegate defining the method used to subsribe to the operating system keyboard events
    /// </summary>
    /// <param name="messageRaised">The System.Windows.Forms.Message object containing the
    /// necessary information about the key event</param>
    /// <returns>The subscriber should return true if the key was processed and should not be
    /// forwarded to the rest of the application</returns>
    public delegate bool ManageKeyboardHookEvent(Message messageRaised);

    /// <summary>
    /// This static class is used to hook the keyboard events, operating-system wise. That is, if the hook is active, all the
    /// key pressed even when the application doesn't have the focus are processed here.
    /// Parts of the software which want to intercept the keys should register here with the appropriate method which requires a
    /// delegate to be passed so that the event is raised to all the subscribers, until one processes the key received (returning
    /// true to the event); when this happens the subsequent subscribers will not receive the event notification
    /// </summary>
    public static class KeyboardHookHandler
    {
        /// <summary>
        /// An application-defined or library-defined callback function used with the SetWindowsHookEx function.
        /// The system calls this function every time a new keyboard input event is about to be posted into a thread input queue.
        /// </summary>
        /// <param name="nCode">A code the hook procedure uses to determine how to process the message.
        /// If nCode is less than zero, the hook procedure must pass the message to the CallNextHookEx function without further processing and should return the value returned by CallNextHookEx.
        /// This parameter can be one of the following values:
        /// HC_ACTION = 0
        /// The wParam and lParam parameters contain information about a keyboard message.</param>
        /// <param name="wParam">The identifier of the keyboard message.
        /// This parameter can be one of the following messages: WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP.</param>
        /// <param name="lParam">A pointer to a KBDLLHOOKSTRUCT structure.</param>
        /// <returns>If nCode is less than zero, the hook procedure must return the value returned by CallNextHookEx.
        /// If nCode is greater than or equal to zero, and the hook procedure did not process the message,
        /// it is highly recommended that you call CallNextHookEx and return the value it returns;
        /// otherwise, other applications that have installed WH_KEYBOARD_LL hooks will not receive hook notifications and may behave incorrectly as a result.
        /// If the hook procedure processed the message, it may return a nonzero value to prevent the system from passing the message to the rest of the hook chain
        /// or the target window procedure.</returns>
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;

        /// <summary>
        /// Structure used with keyboard hooks to manage low-level keyboard events
        /// </summary>
        private struct KBDLLHOOKSTRUCT
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
        /// Installs an application-defined hook procedure into a hook chain.
        /// You would install a hook procedure to monitor the system for certain types of events.
        /// These events are associated either with a specific thread or with all threads in the same desktop as the calling thread.
        /// </summary>
        /// <param name="idHook">The type of hook procedure to be installed. This parameter can be one of the following values:
        /// WH_KEYBOARD_LL = 13
        /// Installs a hook procedure that monitors low-level keyboard input events. For more information, see the LowLevelKeyboardProc hook procedure.</param>
        /// <param name="lpfn">A pointer to the hook procedure.
        /// If the dwThreadId parameter is zero or specifies the identifier of a thread created by a different process,
        /// the lpfn parameter must point to a hook procedure in a DLL.
        /// Otherwise, lpfn can point to a hook procedure in the code associated with the current process.</param>
        /// <param name="hMod"></param>
        /// <param name="dwThreadId">The identifier of the thread with which the hook procedure is to be associated.
        /// For desktop apps, if this parameter is zero, the hook procedure is associated with all existing threads running in the same desktop as the calling thread.</param>
        /// <returns>If the function succeeds, the return value is the handle to the hook procedure.
        /// If the function fails, the return value is NULL. To get extended error information, call GetLastError.</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        /// <summary>
        /// Removes a hook procedure installed in a hook chain by the SetWindowsHookEx function.
        /// </summary>
        /// <param name="hhk">A handle to the hook to be removed. This parameter is a hook handle obtained by a previous call to SetWindowsHookEx.</param>
        /// <returns>Success</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// Passes the hook information to the next hook procedure in the current hook chain.
        /// A hook procedure can call this function either before or after processing the hook information.
        /// </summary>
        /// <param name="hhk">This parameter is ignored.</param>
        /// <param name="nCode">The hook code passed to the current hook procedure.
        /// The next hook procedure uses this code to determine how to process the hook information.</param>
        /// <param name="wParam">The wParam value passed to the current hook procedure.
        /// The meaning of this parameter depends on the type of hook associated with the current hook chain.</param>
        /// <param name="lParam">The lParam value passed to the current hook procedure.
        /// The meaning of this parameter depends on the type of hook associated with the current hook chain.</param>
        /// <returns>his value is returned by the next hook procedure in the chain. The current hook procedure must also return this value.
        /// The meaning of the return value depends on the hook type. For more information, see the descriptions of the individual hook procedures.</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Retrieves a module handle for the specified module. The module must have been loaded by the calling process.
        /// </summary>
        /// <param name="lpModuleName">The name of the loaded module (either a .dll or .exe file).
        /// If the file name extension is omitted, the default library extension .dll is appended.
        /// The file name string can include a trailing point character (.) to indicate that the module name has no extension.
        /// The string does not have to specify a path. When specifying a path, be sure to use backslashes (\), not forward slashes (/).
        /// The name is compared (case independently) to the names of modules currently mapped into the address space of the calling process.
        /// If this parameter is NULL, GetModuleHandle returns a handle to the file used to create the calling process (.exe file).</param>
        /// <returns>If the function succeeds, the return value is a handle to the specified module.
        /// If the function fails, the return value is NULL. To get extended error information, call GetLastError.</returns>
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// The delegate used for the hook. Stored in a variable in order to avoid garbage collection
        /// </summary>
        /// DP2DO: don't know if this is still needed, since I made KeyboardHookCallback static
        static private LowLevelKeyboardProc hookCallback = KeyboardHookCallback;

        /// <summary>
        /// The hook id.
        /// </summary>
        static private IntPtr keyboardHookID = IntPtr.Zero;

        /// <summary>
        /// Unhooks keyboard to restore standard system function keys management
        /// </summary>
        static public void UnHookKeyboard()
        {
            lock (hookCallback)
            {
                if (keyboardHookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(keyboardHookID);
                    keyboardHookID = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Initialize the hook that intercept dangerous Windows keys
        /// </summary>
        static public void HookKeyboard()
        {
            lock (hookCallback)
            {
                IntPtr moduleHandle = GetModuleHandle(null);
                if (moduleHandle == IntPtr.Zero)
                {
                    Console.WriteLine("It was not possible to retrieve a moduleHandle: {0}", Win32.FormatMessage());
                }
                else
                {
                    keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, hookCallback, moduleHandle, 0);
                    if (keyboardHookID == IntPtr.Zero)
                    {
                        Console.WriteLine("It was not possible to install the keyboard hook: {0}", Win32.FormatMessage());
                    }
                }
            }
        }

        /// <summary>
        /// Intecept the keypresses
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam">The key event</param>
        /// <param name="lParam">The pointer to the structure that contains the virtual key value.</param>
        /// <returns></returns>
        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Return this value in case the event is handled
            IntPtr handledRc = (IntPtr)1;

            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT kbd = (KBDLLHOOKSTRUCT)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                Message msg = new Message();
                msg.Msg = (int)wParam;
                Int64 newLParam = (Int64)kbd.scanCode;
                newLParam <<= 16;
                msg.LParam = new IntPtr(newLParam);
                msg.WParam = new IntPtr((Int64)kbd.vkCode);

                if (registeredDelegates != null && registeredDelegates.Count > 0)
                {
                    foreach (ManageKeyboardHookEvent del in registeredDelegates)
                    {
                        if (del(msg))
                        {
                            return handledRc;
                        }
                    }
                }
            }

            return CallNextHookEx(keyboardHookID, nCode, wParam, lParam);
        }

        private static List<ManageKeyboardHookEvent> registeredDelegates = new List<ManageKeyboardHookEvent>();

        /// <summary>
        /// Subscribe to receive notification when a key event is raised from the operating system. A delegate
        /// is required as parameter and the delegate will be called for all the subscribers until one returns
        /// true, meaning that it was processed and should not be passed to the rest of the application.
        /// </summary>
        /// <param name="eventDelegate">Delegate to the method which will be called when the key event raises</param>
        public static void RegisterHandler(ManageKeyboardHookEvent eventDelegate)
        {
            // When the first registration is raised, the keyboard is hooked application wise
            if (registeredDelegates.Count == 0)
            {
                HookKeyboard();
            }

            registeredDelegates.Add(eventDelegate);
        }

    }
}
