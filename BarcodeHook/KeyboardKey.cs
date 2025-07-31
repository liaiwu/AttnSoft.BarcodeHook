using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodeApp
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
        /// </summary>
        public char ConvertedChar
        {
            get { return convertedChar; }
        }

        private readonly char convertedChar;

        /// <summary>
        /// The key scancode.
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
        public KeyboardKey(ref System.Windows.Forms.Message m)
          : this(ref m, false)
        { }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="m">The WM_KEYDOWN message containing the key information.</param>
        /// <param name="shift">When true, the shift key must be considered pressed</param>
        public KeyboardKey(ref System.Windows.Forms.Message m, bool shift)
        {
            // Hiword of LParam
            scancode = Convert.ToInt32((m.LParam.ToInt64() >> 16) & 0x1FF);
            // Lower 16 bits of m.WParam
            messageChar = Convert.ToChar(m.WParam.ToInt64() & 0xFFFF);
            convertedChar = ScancodeToChar(scancode, shift);
            string charForTrace = convertedChar < ' ' ? string.Format("0x{0:X2}", (int)convertedChar) : convertedChar.ToString();
            Console.WriteLine("Key: Msg={0}, Scancode={1,3:X}h, MsgChar={2}, CvtChar={3} ({4})",
              m.Msg == Win32.WM_KEYDOWN ? "down" : "up  ",
              Scancode, MessageChar, charForTrace, Convert.ToInt32(ConvertedChar));
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
