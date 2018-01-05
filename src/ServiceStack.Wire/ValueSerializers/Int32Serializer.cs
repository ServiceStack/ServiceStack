// -----------------------------------------------------------------------
//   <copyright file="Int32Serializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using Wire.Internal;

namespace Wire.ValueSerializers
{
    public class Int32Serializer : SessionAwareByteArrayRequiringValueSerializer<int>
    {
        public const byte Manifest = 8;
        public const int Size = sizeof(int);
        public static readonly Int32Serializer Instance = new Int32Serializer();

        public Int32Serializer()
            : base(Manifest, () => WriteValueImpl, () => ReadValueImpl)
        {
        }

        public override int PreallocatedByteBufferSize => Size;

        public static void WriteValueImpl(Stream stream, int i, byte[] bytes)
        {
            NoAllocBitConverter.GetBytes(i, bytes);
            stream.Write(bytes, 0, Size);
        }

        public static void WriteValueImpl(Stream stream, int i, SerializerSession session)
        {
            var bytes = session.GetBuffer(Size);
            WriteValueImpl(stream, i, bytes);
        }

        public static int ReadValueImpl(Stream stream, byte[] bytes)
        {
            stream.Read(bytes, 0, Size);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}