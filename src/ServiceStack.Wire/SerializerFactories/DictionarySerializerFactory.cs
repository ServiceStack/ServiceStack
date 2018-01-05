// -----------------------------------------------------------------------
//   <copyright file="DictionarySerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Wire.Extensions;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class DictionarySerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type) => IsInterface(type);

        private static bool IsInterface(Type type)
        {
            return type
                .GetTypeInfo()
                .GetInterfaces()
                .Select(
                    t =>
                        t.GetTypeInfo().IsGenericType &&
                        t.GetTypeInfo().GetGenericTypeDefinition() == typeof(IDictionary<,>))
                .Any(isDict => isDict);
        }

        public override bool CanDeserialize(Serializer serializer, Type type) => IsInterface(type);

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var preserveObjectReferences = serializer.Options.PreserveObjectReferences;
            var ser = new ObjectSerializer(type);
            typeMapping.TryAdd(type, ser);
            var elementSerializer = serializer.GetSerializerByType(typeof(DictionaryEntry));

            object Reader(Stream stream, DeserializerSession session)
            {
                throw new NotSupportedException("Generic IDictionary<TKey,TValue> are not yet supported");
#pragma warning disable CS0162 // Unreachable code detected
                var instance = Activator.CreateInstance(type);
#pragma warning restore CS0162 // Unreachable code detected
                if (preserveObjectReferences)
                {
                    session.TrackDeserializedObject(instance);
                }
                var count = stream.ReadInt32(session);
                var entries = new DictionaryEntry[count];
                for (var i = 0; i < count; i++)
                {
                    var entry = (DictionaryEntry) stream.ReadObject(session);
                    entries[i] = entry;
                }
                //TODO: populate dictionary
                return instance;
            }

            void Writer(Stream stream, object obj, SerializerSession session)
            {
                if (preserveObjectReferences)
                {
                    session.TrackSerializedObject(obj);
                }
                var dict = obj as IDictionary;
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