// -----------------------------------------------------------------------
//   <copyright file="ByteArraySerializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using Wire.Extensions;

namespace Wire.ValueSerializers
{
    public class ByteArraySerializer : ValueSerializer
    {
        public const byte Manifest = 9;
        public static readonly ByteArraySerializer Instance = new ByteArraySerializer();

        public override void WriteManifest(Stream stream, SerializerSession session)
        {
            stream.WriteByte(Manifest);
        }

        public override void WriteValue(Stream stream, object value, SerializerSession session)
        {
            var bytes = (byte[]) value;
            stream.WriteLengthEncodedByteArray(bytes, session);

            if (session.Serializer.Options.PreserveObjectReferences)
            {
                session.TrackSerializedObject(bytes);
            }
        }

        public override object ReadValue(Stream stream, DeserializerSession session)
        {
            var res = stream.ReadLengthEncodedByteArray(session);
            if (session.Serializer.Options.PreserveObjectReferences)
            {
                session.TrackDeserializedObject(res);
            }
            return res;
        }

        public override Type GetElementType()
        {
            return typeof(byte[]);
        }
    }
}