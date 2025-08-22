# ![Alt text](BarcodeHook/BarcodeReader.ico "AttnSoft.AutoUpdate")AttnSoft.BarcodeHook
A Cross-platform Focal Input System for Industrial Scanning Gun.

一个跨平台的工业扫码枪无焦点输入系统
### 系统说明
本系统支持两种模式实现扫码枪无焦点输入：

1、使用键盘钩子实现（适合单个扫码枪）

2、使用系统API实现（适合多扫码枪）

Windows下可以使用以上两种模式，Linux下仅支持API模式。


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
### 超时触发模式

在解决扫码枪无焦点输入问题后，部分用户可能会遇到以下情况：操作软件时，最后的焦点停留在某个按钮上，导致扫码时误触发按钮事件。
由于大部分扫码枪的条码格式以回车符结尾，去掉回车符可以避免误触发按钮事件，但同时也会导致原本以回车符为结束标志的条码无法正常触发。

为了解决这一问题，本模块引入了**超时触发模式**。当最后一个字符到达后，如果在指定时间内没有新的字符到达，系统会认为扫码结束并触发扫码事件。

#### 启用超时触发模式

通过以下代码配置超时触发模式(即去掉所有条码格式,系统自动进入超时触发模式)：
```csharp
var readSetting = new BarCodeReadSetting()
{
    BarcodeHeader = "",//条码前缀
    Trailer = "",//去掉条码结尾
    BarcodeLength = 0  //条码长度
};
BarcodeApiReader scanerHook = new BarcodeApiReader(readSetting);
```
注意事项:

1、采用无格式触，不要使用键盘钩子模式，会导致用户的普通键盘输入触发扫码事件。建议采用API模式并绑定设备ID，这样避免了误触发。


2、处理扫码事件时，如果要访问UI控件，请加入InvokeRequired判断并处理异步，因为超时触发不在UI线程。

3、Linux下需要当前用户在input组下。参考如下命令将当前用户添加到input组:
```
sudo usermod -aG input $USER
```
### UsbTest运行示例
![Alt text](demo.gif "Demo")
此示例演示了多扫码枪无焦点输入时的运行效果.
在不同的窗口,通过鼠标右键菜单选择扫码枪设备，即可监听指定的设备。
### AvaDemo在Linux下t运行示例
![Alt text](Avdemo.gif "Demo")

### 支持框架

| .NET框架名称               | 是否支持 |
| -------------------------- | -------- |
| .NET Framework 4.0        | 支持     |
| .NET Core 2.0 、3.1        | 支持     |
| .NET 5  to last version   | 支持     |

### 开源链接
Github: https://github.com/liaiwu/AttnSoft.BarcodeHook.git

Gitee:  https://gitee.com/attnsoft/AttnSoft.BarcodeHook.git
