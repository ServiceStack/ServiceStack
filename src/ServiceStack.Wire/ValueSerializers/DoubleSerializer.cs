// -----------------------------------------------------------------------
//   <copyright file="DoubleSerializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using Wire.Internal;

namespace Wire.ValueSerializers
{
    public class DoubleSerializer : SessionAwareByteArrayRequiringValueSerializer<double>
    {
        public const byte Manifest = 13;
        public const int Size = sizeof(double);
        public static readonly DoubleSerializer Instance = new DoubleSerializer();

        public DoubleSerializer() : base(Manifest, () => WriteValueImpl, () => ReadValueImpl)
        {
        }

        public override int PreallocatedByteBufferSize => Size;

        public static void WriteValueImpl(Stream stream, double d, byte[] bytes)
        {
            NoAllocBitConverter.GetBytes(d, bytes);
            stream.Write(bytes, 0, Size);
        }

        public static double ReadValueImpl(Stream stream, byte[] bytes)
        {
            stream.Read(bytes, 0, Size);
            return BitConverter.ToDouble(bytes, 0);
        }
    }
}