// -----------------------------------------------------------------------
//   <copyright file="EnumerableSerializerFactory.cs" company="Asynkron HB">
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
    public class EnumerableSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type)
        {
            //TODO: check for constructor with IEnumerable<T> param

            if (!type.GetTypeInfo().GetMethods().Any(m => m.Name == "AddRange" || m.Name == "Add"))
            {
                return false;
            }

            if (type.GetTypeInfo().GetProperty("Count") == null)
            {
                return false;
            }

            var isGenericEnumerable = GetEnumerableType(type) != null;
            if (isGenericEnumerable)
            {
                return true;
            }

            if (typeof(ICollection).GetTypeInfo().IsAssignableFrom(type))
            {
                return true;
            }

            return false;
        }

        public override bool CanDeserialize(Serializer serializer, Type type)
        {
            return CanSerialize(serializer, type);
        }

        private static Type GetEnumerableType(Type type)
        {
            return type
                .GetTypeInfo()
                .GetInterfaces()
                .Where(
                    intType =>
                        intType.GetTypeInfo().IsGenericType &&
                        intType.GetTypeInfo().GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(intType => intType.GetTypeInfo().GetGenericArguments()[0])
                .FirstOrDefault();
        }

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var x = new ObjectSerializer(type);
            typeMapping.TryAdd(type, x);
            var preserveObjectReferences = serializer.Options.PreserveObjectReferences;

            var elementType = GetEnumerableType(type) ?? typeof(object);
            var elementSerializer = serializer.GetSerializerByType(elementType);

            var countProperty = type.GetTypeInfo().GetProperty("Count");
            var addRange = type.GetTypeInfo().GetMethod("AddRange");
            var add = type.GetTypeInfo().GetMethod("Add");

            int CountGetter(object o) => (int) countProperty.GetValue(o);

            object Reader(Stream stream, DeserializerSession session)
            {
                var instance = Activator.CreateInstance(type);
                if (preserveObjectReferences)
                {
                    session.TrackDeserializedObject(instance);
                }

                var count = stream.ReadInt32(session);

                if (addRange != null)
                {
                    var items = Array.CreateInstance(elementType, count);
                    for (var i = 0; i < count; i++)
                    {
                        var value = stream.ReadObject(session);
                        items.SetValue(value, i);
                    }
                    //HACK: this needs to be fixed, codegenerated or whatever

                    addRange.Invoke(instance, new object[] {items});
                    return instance;
                }
                if (add != null)
                {
                    for (var i = 0; i < count; i++)
                    {
                        var value = stream.ReadObject(session);
                        add.Invoke(instance, new[] {value});
                    }
                }

                return instance;
            }

            void Writer(Stream stream, object o, SerializerSession session)
            {
                if (preserveObjectReferences)
                {
                    session.TrackSerializedObject(o);
                }
                Int32Serializer.WriteValueImpl(stream, CountGetter(o), session);
                var enumerable = o as IEnumerable;
                // ReSharper disable once PossibleNullReferenceException
                foreach (var value in enumerable)
                {
                    stream.WriteObject(value, elementType, elementSerializer, preserveObjectReferences, session);
                }
            }

            x.Initialize(Reader, Writer);
            return x;
        }
    }
}