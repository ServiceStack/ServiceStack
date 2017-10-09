// -----------------------------------------------------------------------
//   <copyright file="TypeEx.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Wire.Internal;

#if NET45
using System.Runtime.Serialization;

#endif

namespace Wire.Extensions
{
    public static class TypeEx
    {
        const string VERSION_REGEX = @", Version=(\d+([.]\d+)?([.]\d+)?([.]\d+)?.*?)";
        //Why not inline typeof you ask?
        //Because it actually generates calls to get the type.
        //We prefetch all primitives here
        public static readonly Type SystemObject = typeof(object);
        public static readonly Type Int32Type = typeof(int);
        public static readonly Type Int64Type = typeof(long);
        public static readonly Type Int16Type = typeof(short);
        public static readonly Type UInt32Type = typeof(uint);
        public static readonly Type UInt64Type = typeof(ulong);
        public static readonly Type UInt16Type = typeof(ushort);
        public static readonly Type ByteType = typeof(byte);
        public static readonly Type SByteType = typeof(sbyte);
        public static readonly Type BoolType = typeof(bool);
        public static readonly Type DateTimeType = typeof(DateTime);
        public static readonly Type StringType = typeof(string);
        public static readonly Type GuidType = typeof(Guid);
        public static readonly Type FloatType = typeof(float);
        public static readonly Type DoubleType = typeof(double);
        public static readonly Type DecimalType = typeof(decimal);
        public static readonly Type CharType = typeof(char);
        public static readonly Type ByteArrayType = typeof(byte[]);
        public static readonly Type TypeType = typeof(Type);
        public static readonly Type RuntimeType = Type.GetType("System.RuntimeType");

        public static bool IsWirePrimitive(this Type type)
        {
            return type == Int32Type ||
                   type == Int64Type ||
                   type == Int16Type ||
                   type == UInt32Type ||
                   type == UInt64Type ||
                   type == UInt16Type ||
                   type == ByteType ||
                   type == SByteType ||
                   type == DateTimeType ||
                   type == BoolType ||
                   type == StringType ||
                   type == GuidType ||
                   type == FloatType ||
                   type == DoubleType ||
                   type == DecimalType ||
                   type == CharType;
            //add TypeSerializer with null support
        }

#if !NET45
    //HACK: the GetUnitializedObject actually exists in .NET Core, its just not public
        private static readonly Func<Type, object> getUninitializedObjectDelegate = (Func<Type, object>)
            typeof(string)
                .GetTypeInfo()
                .Assembly
                .GetType("System.Runtime.Serialization.FormatterServices")
                ?.GetTypeInfo()
                ?.GetMethod("GetUninitializedObject", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                ?.CreateDelegate(typeof(Func<Type, object>));

        public static object GetEmptyObject(this Type type)
        {
            return getUninitializedObjectDelegate(type);
        }
#else
        public static object GetEmptyObject(this Type type)
        {
            return FormatterServices.GetUninitializedObject(type);
        }
#endif

        public static bool IsOneDimensionalArray(this Type type)
        {
            return type.IsArray && type.GetArrayRank() == 1;
        }

        public static bool IsOneDimensionalPrimitiveArray(this Type type)
        {
            return type.IsArray && type.GetArrayRank() == 1 && type.GetElementType().IsWirePrimitive();
        }

        private static readonly ConcurrentDictionary<ByteArrayKey, Type> TypeNameLookup =
            new ConcurrentDictionary<ByteArrayKey, Type>(ByteArrayKeyComparer.Instance);

        public static byte[] GetTypeManifest(IReadOnlyCollection<byte[]> fieldNames)
        {
            IEnumerable<byte> result = new[] {(byte) fieldNames.Count};
            foreach (var name in fieldNames)
            {
                var encodedLength = BitConverter.GetBytes(name.Length);
                result = result.Concat(encodedLength);
                result = result.Concat(name);
            }
            var versionTolerantHeader = result.ToArray();
            return versionTolerantHeader;
        }

        private static Type GetTypeFromManifestName(Stream stream, DeserializerSession session)
        {
            var bytes = stream.ReadLengthEncodedByteArray(session);
            var byteArr = ByteArrayKey.Create(bytes);
            return TypeNameLookup.GetOrAdd(byteArr, b =>
            {
                var shortName = StringEx.FromUtf8Bytes(b.Bytes, 0, b.Bytes.Length);
                var typename = ToQualifiedAssemblyName(shortName);
                return Type.GetType(typename, true);
            });
        }

        public static Type GetTypeFromManifestFull(Stream stream, DeserializerSession session)
        {
            var type = GetTypeFromManifestName(stream, session);
            session.TrackDeserializedType(type);
            return type;
        }

        public static Type GetTypeFromManifestVersion(Stream stream, DeserializerSession session)
        {
            var type = GetTypeFromManifestName(stream, session);

            var fieldCount = stream.ReadByte();
            for (var i = 0; i < fieldCount; i++)
            {
                var fieldName = stream.ReadLengthEncodedByteArray(session);
            }

            session.TrackDeserializedTypeWithVersion(type, null);
            return type;
        }

        public static Type GetTypeFromManifestIndex(int typeId, DeserializerSession session)
        {
            var type = session.GetTypeFromTypeId(typeId);
            return type;
        }

        public static bool IsNullable(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Type GetNullableElement(this Type type)
        {
            return type.GetTypeInfo().GetGenericArguments()[0];
        }

        public static bool IsFixedSizeType(this Type type)
        {
            return type == Int16Type ||
                   type == Int32Type ||
                   type == Int64Type ||
                   type == BoolType ||
                   type == UInt16Type ||
                   type == UInt32Type ||
                   type == UInt64Type ||
                   type == CharType;
        }

        public static int GetTypeSize(this Type type)
        {
            if (type == Int16Type)
            {
                return sizeof(short);
            }
            if (type == Int32Type)
            {
                return sizeof(int);
            }
            if (type == Int64Type)
            {
                return sizeof(long);
            }
            if (type == BoolType)
            {
                return sizeof(bool);
            }
            if (type == UInt16Type)
            {
                return sizeof(ushort);
            }
            if (type == UInt32Type)
            {
                return sizeof(uint);
            }
            if (type == UInt64Type)
            {
                return sizeof(ulong);
            }
            if (type == CharType)
            {
                return sizeof(char);
            }

            throw new NotSupportedException();
        }

        private static readonly string CoreAssemblyName = GetCoreAssemblyName();

        private static string GetCoreAssemblyName()
        {
            var name = 1.GetType().AssemblyQualifiedName;
            var part = name.Substring(name.IndexOf(", Version", StringComparison.Ordinal));
            return part;
        }

        public static string GetShortAssemblyQualifiedName(this Type self)
        {
            var name = self.AssemblyQualifiedName;
            return ReplaceTokens(name);
        }

        public static string ToQualifiedAssemblyName(string shortName)
        {
            var res = shortName.Replace(",%core%", CoreAssemblyName);
            return res;
        }

        public static string ReplaceTokens(string input)
        {
            var name = input;
            name = name.Replace(CoreAssemblyName, ",%core%");
            name = name.Replace(", Culture=neutral", "");
            name = name.Replace(", PublicKeyToken=null", "");
            name = ReplaceVersion(name);
            return name;
        }

        static string ReplaceVersion(string input)
        {
            var regex = new Regex(VERSION_REGEX);
            return regex.Replace(input, "");
        }
    }
}