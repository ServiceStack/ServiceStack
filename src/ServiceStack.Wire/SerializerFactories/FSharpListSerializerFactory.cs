// -----------------------------------------------------------------------
//   <copyright file="FSharpListSerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class FSharpListSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type)
            => type.FullName.StartsWith("Microsoft.FSharp.Collections.FSharpList`1");

        public override bool CanDeserialize(Serializer serializer, Type type) => CanSerialize(serializer, type);

        private static Type GetEnumerableType(Type type)
        {
            return type
                .GetTypeInfo()
                .GetInterfaces()
                .Where(
                    intType =>
                        intType.GetTypeInfo().IsGenericType &&
                        intType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(intType => intType.GetTypeInfo().GetGenericArguments()[0])
                .FirstOrDefault();
        }

        private static TypedArray CompileToDelegate(MethodInfo method, Type argType)
        {
            var arg = Expression.Parameter(typeof(object));
            var castArg = Expression.Convert(arg, argType);
            var call = Expression.Call(method, new Expression[] {castArg});
            var castRes = Expression.Convert(call, typeof(object));
            var lambda = Expression.Lambda<TypedArray>(castRes, arg);
            var compiled = lambda.Compile();
            return compiled;
        }

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var x = new ObjectSerializer(type);
            typeMapping.TryAdd(type, x);

            var elementType = GetEnumerableType(type);
            var arrType = elementType.MakeArrayType();
            var listModule = type.GetTypeInfo().Assembly.GetType("Microsoft.FSharp.Collections.ListModule");
            var ofArray = listModule.GetTypeInfo().GetMethod("OfArray");
            var ofArrayConcrete = ofArray.MakeGenericMethod(elementType);
            var ofArrayCompiled = CompileToDelegate(ofArrayConcrete, arrType);
            var toArray = listModule.GetTypeInfo().GetMethod("ToArray");
            var toArrayConcrete = toArray.MakeGenericMethod(elementType);
            var toArrayCompiled = CompileToDelegate(toArrayConcrete, type);
            var preserveObjectReferences = serializer.Options.PreserveObjectReferences;

            void Writer(Stream stream, object o, SerializerSession session)
            {
                var arr = toArrayCompiled(o);
                var arrSerializer = serializer.GetSerializerByType(arrType);
                arrSerializer.WriteValue(stream, arr, session);
                if (preserveObjectReferences)
                {
                    session.TrackSerializedObject(o);
                }
            }

            object Reader(Stream stream, DeserializerSession session)
            {
                var arrSerializer = serializer.GetSerializerByType(arrType);
                var items = (Array) arrSerializer.ReadValue(stream, session);
                var res = ofArrayCompiled(items);
                if (preserveObjectReferences)
                {
                    session.TrackDeserializedObject(res);
                }
                return res;
            }

            x.Initialize(Reader, Writer);
            return x;
        }
    }
}