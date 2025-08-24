using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace AttnSoft.BarcodeHook
{
    internal sealed class Utf8Marshaler : ICustomMarshaler
    {
        [ThreadStatic]
        static HashSet<IntPtr> _allocations = new HashSet<IntPtr>(); // Workaround for Mono bug 4722.

        static HashSet<IntPtr> GetAllocations()
        {
            return _allocations;
        }

        public void CleanUpManagedData(object obj)
        {

        }

        public void CleanUpNativeData(IntPtr ptr)
        {
            var allocations = GetAllocations();
            if (IntPtr.Zero == ptr || !allocations.Contains(ptr)) { return; }
            Marshal.FreeHGlobal(ptr); allocations.Remove(ptr);
        }

        public int GetNativeDataSize()
        {
            return -1;
        }

        public IntPtr MarshalManagedToNative(object obj)
        {
            string str = obj as string;
            if (str == null) { return IntPtr.Zero; }
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr, bytes.Length, 0);
            var allocations = GetAllocations();
            allocations.Add(ptr); return ptr;
        }

        public unsafe object MarshalNativeToManaged(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) { return null; }
            int length = 0;
            sbyte* psbyte = (sbyte*)ptr;
            for (; psbyte[length] != 0; ++length) { }
            return new string(psbyte, 0, length, Encoding.UTF8);
        }

        // This method needs to keep its original name.
        [Obfuscation(Exclude = true)]
        public static ICustomMarshaler GetInstance(string cookie)
        {
            return new Utf8Marshaler();
        }
    }
}
