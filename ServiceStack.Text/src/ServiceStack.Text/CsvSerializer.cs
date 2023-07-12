using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using ServiceStack.Text.Common;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text
{
    public class CsvSerializer
    {
        //Don't emit UTF8 BOM by default
        public static Encoding UseEncoding { get; set; } = PclExport.Instance.GetUTF8Encoding(false);

        public static Action<object> OnSerialize { get; set; }

        private static Dictionary<Type, WriteObjectDelegate> WriteFnCache = new();
        internal static WriteObjectDelegate GetWriteFn(Type type)
        {
            try
            {
                if (WriteFnCache.TryGetValue(type, out var writeFn)) return writeFn;

                var genericType = typeof(CsvSerializer<>).MakeGenericType(type);
                var mi = genericType.GetStaticMethod("WriteFn");
                var writeFactoryFn = (Func<WriteObjectDelegate>)mi.MakeDelegate(
                    typeof(Func<WriteObjectDelegate>));

                writeFn = writeFactoryFn();

                Dictionary<Type, WriteObjectDelegate> snapshot, newCache;
                do
                {
                    snapshot = WriteFnCache;
                    newCache = new Dictionary<Type, WriteObjectDelegate>(WriteFnCache) {[type] = writeFn};

                } while (!ReferenceEquals(
                    Interlocked.CompareExchange(ref WriteFnCache, newCache, snapshot), snapshot));

                return writeFn;
            }
            catch (Exception ex)
            {
                Tracer.Instance.WriteError(ex);
                throw;
            }
        }

        private static Dictionary<Type, ParseStringDelegate> ReadFnCache = new();
        internal static ParseStringDelegate GetReadFn(Type type)
        {
            try
            {
                if (ReadFnCache.TryGetValue(type, out var writeFn)) return writeFn;

                var genericType = typeof(CsvSerializer<>).MakeGenericType(type);
                var mi = genericType.GetStaticMethod("ReadFn");
                var writeFactoryFn = (Func<ParseStringDelegate>)mi.MakeDelegate(
                    typeof(Func<ParseStringDelegate>));

                writeFn = writeFactoryFn();

                Dictionary<Type, ParseStringDelegate> snapshot, newCache;
                do
                {
                    snapshot = ReadFnCache;
                    newCache = new Dictionary<Type, ParseStringDelegate>(ReadFnCache) {[type] = writeFn};

                } while (!ReferenceEquals(
                    Interlocked.CompareExchange(ref ReadFnCache, newCache, snapshot), snapshot));

                return writeFn;
            }
            catch (Exception ex)
            {
                Tracer.Instance.WriteError(ex);
                throw;
            }
        }

        public static string SerializeToCsv<T>(IEnumerable<T> records)
        {
            var writer = StringWriterThreadStatic.Allocate();
            writer.WriteCsv(records);
            return StringWriterThreadStatic.ReturnAndFree(writer);
        }

        public static string SerializeToString<T>(T value)
        {
            if (value == null) return null;
            if (typeof(T) == typeof(string)) return value as string;

            var writer = StringWriterThreadStatic.Allocate();
            CsvSerializer<T>.WriteObject(writer, value);
            return StringWriterThreadStatic.ReturnAndFree(writer);
        }

        public static void SerializeToWriter<T>(T value, TextWriter writer)
        {
            if (value == null) return;
            if (typeof(T) == typeof(string))
            {
                writer.Write(value);
                return;
            }
            CsvSerializer<T>.WriteObject(writer, value);
        }

        public static void SerializeToStream<T>(T value, Stream stream)
        {
            if (value == null) return;
            var writer = new StreamWriter(stream, UseEncoding);
            CsvSerializer<T>.WriteObject(writer, value);
            writer.Flush();
        }

        public static void SerializeToStream(object obj, Stream stream)
        {
            if (obj == null) return;
            var writer = new StreamWriter(stream, UseEncoding);
            var writeFn = GetWriteFn(obj.GetType());
            writeFn(writer, obj);
            writer.Flush();
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            if (stream == null) return default(T);
            using (var reader = new StreamReader(stream, UseEncoding))
            {
                return DeserializeFromString<T>(reader.ReadToEnd());
            }
        }

        public static object DeserializeFromStream(Type type, Stream stream)
        {
            if (stream == null) return null;
            using (var reader = new StreamReader(stream, UseEncoding))
            {
                return DeserializeFromString(type, reader.ReadToEnd());
            }
        }

        public static T DeserializeFromReader<T>(TextReader reader)
        {
            return DeserializeFromString<T>(reader.ReadToEnd());
        }

        public static T DeserializeFromString<T>(string text)
        {
            if (string.IsNullOrEmpty(text)) return default;
            var results = CsvSerializer<T>.ReadObject(text);
            return ConvertFrom<T>(results);
        }

        public static object DeserializeFromString(Type type, string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var hold = JsState.IsCsv;
            JsState.IsCsv = true;
            try
            {
                var fn = GetReadFn(type);
                var result = fn(text);
                var converted = ConvertFrom(type, result);
                return converted;
            }
            finally
            {
                JsState.IsCsv = hold;
            }
        }

        public static void WriteLateBoundObject(TextWriter writer, object value)
        {
            if (value == null) return;
            var writeFn = GetWriteFn(value.GetType());
            writeFn(writer, value);
        }

        public static object ReadLateBoundObject(Type type, string value)
        {
            if (value == null) return null;
            var readFn = GetReadFn(type);
            return readFn(value);
        }

        internal static T ConvertFrom<T>(object results)
        {
            if (results is T variable)
                return variable;

            foreach (var ci in typeof(T).GetConstructors())
            {
                var ciParams = ci.GetParameters();
                if (ciParams.Length == 1)
                {
                    var pi = ciParams.First();
                    if (pi.ParameterType.IsAssignableFrom(typeof(T)))
                    {
                        var to = ci.Invoke(new[] { results });
                        return (T)to;
                    }
                }
            }

            return results.ConvertTo<T>();
        }

        internal static object ConvertFrom(Type type, object results)
        {
            if (type.IsInstanceOfType(results))
                return results;

            foreach (var ci in type.GetConstructors())
            {
                var ciParams = ci.GetParameters();
                if (ciParams.Length == 1)
                {
                    var pi = ciParams.First();
                    if (pi.ParameterType.IsAssignableFrom(type))
                    {
                        var to = ci.Invoke(new[] { results });
                        return to;
                    }
                }
            }

            return results.ConvertTo(type);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void InitAot<T>()
        {
            CsvSerializer<T>.WriteFn();
            CsvSerializer<T>.WriteObject(null, null);
            CsvWriter<T>.Write(null, default(IEnumerable<T>));
            CsvWriter<T>.WriteRow(null, default(T));
            CsvWriter<T>.WriteObject(null, default(IEnumerable<T>));
            CsvWriter<T>.WriteObjectRow(null, default(IEnumerable<T>));

            CsvReader<T>.ReadRow(null);
            CsvReader<T>.ReadObject(null);
            CsvReader<T>.ReadObjectRow(null);
            CsvReader<T>.ReadStringDictionary(null);
        }

        public static (string PropertyName, Type PropertyType)[] PropertiesFor<T>() => CsvSerializer<T>.Properties;
        public static (string PropertyName, Type PropertyType)[] PropertiesFor(Type type)
        {
            var genericType = typeof(CsvSerializer<>).MakeGenericType(type);
            var pi = genericType.GetProperty("Properties", BindingFlags.Public | BindingFlags.Static);
            var ret = ((string PropertyName, Type PropertyType)[]) pi!.GetValue(null, null);
            return ret;
        }
    }

    public static class CsvSerializer<T>
    {
        private static readonly WriteObjectDelegate WriteCacheFn;
        private static readonly ParseStringDelegate ReadCacheFn;

        public static (string PropertyName, Type PropertyType)[] Properties { get; } = Array.Empty<(string PropertyName, Type PropertyType)>(); 

        public static WriteObjectDelegate WriteFn()
        {
            return WriteCacheFn;
        }

        private const string IgnoreResponseStatus = "ResponseStatus";

        private static GetMemberDelegate valueGetter = null;
        private static WriteObjectDelegate writeElementFn = null;

        private static WriteObjectDelegate GetWriteFn()
        {
            PropertyInfo firstCandidate = null;
            Type bestCandidateEnumerableType = null;
            PropertyInfo bestCandidate = null;

            if (typeof(T).IsValueType)
            {
                return JsvWriter<T>.WriteObject;
            }

            //If type is an enumerable property itself write that
            bestCandidateEnumerableType = typeof(T).GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));
            if (bestCandidateEnumerableType != null)
            {
                var dictionaryOrKvps = typeof(T).HasInterface(typeof(IEnumerable<KeyValuePair<string, object>>))
                                    || typeof(T).HasInterface(typeof(IEnumerable<KeyValuePair<string, string>>));
                if (dictionaryOrKvps)
                {
                    return WriteSelf;
                }

                var elementType = bestCandidateEnumerableType.GetGenericArguments()[0];
                writeElementFn = CreateWriteFn(elementType);

                return WriteEnumerableType;
            }

            //Look for best candidate property if DTO
            if (typeof(T).IsDto() || typeof(T).HasAttribute<CsvAttribute>())
            {
                var properties = TypeConfig<T>.Properties;
                foreach (var propertyInfo in properties)
                {
                    if (propertyInfo.Name == IgnoreResponseStatus) continue;

                    if (propertyInfo.PropertyType == typeof(string)
                        || propertyInfo.PropertyType.IsValueType
                        || propertyInfo.PropertyType == typeof(byte[]))
                        continue;

                    if (firstCandidate == null)
                    {
                        firstCandidate = propertyInfo;
                    }

                    var enumProperty = propertyInfo.PropertyType
                        .GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));

                    if (enumProperty != null)
                    {
                        bestCandidateEnumerableType = enumProperty;
                        bestCandidate = propertyInfo;
                        break;
                    }
                }
            }

            //If is not DTO or no candidates exist, write self
            var noCandidatesExist = bestCandidate == null && firstCandidate == null;
            if (noCandidatesExist)
            {
                return WriteSelf;
            }

            //If is DTO and has an enumerable property serialize that
            if (bestCandidateEnumerableType != null)
            {
                valueGetter = bestCandidate.CreateGetter();
                var elementType = bestCandidateEnumerableType.GetGenericArguments()[0];
                writeElementFn = CreateWriteFn(elementType);

                return WriteEnumerableProperty;
            }

            //If is DTO and has non-enumerable, reference type property serialize that
            valueGetter = firstCandidate.CreateGetter();
            writeElementFn = CreateWriteRowFn(firstCandidate.PropertyType);

            return WriteNonEnumerableType;
        }

        private static WriteObjectDelegate CreateWriteFn(Type elementType)
        {
            return CreateCsvWriterFn(elementType, "WriteObject");
        }

        private static WriteObjectDelegate CreateWriteRowFn(Type elementType)
        {
            return CreateCsvWriterFn(elementType, "WriteObjectRow");
        }

        private static WriteObjectDelegate CreateCsvWriterFn(Type elementType, string methodName)
        {
            var genericType = typeof(CsvWriter<>).MakeGenericType(elementType);
            var mi = genericType.GetStaticMethod(methodName);
            var writeFn = (WriteObjectDelegate)mi.MakeDelegate(typeof(WriteObjectDelegate));
            return writeFn;
        }

        public static void WriteEnumerableType(TextWriter writer, object obj)
        {
            writeElementFn(writer, obj);
        }

        public static void WriteSelf(TextWriter writer, object obj)
        {
            CsvWriter<T>.WriteRow(writer, (T)obj);
        }

        public static void WriteEnumerableProperty(TextWriter writer, object obj)
        {
            if (obj == null) return; //AOT

            var enumerableProperty = valueGetter(obj);
            writeElementFn(writer, enumerableProperty);
        }

        public static void WriteNonEnumerableType(TextWriter writer, object obj)
        {
            var nonEnumerableType = valueGetter(obj);
            writeElementFn(writer, nonEnumerableType);
        }

        public static void WriteObject(TextWriter writer, object value)
        {
            var hold = JsState.IsCsv;
            JsState.IsCsv = true;
            try
            {
                CsvSerializer.OnSerialize?.Invoke(value);
                WriteCacheFn(writer, value);
            }
            finally
            {
                JsState.IsCsv = hold;
            }
        }


        static CsvSerializer()
        {
            if (typeof(T) == typeof(object))
            {
                WriteCacheFn = CsvSerializer.WriteLateBoundObject;
                ReadCacheFn = str => CsvSerializer.ReadLateBoundObject(typeof(T), str);
            }
            else
            {
                WriteCacheFn = GetWriteFn();
                ReadCacheFn = GetReadFn();

                var writers = WriteType<T, JsvTypeSerializer>.PropertyWriters;
                if (writers != null)
                    Properties = writers.Select(x => (x.propertyName, x.PropertyType)).ToArray();
            }
        }


        public static ParseStringDelegate ReadFn()
        {
            return ReadCacheFn;
        }

        private static SetMemberDelegate valueSetter = null;
        private static ParseStringDelegate readElementFn = null;

        private static ParseStringDelegate GetReadFn()
        {
            PropertyInfo firstCandidate = null;
            Type bestCandidateEnumerableType = null;
            PropertyInfo bestCandidate = null;

            if (typeof(T).IsValueType)
            {
                return JsvReader<T>.Parse;
            }

            //If type is an enumerable property itself write that
            bestCandidateEnumerableType = typeof(T).GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));
            if (bestCandidateEnumerableType != null)
            {
                var elementType = bestCandidateEnumerableType.GetGenericArguments()[0];
                readElementFn = CreateReadFn(elementType);

                return ReadEnumerableType;
            }

            //Look for best candidate property if DTO
            if (typeof(T).IsDto() || typeof(T).HasAttribute<CsvAttribute>())
            {
                var properties = TypeConfig<T>.Properties;
                foreach (var propertyInfo in properties)
                {
                    if (propertyInfo.Name == IgnoreResponseStatus) continue;

                    if (propertyInfo.PropertyType == typeof(string)
                        || propertyInfo.PropertyType.IsValueType
                        || propertyInfo.PropertyType == typeof(byte[]))
                        continue;

                    if (firstCandidate == null)
                    {
                        firstCandidate = propertyInfo;
                    }

                    var enumProperty = propertyInfo.PropertyType
                        .GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));

                    if (enumProperty != null)
                    {
                        bestCandidateEnumerableType = enumProperty;
                        bestCandidate = propertyInfo;
                        break;
                    }
                }
            }

            //If is not DTO or no candidates exist, write self
            var noCandidatesExist = bestCandidate == null && firstCandidate == null;
            if (noCandidatesExist)
            {
                return ReadSelf;
            }

            //If is DTO and has an enumerable property serialize that
            if (bestCandidateEnumerableType != null)
            {
                valueSetter = bestCandidate.CreateSetter();
                var elementType = bestCandidateEnumerableType.GetGenericArguments()[0];
                readElementFn = CreateReadFn(elementType);

                return ReadEnumerableProperty;
            }

            //If is DTO and has non-enumerable, reference type property serialize that
            valueSetter = firstCandidate.CreateSetter();
            readElementFn = CreateReadRowFn(firstCandidate.PropertyType);

            return ReadNonEnumerableType;
        }

        private static ParseStringDelegate CreateReadFn(Type elementType)
        {
            return CreateCsvReadFn(elementType, "ReadObject");
        }

        private static ParseStringDelegate CreateReadRowFn(Type elementType)
        {
            return CreateCsvReadFn(elementType, "ReadObjectRow");
        }

        private static ParseStringDelegate CreateCsvReadFn(Type elementType, string methodName)
        {
            var genericType = typeof(CsvReader<>).MakeGenericType(elementType);
            var mi = genericType.GetStaticMethod(methodName);
            var readFn = (ParseStringDelegate)mi.MakeDelegate(typeof(ParseStringDelegate));
            return readFn;
        }

        public static object ReadEnumerableType(string value)
        {
            return readElementFn(value);
        }

        public static object ReadSelf(string value)
        {
            return CsvReader<T>.ReadRow(value);
        }

        public static object ReadEnumerableProperty(string row)
        {
            if (row == null) return null; //AOT

            var value = readElementFn(row);
            var to = typeof(T).CreateInstance();
            valueSetter(to, value);
            return to;
        }

        public static object ReadNonEnumerableType(string row)
        {
            if (row == null) return null; //AOT

            var value = readElementFn(row);
            var to = typeof(T).CreateInstance();
            valueSetter(to, value);
            return to;
        }

        public static object ReadObject(string value)
        {
            if (value == null) return null; //AOT

            var hold = JsState.IsCsv;
            JsState.IsCsv = true;
            try
            {
                return ReadCacheFn(value);
            }
            finally
            {
                JsState.IsCsv = hold;
            }
        }
    }
    
    public class CsvStringSerializer : IStringSerializer
    {
        public To DeserializeFromString<To>(string serializedText) => 
            CsvSerializer.DeserializeFromString<To>(serializedText);
        public object DeserializeFromString(string serializedText, Type type) => 
            CsvSerializer.DeserializeFromString(type, serializedText);
        public string SerializeToString<TFrom>(TFrom @from) => 
            CsvSerializer.SerializeToString(@from);
    }
}