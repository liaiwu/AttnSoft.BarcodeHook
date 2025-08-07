using AttnSoft.BarcodeHook.RawInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttnSoft.BarcodeHook
{
    /// <summary>
    /// 通过Windows API 无焦点读取条码
    /// </summary>
    public class BarcodeApiReader : BarcodeReaderBase
    {
        public event Action<HookResult>? HookEvent;
        IKeyboardHook keyboardHook;
        public BarcodeApiReader():base()
        {
            keyboardHook = KeyboardHookService.GetService();
        }
        public BarcodeApiReader(BarCodeReadSetting ReadSetting):base(ReadSetting)
        {
            keyboardHook = KeyboardHookService.GetService();
        }
        string _deviceId = string.Empty;
        /// <summary>
        /// 指定监听的设备ID,默认为空，表示监听所有设备
        /// </summary>
        public string DeviceId
        {
            get { return _deviceId; }
            set {
                _deviceId = value;
                if (string.IsNullOrEmpty(value))
                {
                    _deviceId = string.Empty;
                }
            }
        }
        /// <summary>
        /// 获取所有输入设备
        /// </summary>
        /// <returns></returns>
        public List<RawDevice> GetDeviceList()
        {
            return keyboardHook.Devices.Values.ToList();
        }
        /// <summary>
        /// 设备变动事件
        /// </summary>
        public event Action<DeviceEvent>? DeviceAction
        {
            add
            {
                keyboardHook.DeviceAction += value;
            }
            remove
            {
                keyboardHook.DeviceAction -= value;
            }
        }
        volatile bool isStart = false;
        /// <summary>
        /// 启动监听
        /// </summary>
        /// <returns></returns>
        public override bool Start()
        {
            if (isStart) return true;
            isStart = true;
            RawDeviceInput.Instance.KeyPressAction += Instance_KeyPressAction;
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
                RawDeviceInput.Instance.KeyPressAction -= Instance_KeyPressAction;
            }
        }
        string lastDeviceId = "";
        private void Instance_KeyPressAction(KeyboardDeviceMsg keyMsg)
        {
            if (_deviceId.Length > 0)//如果指定设备ID
            {
                if (keyMsg.KeyboardDevice.DeviceId != _deviceId)
                {
                    return;//不是指定设备ID,忽略
                }
                lastDeviceId = keyMsg.KeyboardDevice.DeviceId;
            }
            else if (lastDeviceId != keyMsg.KeyboardDevice.DeviceId)
            {
                lastDeviceId = keyMsg.KeyboardDevice.DeviceId;
                keybState = KeybStatus.Idle;
            }
            KeyboardHookHandler_KeyboardHookEvent(keyMsg);
        }


        /// <summary>
        /// 监听结果事件
        /// </summary>
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
                HookEvent?.Invoke(new HookResult(barcode, lastDeviceId));
            }
            keysBuffer.Length = 0;
        }
    }
}
