# ![Alt text](BarcodeHook/BarcodeReader.ico "AttnSoft.AutoUpdate")AttnSoft.BarcodeHook
Windows Keyboard Hook Realizes Industrial Scanning Gun Focal-less Input.

Windows 键盘钩子实现工业扫码枪无焦点输入
### 功能特性
1.通过单例模式及引用计数，实现键盘钩子全局仅注入一次，减少系统消息处理开销。

2.高效的解码逻辑，支持自定义前缀、后缀或条码长度解析读取条码。
### 快速开始
安装NuGet包：AttnSoft.BarcodeHook

1、创建钩子
```csharp
BarcodeReaders scanerHook = new BarcodeReaders();
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
### 支持框架

| .NET框架名称               | 是否支持 |
| -------------------------- | -------- |
| .NET Framework 4.0        | 支持     |
| .NET Core 2.0 、3.1        | 支持     |
| .NET 5  to last version   | 支持     |

### Github
https://github.com/liaiwu/AttnSoft.BarcodeHook.git
