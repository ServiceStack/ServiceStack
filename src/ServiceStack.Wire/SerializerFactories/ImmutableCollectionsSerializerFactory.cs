// -----------------------------------------------------------------------
//   <copyright file="ImmutableCollectionsSerializerFactory.cs" company="Asynkron HB">
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
    public class ImmutableCollectionsSerializerFactory : ValueSerializerFactory
    {
        private const string ImmutableCollectionsNamespace = "System.Collections.Immutable";
        private const string ImmutableCollectionsAssembly = "System.Collections.Immutable";

        public override bool CanSerialize(Serializer serializer, Type type)
        {
            if (type.Namespace == null || !type.Namespace.Equals(ImmutableCollectionsNamespace))
            {
                return false;
            }
            var isGenericEnumerable = GetEnumerableType(type) != null;
            if (isGenericEnumerable)
            {
                return true;
            }

            return false;
        }

        public override bool CanDeserialize(Serializer serializer, Type type) => CanSerialize(serializer, type);

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

            var typeName = type.Name;
            var genericSufixIdx = typeName.IndexOf('`');
            typeName = genericSufixIdx != -1 ? typeName.Substring(0, genericSufixIdx) : typeName;
            var creatorType =
                Type.GetType(
                    ImmutableCollectionsNamespace + "." + typeName + ", " + ImmutableCollectionsAssembly, true);

            var genericTypes = elementType.GetTypeInfo().IsGenericType
                ? elementType.GetTypeInfo().GetGenericArguments()
                : new[] {elementType};
            var createRange = creatorType.GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(methodInfo => methodInfo.Name == "CreateRange" && methodInfo.GetParameters().Length == 1)
                .MakeGenericMethod(genericTypes);

            void Writer(Stream stream, object o, SerializerSession session)
            {
                var enumerable = o as ICollection;
                if (enumerable == null)
                {
                    // object can be IEnumerable but not ICollection i.e. ImmutableQueue
                    var e = (IEnumerable) o;
                    var list = e.Cast<object>().ToList(); //

                    enumerable = list;
                }
                Int32Serializer.WriteValueImpl(stream, enumerable.Count, session);
                foreach (var value in enumerable)
                {
                    stream.WriteObject(value, elementType, elementSerializer, preserveObjectReferences, session);
                }
                if (preserveObjectReferences)
                {
                    session.TrackSerializedObject(o);
                }
            }

            object Reader(Stream stream, DeserializerSession session)
            {
                var count = stream.ReadInt32(session);
                var items = Array.CreateInstance(elementType, count);
                for (var i = 0; i < count; i++)
                {
                    var value = stream.ReadObject(session);
                    items.SetValue(value, i);
                }

                var instance = createRange.Invoke(null, new object[] {items});
                if (preserveObjectReferences)
                {
                    session.TrackDeserializedObject(instance);
                }
                return instance;
            }

            x.Initialize(Reader, Writer);
            return x;
        }
    }
}