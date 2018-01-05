// -----------------------------------------------------------------------
//   <copyright file="GuidSerializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using Wire.Extensions;

namespace Wire.ValueSerializers
{
    public class GuidSerializer : SessionIgnorantValueSerializer<Guid>
    {
        public const byte Manifest = 11;
        public static readonly GuidSerializer Instance = new GuidSerializer();

        public GuidSerializer() : base(Manifest, () => WriteValueImpl, () => ReadValueImpl)
        {
        }

        public static void WriteValueImpl(Stream stream, Guid g)
        {
            var bytes = g.ToByteArray();
            stream.Write(bytes);
        }

        public static Guid ReadValueImpl(Stream stream)
        {
            var buffer = new byte[16];
            stream.Read(buffer, 0, 16);
            return new Guid(buffer);
        }
    }
}