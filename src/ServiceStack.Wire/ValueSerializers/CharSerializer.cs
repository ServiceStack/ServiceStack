// -----------------------------------------------------------------------
//   <copyright file="CharSerializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using Wire.Internal;

namespace Wire.ValueSerializers
{
    public class CharSerializer : SessionAwareByteArrayRequiringValueSerializer<char>
    {
        public const byte Manifest = 15;
        public const int Size = sizeof(char);
        public static readonly CharSerializer Instance = new CharSerializer();

        public CharSerializer() : base(Manifest, () => WriteValueImpl, () => ReadValueImpl)
        {
        }

        public override int PreallocatedByteBufferSize => Size;

        public static char ReadValueImpl(Stream stream, byte[] bytes)
        {
            stream.Read(bytes, 0, Size);
            return BitConverter.ToChar(bytes, 0);
        }

        public static void WriteValueImpl(Stream stream, char ch, byte[] bytes)
        {
            NoAllocBitConverter.GetBytes(ch, bytes);
            stream.Write(bytes, 0, Size);
        }
    }
}