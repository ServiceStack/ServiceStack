// -----------------------------------------------------------------------
//   <copyright file="ArraySerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using Wire.Extensions;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class ArraySerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type) => type.IsOneDimensionalArray();

        public override bool CanDeserialize(Serializer serializer, Type type) => CanSerialize(serializer, type);

        private static void WriteValues<T>(T[] array, Stream stream, Type elementType, ValueSerializer elementSerializer,
            SerializerSession session, bool preserveObjectReferences)
        {
            if (preserveObjectReferences)
            {
                session.TrackSerializedObject(array);
            }

            Int32Serializer.WriteValueImpl(stream, array.Length, session);
            foreach (var value in array)
            {
                stream.WriteObject(value, elementType, elementSerializer, preserveObjectReferences, session);
            }
        }

        private static object ReadValues<T>(Stream stream, DeserializerSession session, bool preserveObjectReferences)
        {
            var length = stream.ReadInt32(session);
            var array = new T[length];
            if (preserveObjectReferences)
            {
                session.TrackDeserializedObject(array);
            }
            for (var i = 0; i < length; i++)
            {
                var value = (T) stream.ReadObject(session);
                array[i] = value;
            }
            return array;
        }

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var arraySerializer = new ObjectSerializer(type);

            var elementType = type.GetElementType();
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