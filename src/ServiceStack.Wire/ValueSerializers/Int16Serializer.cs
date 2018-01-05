// -----------------------------------------------------------------------
//   <copyright file="Int16Serializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using Wire.Internal;

namespace Wire.ValueSerializers
{
    public class Int16Serializer : SessionAwareByteArrayRequiringValueSerializer<short>
    {
        public const byte Manifest = 3;
        public const int Size = sizeof(short);
        public static readonly Int16Serializer Instance = new Int16Serializer();

        public Int16Serializer() : base(Manifest, () => WriteValueImpl, () => ReadValueImpl)
        {
        }

        public override int PreallocatedByteBufferSize => Size;

        public static void WriteValueImpl(Stream stream, short sh, byte[] bytes)
        {
            NoAllocBitConverter.GetBytes(sh, bytes);
            stream.Write(bytes, 0, Size);
        }

        public static short ReadValueImpl(Stream stream, byte[] bytes)
        {
            stream.Read(bytes, 0, Size);
            return BitConverter.ToInt16(bytes, 0);
        }
    }
}