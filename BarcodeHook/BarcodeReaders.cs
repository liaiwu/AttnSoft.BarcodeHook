using System;
using System.Text;

///此项目改编自  https://github.com/draior/BarcodeHook.git

namespace AttnSoft.BarcodeHook
{
    public delegate void BarcodeReaderEvent(string barcode);
    /// <summary>
    /// 通过键盘钩子无焦点读取条码
    /// </summary>
    public class BarcodeReaders: BarcodeReaderBase//BarcodeApiReaders
    {
        public event BarcodeReaderEvent? ScanerEvent;

        public BarcodeReaders():base()
        {
        }
        public BarcodeReaders(BarCodeReadSetting ReadSetting):base(ReadSetting)
        {
        }
        bool isStart = false;
        /// <summary>
        /// 启动监听
        /// </summary>
        /// <returns></returns>
        public override bool Start()
        {
            if (isStart) return true;
            isStart = KeyboardHookHandler.Instance.HookKeyboard();
            if (isStart)
            {
                KeyboardHookHandler.Instance.KeyboardHookEvent += KeyboardHookHandler_KeyboardHookEvent;
            }
            return isStart;
        }
        /// <summary>
        /// 停止监听
        /// </summary>
        public override void Stop()
        {
            if (isStart)
            {
                isStart = false;
                KeyboardHookHandler.Instance.KeyboardHookEvent -= KeyboardHookHandler_KeyboardHookEvent;
                KeyboardHookHandler.Instance.UnHookKeyboard();
            }
        }
        protected override void RaiseScanerEvent()
        {
            keybState = KeybStatus.Idle;
            if (readSetting.Trailer.Length > 0)
            {
                keysBuffer.Length-=readSetting.Trailer.Length;
            }
            string barcode = keysBuffer.ToString();
            if (!string.IsNullOrEmpty(barcode))
            {
                ScanerEvent?.Invoke(barcode);
            }
            keysBuffer.Length = 0;
        }
    }
}
