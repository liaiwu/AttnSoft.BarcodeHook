using AttnSoft.BarcodeHook.RawInput;
using System;
using System.Text;
using System.Threading;

///此项目改编自  https://github.com/draior/BarcodeHook.git

namespace AttnSoft.BarcodeHook
{
    /// <summary>
    /// 通过键盘钩子无焦点读取条码
    /// </summary>
    public class BarcodeReaderBase
    {
        protected BarCodeReadSetting readSetting=new BarCodeReadSetting();
        public BarcodeReaderBase()
        {
            keybState = KeybStatus.Idle;
            lastKeyTimestamp = DateTime.Now.Ticks;
        }
        public BarcodeReaderBase(BarCodeReadSetting ReadSetting)
        {
            this.readSetting = ReadSetting;
            //如何未设置条码头、条码尾及条码长度，则启用超时触发
            if (string.IsNullOrEmpty(readSetting.BarcodeHeader) && string.IsNullOrEmpty(readSetting.Trailer) && readSetting.BarcodeLength == 0)
            {
                enableTrailerTimeout = true;
                trailerTimeoutTimer = new System.Threading.Timer(timeoutTimer_Callback, null, Timeout.Infinite, Timeout.Infinite);
            }
            keybState = KeybStatus.Idle;
            lastKeyTimestamp = DateTime.Now.Ticks;
        } 
        /// <summary>
        /// 启动监听
        /// </summary>
        /// <returns></returns>
        public virtual bool Start()
        {
            return true;
        }
        /// <summary>
        /// 停止监听
        /// </summary>
        public virtual void Stop()
        {
        }
        /// <summary>
        /// The string that contains the keypress saved so far
        /// 包含到目前为止保存的按键信息的字符串
        /// </summary>
        protected StringBuilder keysBuffer = new StringBuilder(50);
        /// <summary>
        /// 读取到的条码头
        /// </summary>
        protected string barcodeHeader="";
        /// <summary>
        /// The shift status is handled 
        /// 处理shift状态
        /// </summary>
        protected bool shiftDown = false;

        /// <summary>
        /// The timestamp of the last keypress.
        /// 上一次按键的时间戳。
        /// </summary>
        protected long lastKeyTimestamp;

        protected KeybStatus keybState = KeybStatus.Idle;
        //两个按键的间隔时间
        protected readonly int KeyTimeout = 300;

        /// <summary>
        /// When > 0, in no character arrives within the specified number of milliseconds
        /// the barcode is considered as terminated and the received character buffer is sent.
        /// Use this parameter when a barcode has no trailer
        /// </summary>
        private readonly int trailerTimeout=300;

        /// <summary>
        /// Timer used to implement the trailerTimeout. This timer has to run only once after it has been activated
        /// and not at regular interval.
        /// </summary>
        private readonly System.Threading.Timer? trailerTimeoutTimer;

        private bool enableTrailerTimeout = false;

