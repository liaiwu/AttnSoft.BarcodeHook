using AttnSoft.BarcodeHook;
using AttnSoft.BarcodeHook.RawInput;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AvaDemo.Views;

public partial class MainView : UserControl
{
    BarcodeApiReader scanerHook = new BarcodeApiReader();
    List<string> devices = new List<string>();
    object locker = new object();
    public MainView()
    {
        InitializeComponent();
        txtDeviceId.IsReadOnly = true;
        menuItemSetId.Click+= (s, e) => 
        {
            if (listDevice.SelectedValue is RawDevice device )
            {
                scanerHook.DeviceId = device.DeviceId;
            }
        };
        scanerHook.HookEvent += ScanerHook_BarCodeEvent;
        scanerHook.DeviceAction += ScanerHook_DeviceAction;
        scanerHook.Start();
    }
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        lock (locker)
        {
            foreach (RawDevice item in scanerHook.GetDeviceList())
            {
                if (!listDevice.Items.Contains(item))
                {
                    listDevice.Items.Add(item);
                }
            }
        }

    }
    private void ScanerHook_DeviceAction(DeviceEvent deviceEvent)
    {
        Dispatcher.UIThread.Post(() => {
            lock (locker)
            {
                if (deviceEvent.Attached)
                {
                    listDevice.Items.Add(deviceEvent.Device);
                }
                else
                {
                    if (listDevice.Items.Contains(deviceEvent.Device))
                        listDevice.Items.Remove(deviceEvent.Device);
                }
            }
            this.InvalidateVisual();
        }, DispatcherPriority.Background);
    }
    private object? FindItem(string  deviceid)
    {
        foreach (RawDevice? item in listDevice.Items)
        {
            if (item!=null && item.DeviceId  == deviceid)
            {
                return item;
            }
        }
        return null;
    }

    private void ScanerHook_BarCodeEvent(HookResult result)
    {
        Dispatcher.UIThread.Post(() => {
            this.txtDeviceId.Text=result.DeviceId;
            this.txtBarcode.Text = result.Barcode;
            var item = FindItem(result.DeviceId);
            if (item != null)
            {
                listDevice.UnselectAll();
                this.InvalidateVisual();
                listDevice.SelectedItem = item;
            }
        }, DispatcherPriority.Background);
   
    }
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        scanerHook.Stop();
    }
}
