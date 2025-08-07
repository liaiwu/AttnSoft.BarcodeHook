# ![Alt text](BarcodeHook/BarcodeReader.ico "AttnSoft.AutoUpdate")AttnSoft.BarcodeHook
Industrial Scanning Gun Focal-less Input.

工业扫码枪无焦点输入
### 系统说明
本系统支持两种模式实现扫码枪无焦点输入：

1、使用键盘钩子实现

2、使用系统API实现

### 功能特性
1、键盘钩子模式通过单例模式及引用计数，实现键盘钩子全局仅注入一次，减少系统消息处理开销。

2、通过操作系统API，直接监听原始硬件输入，实现工业条码枪无焦点输入。

3、通过API获取硬件ID,以区分不同的扫码枪输入。

4、高效的解码逻辑，支持自定义前缀、后缀或条码长度解析读取条码。
### 快速开始
1、安装NuGet包
首先，项目中安装 AttnSoft.BarcodeHook NuGet 包：
```csharp
Install-Package AttnSoft.BarcodeHook
```
#### 键盘钩子模式（适合单一扫码枪）
1、创建钩子
```csharp
BarcodeReaders  scanerHook = new BarcodeReaders ();
```
2、绑定事件并启动钩子
```csharp
scanerHook.ScanerEvent += ScanerHook_BarCodeEvent;
scanerHook.Start();
```
3、处理扫码事件
```csharp
private void ScanerHook_BarCodeEvent(string barcode)
{
    this.listBox1.Items.Add(barcode);
}
```
4、停止钩子
```csharp
scanerHook.Stop();
```
#### API模式（适合多扫码枪）
1、创建监听
```csharp
BarcodeApiReader scanerHook = new BarcodeApiReader();
//指定监听设备。默认为空，监听所有设备。
scanerHook.DeviceId = "HID_VID_1EAB&PID_3222&MI_00_7&39461ef6&0&0000";
```
2、绑定事件并启动监听
```csharp
scanerHook.ScanerEvent += ScanerHook_BarCodeEvent;
scanerHook.Start();
```
3、处理扫码事件
```csharp
private void ScanerHook_BarCodeEvent(HookResult hookResult)
{
    //string deviceId= hookResult.DeviceId;//当前设备ID
    //如果未指定监听设备ID,可在这里可根据设备ID进行过滤。
    //string barcode = hookResult.Barcode;//条码
    this.listBox1.Items.Add(hookResult.Barcode);
}
```
4、停止监听
```csharp
scanerHook.Stop();
```
#### 如何获取设备ID
可参考示例:UsbTest
```csharp
//获取所有输入设备
var deviceList= scanerHook.GetDeviceList()

//监听扫码枪的插入与移除事件
scanerHook.DeviceAction+=ScanerHook_DeviceAction;
```

### 自定义条码格式

```csharp
var readSetting = new BarCodeReadSetting()
{
    BarcodeHeader = "^",//条码前缀
    Trailer = "\r",//条码结尾
    BarcodeLength = 20//条码长度
};
BarcodeReaders scanerHook = new BarcodeReaders(readSetting);
```
不指定条码格式时,系统内部默认为以回车符结尾的条码格式:
```csharp
BarcodeReaders scanerHook = new BarcodeReaders(new BarCodeReadSetting { Trailer="\r"});
```
### UsbTest运行示例
![Alt text](demo.gif "Demo")
此示例演示了多扫码枪无焦点输入时的运行效果.
在不同的窗口,通过鼠标右键菜单选择扫码枪设备，即可监听指定的设备。

### 支持框架

| .NET框架名称               | 是否支持 |
| -------------------------- | -------- |
| .NET Framework 4.0        | 支持     |
| .NET Core 2.0 、3.1        | 支持     |
| .NET 5  to last version   | 支持     |

### 开源链接
Github: https://github.com/liaiwu/AttnSoft.BarcodeHook.git
Gitee:  https://gitee.com/attnsoft/AttnSoft.BarcodeHook.git
