using AttnSoft.BarcodeHook;
using System.Runtime.InteropServices;
using System.Text;

namespace Test
{
    public partial class Form1 : Form
    {


        BarcodeReaders scanerHook = new BarcodeReaders();
        public Form1()
        {
            InitializeComponent();
            //自定义条码格式:
            //var readSetting = new BarCodeReadSetting()
            //{
            //    BarcodeHeader = "^",//条码前缀
            //    Trailer = "\r",//条码结尾
            //    BarcodeLength = 20//条码长度
            //};
            //BarcodeReaders scanerHook = new BarcodeReaders(readSetting);

            scanerHook.ScanerEvent += ScanerHook_BarCodeEvent;
            scanerHook.Start();
        }

        private void ScanerHook_BarCodeEvent(string barcode)
        {
            this.listBox1.Items.Add(barcode);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            scanerHook.Stop();
        }
    }
}
