using AttnSoft.BarcodeHook;
using AttnSoft.BarcodeHook.RawInput;

namespace AttnSoft.BarcodeHook
{
    public readonly struct DeviceEvent
    {
        public RawDevice Device { get; }
        public bool Attached { get; }

        public DeviceEvent(RawDevice device, bool attached)
        {
            Device = device;
            Attached = attached;
        }
    }
}