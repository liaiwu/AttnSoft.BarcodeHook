using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttnSoft.BarcodeHook
{
    /// <summary>
    /// 扫码结果
    /// </summary>
    public class HookResult
    {
        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; }
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; }
        public HookResult(string barcode, string deviceId) {
            Barcode = barcode;
            DeviceId = deviceId;
        }
    }
}
