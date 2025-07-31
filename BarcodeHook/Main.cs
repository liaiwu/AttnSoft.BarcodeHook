using System.Reflection;
using System.Runtime.InteropServices;

namespace BarcodeApp
{
  public partial class Main : Form
  {
    public GenericReader reader;

    public Label lblBC;

    public IntPtr hWnd;

    // Define constants for custom messages
    const int WM_USER = 0x0400;
    const int WM_COPYDATA = 0x004A;
    const int WM_NOTIFYBARCODE = WM_USER + 0x1000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    public Main(string[] args)
    {
      InitializeComponent();

      this.StartPosition = FormStartPosition.Manual;

      // Calculate the position to set the form at the top-right corner of the screen
      int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
      int formWidth = this.Width;
      int xPosition = screenWidth - formWidth;

      // Set the maximum height for the form
      this.Height = Screen.PrimaryScreen.WorkingArea.Height;

      // Set the location to the top-right corner of the screen
      this.Location = new Point(xPosition, 0);

      if (args.Length == 0)
      {
        lb.Items.Add("Manual started app");
        return;
      }

      // Find the window handle of the Delphi application
      if (!IntPtr.TryParse(args[0], out hWnd))
      {
        lb.Items.Add("Invalid window handle.");
        return;
      }

      lb.Items.Add(hWnd.ToString());
      if (hWnd == IntPtr.Zero)
      {
        lb.Items.Add("Could not find window.");
        return;
      }
    }

    private void Main_Load(object sender, EventArgs e)
    {
      var param = new IniValue();
      string exeFilePath = Assembly.GetEntryAssembly().Location;
      string fileName = Path.GetFileNameWithoutExtension(exeFilePath);
      fileName = Path.Combine(Path.GetDirectoryName(exeFilePath), fileName + ".ini");
      if (File.Exists(fileName))
      {
        using (StreamReader reader = new StreamReader(fileName))
        {
          string line;

          while ((line = reader.ReadLine()) != null)
          {
            // Check if the line contains a section header
            param.Parse(line);
          }
        }
      }
      else
      {
        param.AddParamValue("Header", "35");
        param.AddParamValue("Trailer", "13");
        param.AddParamValue("ManageShift", "true");
      }

      reader = new GenericReader(param);
      reader.BarcodeEvent += ReadBC;

      reader.Enable();

      lblBC = new Label();
      this.Controls.Add(lblBC);
      lblBC.Top = 10;
      lblBC.Left = 10;
      lblBC.Visible = true;
      lblBC.Height = 400;

      this.WindowState = FormWindowState.Minimized;
      var showForm = param.ValueOf("ShowForm");
      if (string.IsNullOrEmpty(showForm) || !showForm.ToLower().Equals("true"))
      {
        this.ShowInTaskbar = false;
      }
    }

    private void ReadBC(string barcode)
    {
      lblBC.Text = barcode;
      if (hWnd != IntPtr.Zero)
      {
        SendCustomMessage(hWnd, barcode);
        lb.Items.Add(barcode);
      }
    }

    private void Main_FormClosing(object sender, FormClosingEventArgs e)
    {
      reader.Close();
    }

    public void SendCustomMessage(IntPtr hWnd, string message)
    {
      // Send the custom message as WM_COPYDATA
      COPYDATASTRUCT cds = new COPYDATASTRUCT();
      cds.dwData = new IntPtr(WM_NOTIFYBARCODE);
      cds.cbData = message.Length + 1;
      cds.lpData = Marshal.StringToHGlobalAnsi(message);

      IntPtr ptrCds = Marshal.AllocHGlobal(Marshal.SizeOf(cds));
      Marshal.StructureToPtr(cds, ptrCds, false);

      SendMessage(hWnd, WM_COPYDATA, IntPtr.Zero, ptrCds);

      Marshal.FreeHGlobal(ptrCds);
      Marshal.FreeHGlobal(cds.lpData);
    }

    private void button1_Click(object sender, EventArgs e)
    {
      string message = Microsoft.VisualBasic.Interaction.InputBox("Enter a barcode:", "barcode", "");

      SendCustomMessage(hWnd, message);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT
    {
      public IntPtr dwData;
      public int cbData;
      public IntPtr lpData;
    }
  }
}