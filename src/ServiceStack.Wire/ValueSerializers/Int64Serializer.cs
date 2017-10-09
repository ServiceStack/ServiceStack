// -----------------------------------------------------------------------
//   <copyright file="Int64Serializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using Wire.Internal;

namespace Wire.ValueSerializers
{
    public class Int64Serializer : SessionAwareByteArrayRequiringValueSerializer<long>
    {
        public const byte Manifest = 2;
        public const int Size = sizeof(long);
        public static readonly Int64Serializer Instance = new Int64Serializer();

        public Int64Serializer() : base(Manifest, () => WriteValueImpl, () => ReadValueImpl)
        {
        }

        public override int PreallocatedByteBufferSize => Size;

        public static void WriteValueImpl(Stream stream, long l, byte[] bytes)
        {
            NoAllocBitConverter.GetBytes(l, bytes);
            stream.Write(bytes, 0, Size);
        }

        public static long ReadValueImpl(Stream stream, byte[] bytes)
        {
            stream.Read(bytes, 0, Size);
            return BitConverter.ToInt64(bytes, 0);
        }
    }
}