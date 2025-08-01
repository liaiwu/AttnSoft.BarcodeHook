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
            //�Զ��������ʽ:
            //var readSetting = new BarCodeReadSetting()
            //{
            //    BarcodeHeader = "^",//����ǰ׺
            //    Trailer = "\r",//�����β
            //    BarcodeLength = 20//���볤��
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
