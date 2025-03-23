using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ImGuiNET
{
    internal static unsafe class Util
    {
        internal const int StackAllocationSizeLimit = 2048;

        public static byte[] PointerToByteArray(IntPtr pointer, int size)
        {
            if (pointer == IntPtr.Zero)
            {
                throw new ArgumentException("Pointer cannot be null", nameof(pointer));
            }

            if (size <= 0)
            {
                throw new ArgumentException("Size must be greater than zero", nameof(size));
            }

            byte[] byteArray = new byte[size];
            Marshal.Copy(pointer, byteArray, 0, size);

            return byteArray;
        }

        public static string StringFromPtr(byte* ptr)
        {
            int characters = 0;
            while (ptr[characters] != 0)
            {
                characters++;
            }

            IntPtr intptr = new IntPtr(ptr);
            var data = PointerToByteArray(intptr, characters);

            return Encoding.UTF8.GetString(data);
        }

        internal static bool AreStringsEqual(byte* a, int aLength, byte* b)
        {
            for (int i = 0; i < aLength; i++)
            {
                if (a[i] != b[i]) { return false; }
            }

            if (b[aLength] != 0) { return false; }

            return true;
        }

        internal static byte* Allocate(int byteCount) => (byte*)Marshal.AllocHGlobal(byteCount);

        internal static void Free(byte* ptr) => Marshal.FreeHGlobal((IntPtr)ptr);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        internal static int CalcSizeInUtf8(ReadOnlySpan<char> s, int start, int length)
#else
        internal static int CalcSizeInUtf8(string s, int start, int length)
#endif
        {
            if (start < 0 || length < 0 || start + length > s.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            if(s.Length == 0) return 0;

            fixed (char* utf16Ptr = s)
            {
                return Encoding.UTF8.GetByteCount(utf16Ptr + start, length);
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        internal static int GetUtf8(ReadOnlySpan<char> s, byte* utf8Bytes, int utf8ByteCount)
        {
            if (s.IsEmpty)
            {
                return 0;
            }

            fixed (char* utf16Ptr = s)
            {
                return Encoding.UTF8.GetBytes(utf16Ptr, s.Length, utf8Bytes, utf8ByteCount);
            }
        }
#endif

        internal static int GetUtf8(string s, byte* utf8Bytes, int utf8ByteCount)
        {
            fixed (char* utf16Ptr = s)
            {
                return Encoding.UTF8.GetBytes(utf16Ptr, s.Length, utf8Bytes, utf8ByteCount);
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        internal static int GetUtf8(ReadOnlySpan<char> s, int start, int length, byte* utf8Bytes, int utf8ByteCount)
#else
        internal static int GetUtf8(string s, int start, int length, byte* utf8Bytes, int utf8ByteCount)
#endif
        {
            if (start < 0 || length < 0 || start + length > s.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (s.Length == 0) return 0;

            fixed (char* utf16Ptr = s)
            {
                return Encoding.UTF8.GetBytes(utf16Ptr + start, length, utf8Bytes, utf8ByteCount);
            }
        }
    }
}
