using System;

namespace AttnSoft.BarcodeHook
{
    /// <summary>
    /// Parse a WM_KEYDOWN message extracting the scancode and char information from it.
    /// </summary>
    public class KeyboardKey
    {
        /// <summary>
        /// The keyboard character as returned from the message, so keyboard layout dependant.
        /// </summary>
        public char MessageChar
        {
            get { return messageChar; }
        }

        private readonly char messageChar;

        /// <summary>
        /// The keyboard character as converted by ScancodeToChar(scancode, false), so keyboard layout indipendent.
        /// 按键码转换成字符
        /// </summary>
        public char KeyChar
        {
            get { return keyChar; }
        }

        private readonly char keyChar;

        /// <summary>
        /// The key scancode.
        /// 按键码
        /// </summary>
        public int Scancode
        {
            get { return scancode; }
        }

        private readonly int scancode;

        private const char ch0 = (char)0;
        private static readonly char[] asciiNormal =
          new char[]{
                  ch0,
                  (char) 27,'1','2','3','4','5','6','7','8','9','0','-','=',(char) 8,
                  (char) 9,'q','w','e','r','t','y','u','i','o','p','[',']',(char)13,ch0,
                  'a','s','d','f','g','h','j','k','l',';','\'','`',ch0,'\\',
                  'z','x','c','v','b','n','m',',','.','/',ch0,ch0,ch0,
                  ' '};
        private static readonly char[] asciiShift =
          new char[]{
                  ch0,
                  (char) 27,'!','@','#','$','%','^','&','*','(',')','_','+', (char) 8,
                  (char) 9,'Q','W','E','R','T','Y','U','I','O','P','{','}',(char)13,ch0,
                  'A','S','D','F','G','H','J','K','L',':','"','|',ch0,'|',
                  'Z','X','C','V','B','N','M','<','>','?',ch0,ch0,ch0,
                  ' '};

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="m">The WM_KEYDOWN message containing the key information.</param>
        public KeyboardKey(ref KeyboardMsg m)
          : this(ref m, false)
        { }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="msg">The WM_KEYDOWN message containing the key information.</param>
        /// <param name="shift">When true, the shift key must be considered pressed</param>
        public KeyboardKey(ref KeyboardMsg msg, bool shift)
        {
            // Hiword of LParam
            scancode=msg.ScanCode & 0xFF;
            // Lower 16 bits of m.WParam
            messageChar = Convert.ToChar(msg.VkCode & 0xFF);
            keyChar = ScancodeToChar(scancode, shift);
#if DEBUG
            string charForTrace = keyChar < ' ' ? string.Format("0x{0:X2}", (int)keyChar) : keyChar.ToString();
            string keyPress = msg.Msg == WinApi.WM_KEYDOWN ? "down" : "up  ";
            Console.WriteLine($"Key: Msg={keyPress}, Scancode={Scancode}, MsgChar={MessageChar}, CvtChar={charForTrace} ({Convert.ToInt32(KeyChar)})");
#endif
        }

        /// <summary>
        /// Convert a scancode to the undelying key char using an united stated keyboard layout.
        /// </summary>
        /// <param name="shift">Shift pressed or not</param>
        /// <returns>The char of the key. If a key has no character (like functions keys) an (char) 0 is returned.</returns>
        public char ScancodeToChar(bool shift)
        {
            return ScancodeToChar(scancode, shift);
        }

        /// <summary>
        /// Convert a scancode to the undelying key char using an united stated keyboard layout.
        /// </summary>
        /// <param name="scancode">The key scancode.</param>
        /// <param name="shift">Shift pressed or not</param>
        /// <returns>The char of the key. If a key has no character (like functions keys) an (char) 0 is returned.</returns>
        public static char ScancodeToChar(int scancode, bool shift)
        {
            char chr;
            if (shift)
            {
                if (scancode >= asciiShift.Length)
                {
                    chr = ch0;
                }
                else
                {
                    chr = asciiShift[scancode];
                }
            }
            else
            {
                if (scancode >= asciiNormal.Length)
                {
                    chr = ch0;
                }
                else
                {
                    chr = asciiNormal[scancode];
                }
            }

            return chr;
        }
    }
}
