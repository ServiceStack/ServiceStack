// -----------------------------------------------------------------------
//   <copyright file="ToSurrogateSerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Linq;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class ToSurrogateSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type)
        {
            var surrogate = serializer.Options.Surrogates.FirstOrDefault(s => s.IsSurrogateFor(type));
            return surrogate != null;
        }

        public override bool CanDeserialize(Serializer serializer, Type type) => false;

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var surrogate = serializer
                .Options
                .Surrogates
                .FirstOrDefault(s => s.IsSurrogateFor(type));
            // ReSharper disable once PossibleNullReferenceException
            var objectSerializer = new ObjectSerializer(surrogate.To);
            var toSurrogateSerializer = new ToSurrogateSerializer(surrogate.ToSurrogate);
            typeMapping.TryAdd(type, toSurrogateSerializer);

            serializer.CodeGenerator.BuildSerializer(serializer, objectSerializer);
            return toSurrogateSerializer;
        }
    }
}