// -----------------------------------------------------------------------
//   <copyright file="StringEx.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Wire.Internal;

namespace Wire.Extensions
{
    internal static class StringEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte[] ToUtf8Bytes(this string str)
        {
            return NoAllocBitConverter.Utf8.GetBytes(str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string FromUtf8Bytes(byte[] bytes, int offset, int count)
        {
            return NoAllocBitConverter.Utf8.GetString(bytes, offset, count);
        }
    }
}