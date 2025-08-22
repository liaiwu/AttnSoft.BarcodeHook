using Avalonia.Controls;

namespace AvaDemo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Width = 750;
        this.Height = 550;
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
}
