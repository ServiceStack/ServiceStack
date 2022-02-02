//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.IO;
using System.Text;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text
{
    public class TypeSerializer<T> : ITypeSerializer<T>
    {
        public bool CanCreateFromString(Type type)
        {
            return JsvReader.GetParseFn(type) != null;
        }

        /// <summary>
        /// Parses the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public T DeserializeFromString(string value)
        {
            if (string.IsNullOrEmpty(value)) return default(T);
            return (T)JsvReader<T>.Parse(value);
        }

        public T DeserializeFromReader(TextReader reader)
        {
            return DeserializeFromString(reader.ReadToEnd());
        }

        public string SerializeToString(T value)
        {
            if (value == null) return null;
            if (typeof(T) == typeof(string)) return value as string;

            var writer = StringWriterThreadStatic.Allocate();
            JsvWriter<T>.WriteObject(writer, value);
            return StringWriterThreadStatic.ReturnAndFree(writer);
        }

        public void SerializeToWriter(T value, TextWriter writer)
        {
            if (value == null) return;
            if (typeof(T) == typeof(string))
            {
                writer.Write(value);
                return;
            }

            JsvWriter<T>.WriteObject(writer, value);
        }
    }
}