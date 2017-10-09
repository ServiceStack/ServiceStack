// -----------------------------------------------------------------------
//   <copyright file="ExpandoObjectSerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using Wire.Extensions;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class ExpandoObjectSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type) => type == typeof(ExpandoObject);

        public override bool CanDeserialize(Serializer serializer, Type type) => CanSerialize(serializer, type);

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var preserveObjectReferences = serializer.Options.PreserveObjectReferences;
            var ser = new ObjectSerializer(type);
            typeMapping.TryAdd(type, ser);
            var elementSerializer = serializer.GetSerializerByType(typeof(DictionaryEntry));

            object Reader(Stream stream, DeserializerSession session)
            {
                var instance = Activator.CreateInstance(type) as IDictionary<string, object>;

                if (preserveObjectReferences)
                {
                    session.TrackDeserializedObject(instance);
                }
                var count = stream.ReadInt32(session);
                for (var i = 0; i < count; i++)
                {
                    var entry = (KeyValuePair<string, object>) stream.ReadObject(session);
                    instance.Add(entry);
                }
                return instance;
            }

            void Writer(Stream stream, object obj, SerializerSession session)
            {
                if (preserveObjectReferences)
                {
                    session.TrackSerializedObject(obj);
                }
                var dict = obj as IDictionary<string, object>;
                // ReSharper disable once PossibleNullReferenceException
                Int32Serializer.WriteValueImpl(stream, dict.Count, session);
                foreach (var item in dict)
                {
                    stream.WriteObject(item, typeof(DictionaryEntry), elementSerializer, serializer.Options.PreserveObjectReferences, session);
                    // elementSerializer.WriteValue(stream,item,session);
                }
            }

            ser.Initialize(Reader, Writer);

            return ser;
        }
    }
}