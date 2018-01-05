// -----------------------------------------------------------------------
//   <copyright file="LinkedListSerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Wire.Extensions;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class LinkedListSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type)
        {
            return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(LinkedList<>);
        }

        public override bool CanDeserialize(Serializer serializer, Type type)
        {
            return CanSerialize(serializer, type);
        }

        private static void WriteValues<T>(LinkedList<T> llist, Stream stream, Type elementType, ValueSerializer elementSerializer,
    SerializerSession session, bool preserveObjectReferences)
        {
            if (preserveObjectReferences)
            {
                session.TrackSerializedObject(llist);
            }
            
            Int32Serializer.WriteValueImpl(stream, llist.Count, session);
            foreach (var value in llist)
            {
                stream.WriteObject(value, elementType, elementSerializer, preserveObjectReferences, session);
            }
        }

        private static object ReadValues<T>(Stream stream, DeserializerSession session, bool preserveObjectReferences)
        {
            var length = stream.ReadInt32(session);
            var llist = new LinkedList<T>();
            if (preserveObjectReferences)
            {
                session.TrackDeserializedObject(llist);
            }
            for (var i = 0; i < length; i++)
            {
                var value = (T)stream.ReadObject(session);
                llist.AddLast(value);
            }
            return llist;
        }

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var arraySerializer = new ObjectSerializer(type);

            var elementType = type.GetTypeInfo().GetGenericArguments()[0];
            var elementSerializer = serializer.GetSerializerByType(elementType);
            var preserveObjectReferences = serializer.Options.PreserveObjectReferences;

            var readGeneric = GetType().GetTypeInfo().GetMethod(nameof(ReadValues), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(elementType);
            var writeGeneric = GetType().GetTypeInfo().GetMethod(nameof(WriteValues), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(elementType);

            object Reader(Stream stream, DeserializerSession session)
            {
                //Stream stream, DeserializerSession session, bool preserveObjectReferences
                var res = readGeneric.Invoke(null, new object[] {stream, session, preserveObjectReferences});
                return res;
            }

            void Writer(Stream stream, object arr, SerializerSession session)
            {
                //T[] array, Stream stream, Type elementType, ValueSerializer elementSerializer, SerializerSession session, bool preserveObjectReferences
                writeGeneric.Invoke(null, new[] {arr, stream, elementType, elementSerializer, session, preserveObjectReferences});
            }

            arraySerializer.Initialize(Reader, Writer);
            typeMapping.TryAdd(type, arraySerializer);
            return arraySerializer;
        }
    }
}