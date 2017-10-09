// -----------------------------------------------------------------------
//   <copyright file="NoAllocBitConverter.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Text;
using Wire.ValueSerializers;

namespace Wire.Internal
{
    /// <summary>
    ///     Provides methods not allocating the byte buffer but using <see cref="SerializerSession.GetBuffer" /> to lease a
    ///     buffer.
    /// </summary>
    internal static class NoAllocBitConverter
    {
        internal static readonly UTF8Encoding Utf8 = (UTF8Encoding) Encoding.UTF8;

        public static void GetBytes(char value, byte[] bytes)
        {
            GetBytes((short) value, bytes);
        }

        public static unsafe void GetBytes(short value, byte[] bytes)
        {
            fixed (byte* b = bytes)
            {
                *(short*) b = value;
            }
        }

        public static unsafe void GetBytes(int value, byte[] bytes)
        {
            fixed (byte* b = bytes)
            {
                *(int*) b = value;
            }
        }

        public static unsafe void GetBytes(long value, byte[] bytes)
        {
            fixed (byte* b = bytes)
            {
                *(long*) b = value;
            }
        }

        public static void GetBytes(ushort value, byte[] bytes)
        {
            GetBytes((short) value, bytes);
        }

        public static void GetBytes(uint value, byte[] bytes)
        {
            GetBytes((int) value, bytes);
        }

        public static void GetBytes(ulong value, byte[] bytes)
        {
            GetBytes((long) value, bytes);
        }

        public static unsafe void GetBytes(float value, byte[] bytes)
        {
            GetBytes(*(int*) &value, bytes);
        }

        public static unsafe void GetBytes(double value, byte[] bytes)
        {
            GetBytes(*(long*) &value, bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe byte[] GetBytes(string str, SerializerSession session, out int byteCount)
        {
            //if first byte is 0 = null
            //if first byte is 254 or less, then length is value - 1
            //if first byte is 255 then the next 4 bytes are an int32 for length
            if (str == null)
            {
                byteCount = 1;
                return new[] {(byte) 0};
            }
            byteCount = Utf8.GetByteCount(str);
            if (byteCount < 254) //short string
            {
                var bytes = session.GetBuffer(byteCount + 1);
                Utf8.GetBytes(str, 0, str.Length, bytes, 1);
                bytes[0] = (byte) (byteCount + 1);
                byteCount += 1;
                return bytes;
            }
            else //long string
            {
                var bytes = session.GetBuffer(byteCount + 1 + 4);
                Utf8.GetBytes(str, 0, str.Length, bytes, 1 + 4);
                bytes[0] = 255;


                fixed (byte* b = bytes)
                {
                    *(int*) (b + 1) = byteCount;
                }

                byteCount += 1 + 4;

                return bytes;
            }
        }

        public static unsafe void GetBytes(DateTime dateTime, byte[] bytes)
        {
            //datetime size is 9 ticks + kind
            fixed (byte* b = bytes)
            {
                *(long*) b = dateTime.Ticks;
            }
            bytes[DateTimeSerializer.Size - 1] = (byte) dateTime.Kind;
        }
    }
}