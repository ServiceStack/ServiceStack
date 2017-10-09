// -----------------------------------------------------------------------
//   <copyright file="StringSerializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using Wire.Extensions;
using Wire.Internal;

namespace Wire.ValueSerializers
{
    public class StringSerializer : ValueSerializer
    {
        public const byte Manifest = 7;
        public static readonly StringSerializer Instance = new StringSerializer();

        public static void WriteValueImpl(Stream stream, string s, SerializerSession session)
        {
            var bytes = NoAllocBitConverter.GetBytes(s, session, out int byteCount);
            stream.Write(bytes, 0, byteCount);
        }

        public static string ReadValueImpl(Stream stream, DeserializerSession session)
        {
            return stream.ReadString(session);
        }

        public override void WriteManifest(Stream stream, SerializerSession session)
        {
            stream.WriteByte(Manifest);
        }

        public override void WriteValue(Stream stream, object value, SerializerSession session)
        {
            WriteValueImpl(stream, (string) value, session);
        }

        public override object ReadValue(Stream stream, DeserializerSession session)
        {
            return ReadValueImpl(stream, session);
        }

        public override Type GetElementType()
        {
            return typeof(string);
        }
    }
}