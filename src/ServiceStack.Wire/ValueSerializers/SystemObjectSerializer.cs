// -----------------------------------------------------------------------
//   <copyright file="SystemObjectSerializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;

namespace Wire.ValueSerializers
{
    public class SystemObjectSerializer : ValueSerializer
    {
        public const byte Manifest = 1;
        public static SystemObjectSerializer Instance = new SystemObjectSerializer();

        public override void WriteManifest(Stream stream, SerializerSession session)
        {
            stream.WriteByte(Manifest);
        }

        public override void WriteValue(Stream stream, object value, SerializerSession session)
        {
        }

        public override object ReadValue(Stream stream, DeserializerSession session)
        {
            return new object();
        }

        public override Type GetElementType()
        {
            return typeof(object);
        }
    }
}