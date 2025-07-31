namespace AttnSoft.BarcodeHook
{
    /// <summary>
    /// The setting of barcode read
    /// 条码读取设置
    /// </summary>
    public class BarCodeReadSetting
    {
        /// <summary>
        /// The header of the barcode
        /// 条码头
        /// </summary>
        public string BarcodeHeader { get; set; } = "";
        /// <summary>
        /// The trailer of the barcode
        /// 条码尾(一般以回车为结尾)
        /// </summary>
        public string Trailer { get; set; } = "\r";
        /// <summary>
        /// This field is used when a reader is not able to send a trailer.
        /// In this situation data is complete when a certain length is reached.
        /// If 0, no length check is made
        /// 当阅读器无法发送尾字符时使用此字段。
        /// 在这种情况下，当达到特定长度时数据视为完整。
        /// 如果为0，则不进行长度检查。
        /// </summary>
        public int BarcodeLength { get; set; } = 0;

    }
}