        protected virtual bool KeyboardHookHandler_KeyboardHookEvent(KeyboardMsg msg)
        {
            if (msg.Msg == WinApi.WM_KEYDOWN || msg.Msg == WinApi.WM_SYSKEYDOWN)
            {
                KeyboardKey key = new KeyboardKey(ref msg, shiftDown);
                if (key.Scancode == 42 || key.Scancode == 54)//是否按下了shift键
                {
                    shiftDown = true;
                }
                // if last key was read more than a second ago, return to idle status.
                // 如果上次按键时间超过 KeyTimeout，则置为空闲状态
                if ((DateTime.Now.Ticks - lastKeyTimestamp) > (KeyTimeout * TimeSpan.TicksPerMillisecond))
                {
                    keybState = KeybStatus.Idle;
                    keysBuffer.Length=0;
                    barcodeHeader = "";
                }
                lastKeyTimestamp = DateTime.Now.Ticks;

                try
                {
                    if (keybState == KeybStatus.Idle)
                    {
                        //Console.WriteLine("Keyboard status={0}", keybState);
                        // If no header is present
                        // 如果没有头字符
                        if (string.IsNullOrEmpty(readSetting.BarcodeHeader))
                        {
                            keybState = KeybStatus.ReadingData;
                        }
                        else 
                        {
                            keybState = KeybStatus.ReadingHeader;
                            barcodeHeader = "";
                        }
                    }

                    if (keybState == KeybStatus.ReadingHeader)
                    {
                        //Console.WriteLine("Keyboard status={0}", keybState);
                        if(key.KeyChar != 0)
                        {
                            barcodeHeader += key.KeyChar;
                        }
                        if (barcodeHeader.EndsWith(readSetting.BarcodeHeader))
                        {
                            keybState = KeybStatus.ReadingData;
                        }
                    }

                    if (keybState == KeybStatus.ReadingData)
                    {
                        //Console.WriteLine("Keyboard status={0}", keybState);
                        if (key.KeyChar != 0)
                        {
                            keysBuffer.Append(key.KeyChar);

                            //读到的码长度符合要求
                            if (readSetting.BarcodeLength > 0 && keysBuffer.Length == readSetting.BarcodeLength)
                            {
                                //触发事件
                                RaiseScanerEvent();
                                return true;
                            }
                            else if (readSetting.Trailer.Length > 0 && keysBuffer.Length > readSetting.Trailer.Length)
                            {
                                bool isFind = true;
                                for (int i = 0; i < readSetting.Trailer.Length; i++)
                                {
                                    if (readSetting.Trailer[readSetting.Trailer.Length - i - 1] != keysBuffer[keysBuffer.Length - i - 1])
                                    {
                                        isFind = false;
                                        break;
                                    }
                                }
                                if (isFind)
                                {
                                    RaiseScanerEvent();
                                    return true;
                                }
                            }
                            if (enableTrailerTimeout)
                            {
                                // Reset the moment the timer will be executed
                                trailerTimeoutTimer?.Change(trailerTimeout, Timeout.Infinite);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error while reading from keyboard device, resetting status. {0}", e.Message);
                    // 从键盘设备读取时出错，重置状态
                    keybState = KeybStatus.Idle;
                    keysBuffer.Length = 0;
                    barcodeHeader = "";
                }
            }
            else if (msg.Msg == WinApi.WM_KEYUP || msg.Msg == WinApi.WM_SYSKEYUP)
            {
                KeyboardKey key = new KeyboardKey(ref msg);
                if (key.Scancode == 42 || key.Scancode == 54)//是否按下了shift键
                {
                    shiftDown = false;
                }
            }
            return false;
        }

        protected virtual void RaiseScanerEvent()
        {
        }
        /// <summary>
        /// This procedure is called when the trailer timeout timer is expired.
        /// </summary>
        private void timeoutTimer_Callback(object? state)
        {
            trailerTimeoutTimer?.Change(Timeout.Infinite, Timeout.Infinite);//只触发一次
            if (keybState == KeybStatus.ReadingData && keysBuffer.Length>0)
            {
                //Console.WriteLine("Barcode read timeout, using current buffer.");
                RaiseScanerEvent();
            }
        }
        /// <summary>
        /// The keyboard interface uses a state machine. This enum contains the different states.
        /// 键盘接口使用状态机。此枚举包含不同的状态。
        /// </summary>
        public enum KeybStatus
        {
            /// <summary>
            /// No header byte was received yet.
            /// 尚未收到头字节。
            /// </summary>
            Idle,

            /// <summary>
            /// The first header byte was received. Wait for next bytes.
            /// 已收到第一个头字节，等待下一个字节。
            /// </summary>
            ReadingHeader,

            /// <summary>
            /// Data is arriving and are appended in the buffer until the trailer comes.
            /// 数据正在到达并追加到缓冲区，直到尾字符到来。
            /// </summary>
            ReadingData,

            ///// <summary>
            ///// The first trailer byte was received. Wait for next bytes.
            ///// 已收到第一个尾字节，等待下一个字节。
            ///// </summary>
            //ReadingTrailer
        }
    }
}
