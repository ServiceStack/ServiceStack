// -----------------------------------------------------------------------
//   <copyright file="ValueSerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public abstract class ValueSerializerFactory
    {
        public abstract bool CanSerialize(Serializer serializer, Type type);
        public abstract bool CanDeserialize(Serializer serializer, Type type);

        public abstract ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping);
    }
}