// -----------------------------------------------------------------------
//   <copyright file="DelegateSerializerFactory.cs" company="Asynkron HB">
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
    public class DelegateSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type)
        {
            return type.GetTypeInfo().IsSubclassOf(typeof(Delegate));
        }

        public override bool CanDeserialize(Serializer serializer, Type type)
        {
            return CanSerialize(serializer, type);
        }

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var os = new ObjectSerializer(type);
            typeMapping.TryAdd(type, os);
            var methodInfoSerializer = serializer.GetSerializerByType(typeof(MethodInfo));
            var preserveObjectReferences = serializer.Options.PreserveObjectReferences;

            object Reader(Stream stream, DeserializerSession session)
            {
                var target = stream.ReadObject(session);
                var method = (MethodInfo) stream.ReadObject(session);
                var del = method.CreateDelegate(type, target);
                return del;
            }

            void Writer(Stream stream, object value, SerializerSession session)
            {
                var d = (Delegate) value;
                var method = d.GetMethodInfo();
                stream.WriteObjectWithManifest(d.Target, session);
                //less lookups, slightly faster
                stream.WriteObject(method, type, methodInfoSerializer, preserveObjectReferences, session);
            }

            os.Initialize(Reader, Writer);
            return os;
        }
    }
}