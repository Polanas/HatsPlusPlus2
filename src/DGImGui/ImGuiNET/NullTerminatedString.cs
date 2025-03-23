using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ImGuiNET
{
    public unsafe struct NullTerminatedString
    {
        public readonly byte* Data;

        public NullTerminatedString(byte* data)
        {
            Data = data;
        }

        public override string ToString()
        {
            int length = 0;
            byte* ptr = Data;
            while (*ptr != 0)
            {
                length += 1;
                ptr += 1;
            }

            IntPtr intptr = new IntPtr(ptr);
            var data = Util.PointerToByteArray(intptr, length);

            return Encoding.ASCII.GetString(data);
        }

        public static implicit operator string(NullTerminatedString nts) => nts.ToString();
    }
}
