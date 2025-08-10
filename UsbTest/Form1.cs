

using AttnSoft.BarcodeHook;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace UsbTest
{
    public partial class Form1 : Form
    {
        BarcodeApiReader scanerHook = new BarcodeApiReader();
        public Form1()
        {
            InitializeComponent();
            //指定监听设备。默认为空，监听所有设备。
            #region 超时触发模式
            //去掉所有条码格式进入 超时触发模式:
            //var readSetting = new BarCodeReadSetting()
            //{
            //    BarcodeHeader = "",//条码前缀
            //    Trailer = "",//条码结尾
            //    BarcodeLength = 0//条码长度
            //};
            //scanerHook = new BarcodeApiReader(readSetting);
            //scanerHook.DeviceId = "HID_VID_1EAB&PID_3222&MI_00_7&39461ef6&0&0000";
            #endregion

            scanerHook.HookEvent += ScanerHook_BarCodeEvent;
            scanerHook.DeviceAction+=ScanerHook_DeviceAction;
            scanerHook.Start();
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadDeviceList();
        }

        private void LoadDeviceList()
        {
            listView1.Items.Clear();
            foreach (var device in scanerHook.GetDeviceList())
            {
                listView1.Items.Add(device.DeviceId);
            }
        }

        private void ScanerHook_DeviceAction(DeviceEvent deviceEvent)
        {
            ListViewItem? finditem = FindItem(deviceEvent.Device.DeviceId);
            if (deviceEvent.Attached && finditem==null)//系统插入了新设备
            {
                var newIitem= listView1.Items.Add(deviceEvent.Device.DeviceId);
                newIitem.Selected = true;
            }
            if (!deviceEvent.Attached && finditem != null)//系统移除了设备
            {
                listView1.Items.Remove(finditem);
            }
        }
        private ListViewItem? FindItem(string deviceId)
        {
            ListViewItem? finditem = null;
            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Text == deviceId)
                {
                    finditem = item;
                    break;
                }
            }
            return finditem;
        }

        private void ScanerHook_BarCodeEvent(HookResult hookResult)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    ScanerHook_BarCodeEvent(hookResult);
                }));
                return;
            }
            this.textBox1.Text = "";
            this.textBox2.Text = "";
            this.textBox2.Focus();
            Application.DoEvents();
            Thread.Sleep(200);

            this.textBox1.Text = hookResult.DeviceId;
            this.textBox2.Text=hookResult.Barcode;
            ListViewItem? finditem = FindItem(hookResult.DeviceId);
            if (finditem != null)
            {
                listView1.SelectedItems.Clear();
                finditem.Selected = true;
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            scanerHook.Stop();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            scanerHook.Start();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            scanerHook.Stop();
            this.textBox1.Text = this.textBox2.Text = "";
        }
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            Form1 form1 = new Form1();
            form1.Show();
        }

        private void ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(this.listView1.SelectedItems.Count>0)                    
            {
                scanerHook.DeviceId = this.listView1.SelectedItems[0].Text;
            }
        }
    }
}
