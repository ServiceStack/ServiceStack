// -----------------------------------------------------------------------
//   <copyright file="ISerializableSerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------
#if NET45
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Wire.Extensions;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    // ReSharper disable once InconsistentNaming
    public class ISerializableSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type)
        {
            return typeof(ISerializable).IsAssignableFrom(type);
        }

        public override bool CanDeserialize(Serializer serializer, Type type)
        {
            return CanSerialize(serializer, type);
        }

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var serializableSerializer = new ObjectSerializer(type);
            typeMapping.TryAdd(type, serializableSerializer);

            object Reader(Stream stream, DeserializerSession session)
            {
                var dict = stream.ReadObject(session) as Dictionary<string, object>;
                var info = new SerializationInfo(type, new FormatterConverter());
                // ReSharper disable once PossibleNullReferenceException
                foreach (var item in dict)
                {
                    info.AddValue(item.Key, item.Value);
                }

                var ctor = type.GetConstructor(BindingFlagsEx.All, null, new[] {typeof(SerializationInfo), typeof(StreamingContext)}, null);
                var instance = ctor.Invoke(new object[] {info, new StreamingContext()});
                var deserializationCallback = instance as IDeserializationCallback;
                deserializationCallback?.OnDeserialization(this);
                return instance;
            }

            void Writer(Stream stream, object o, SerializerSession session)
            {
                var info = new SerializationInfo(type, new FormatterConverter());
                var serializable = o as ISerializable;
                // ReSharper disable once PossibleNullReferenceException
                serializable.GetObjectData(info, new StreamingContext());
                var dict = new Dictionary<string, object>();
                foreach (var item in info)
                {
                    dict.Add(item.Name, item.Value);
                }
                stream.WriteObjectWithManifest(dict, session);
            }

            serializableSerializer.Initialize(Reader, Writer);

            return serializableSerializer;
        }
    }
}

#endif