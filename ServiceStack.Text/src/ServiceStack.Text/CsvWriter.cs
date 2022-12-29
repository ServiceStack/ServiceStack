using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ServiceStack.Text.Common;

namespace ServiceStack.Text
{
    internal class CsvDictionaryWriter
    {
        public static void WriteRow(TextWriter writer, IEnumerable<string> row)
        {
            if (writer == null) return; //AOT

            var ranOnce = false;
            foreach (var field in row)
            {
                CsvWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

                writer.Write(field.ToCsvField());
            }
            writer.Write(CsvConfig.RowSeparatorString);
        }

        public static void WriteObjectRow(TextWriter writer, IEnumerable<object> row)
        {
            if (writer == null) return; //AOT

            var ranOnce = false;
            foreach (var field in row)
            {
                CsvWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

                writer.Write(field.ToCsvField());
            }
            writer.Write(CsvConfig.RowSeparatorString);
        }

        public static void Write(TextWriter writer, IEnumerable<KeyValuePair<string, object>> records)
        {
            if (records == null) return; //AOT

            var requireHeaders = !CsvConfig<IEnumerable<KeyValuePair<string, object>>>.OmitHeaders;
            if (requireHeaders)
            {
                var keys = records.Select(x => x.Key);
                WriteRow(writer, keys);
            }

            var values = records.Select(x => x.Value);
            WriteObjectRow(writer, values);
        }

        public static void Write(TextWriter writer, IEnumerable<KeyValuePair<string, string>> records)
        {
            if (records == null) return; //AOT

            var requireHeaders = !CsvConfig<IEnumerable<KeyValuePair<string, string>>>.OmitHeaders;
            if (requireHeaders)
            {
                var keys = records.Select(x => x.Key);
                WriteRow(writer, keys);
            }

            var values = records.Select(x => x.Value);
            WriteObjectRow(writer, values);
        }

        public static void Write(TextWriter writer, IEnumerable<IDictionary<string, object>> records) 
        {
            if (records == null) return; //AOT

            var requireHeaders = !CsvConfig<Dictionary<string, object>>.OmitHeaders;
            foreach (var record in records)
            {
                if (requireHeaders)
                {
                    if (record != null)
                        WriteRow(writer, record.Keys);

                    requireHeaders = false;
                }
                if (record != null) 
                    WriteObjectRow(writer, record.Values);
            }
        }

        public static void Write(TextWriter writer, IEnumerable<IDictionary<string, string>> records)
        {
            if (records == null) return; //AOT

            var allKeys = new HashSet<string>();
            var cachedRecords = new List<IDictionary<string, string>>();

            foreach (var record in records)
            {
                foreach (var key in record.Keys)
                {
                    if (!allKeys.Contains(key))
                    {
                        allKeys.Add(key);
                    }
                }
                cachedRecords.Add(record);
            }

            var headers = allKeys.OrderBy(key => key).ToList();
            if (!CsvConfig<Dictionary<string, string>>.OmitHeaders)
            {
                WriteRow(writer, headers);
            }
            foreach (var cachedRecord in cachedRecords)
            {
                var fullRecord = headers.ConvertAll(header => 
                    cachedRecord.ContainsKey(header) ? cachedRecord[header] : null);
                WriteRow(writer, fullRecord);
            }
        }
    }

    public static class CsvWriter
    {
        public static bool HasAnyEscapeChars(string value)
        {
            return !string.IsNullOrEmpty(value) 
               && (CsvConfig.EscapeStrings.Any(value.Contains)
                    || value[0] == JsWriter.ListStartChar
                    || value[0] == JsWriter.MapStartChar);
        }

        internal static void WriteItemSeperatorIfRanOnce(TextWriter writer, ref bool ranOnce)
        {
            if (ranOnce)
                writer.Write(CsvConfig.ItemSeperatorString);
            else
                ranOnce = true;
        }
    }

    public class CsvWriter<T>
    {
        public const char DelimiterChar = ',';

        public static List<string> Headers { get; set; }

        internal static List<GetMemberDelegate<T>> PropertyGetters;
        internal static List<PropertyInfo> PropertyInfos;

        private static readonly WriteObjectDelegate OptimizedWriter;

        static CsvWriter()
        {
            if (typeof(T) == typeof(string))
            {
                OptimizedWriter = (w, o) => WriteRow(w, (IEnumerable<string>)o);
                return;
            }

            Reset();
        }

        internal static void Reset()
        {
            Headers = new List<string>();

            PropertyGetters = new List<GetMemberDelegate<T>>();
            PropertyInfos = new List<PropertyInfo>();
            foreach (var propertyInfo in TypeConfig<T>.Properties)
            {
                if (!propertyInfo.CanRead || propertyInfo.GetGetMethod(nonPublic:true) == null) continue;
                if (!TypeSerializer.CanCreateFromString(propertyInfo.PropertyType)) continue;

                PropertyGetters.Add(propertyInfo.CreateGetter<T>());
                PropertyInfos.Add(propertyInfo);
                
                var propertyName = propertyInfo.Name;
                var dcsDataMemberName = propertyInfo.GetDataMemberName();
                if (dcsDataMemberName != null)
                    propertyName = dcsDataMemberName;
                Headers.Add(propertyName);
            }
        }

        internal static void ConfigureCustomHeaders(Dictionary<string, string> customHeadersMap)
        {
            Reset();

            for (var i = Headers.Count - 1; i >= 0; i--)
            {
                var oldHeader = Headers[i];
                if (!customHeadersMap.TryGetValue(oldHeader, out var newHeaderValue))
                {
                    Headers.RemoveAt(i);
                    PropertyGetters.RemoveAt(i);
                }
                else
                {
                    Headers[i] = newHeaderValue.EncodeJsv();
                }
            }
        }

        private static List<string> GetSingleRow(IEnumerable<T> records, Type recordType)
        {
            var row = new List<string>();
            foreach (var value in records)
            {
                var strValue = recordType == typeof(string)
                   ? value as string
                   : TypeSerializer.SerializeToString(value);

                row.Add(strValue);
            }
            return row;
        }

        public static List<List<string>> GetRows(IEnumerable<T> records)
        {
            var rows = new List<List<string>>();

            if (records == null) return rows;

            if (typeof(T).IsValueType || typeof(T) == typeof(string))
            {
                rows.Add(GetSingleRow(records, typeof(T)));
                return rows;
            }

            foreach (var record in records)
            {
                var row = new List<string>();
                foreach (var propertyGetter in PropertyGetters)
                {
                    var value = propertyGetter(record) ?? "";

                    var valueStr = value as string;
                    var strValue = valueStr ?? TypeSerializer.SerializeToString(value);

                    row.Add(strValue);
                }
                rows.Add(row);
            }

            return rows;
        }

        public static void WriteObject(TextWriter writer, object records)
        {
            if (writer == null) return; //AOT

            Write(writer, (IEnumerable<T>)records);
        }

        public static void WriteObjectRow(TextWriter writer, object record)
        {
            if (writer == null) return; //AOT

            WriteRow(writer, (T)record);
        }

        public static void Write(TextWriter writer, IEnumerable<T> records)
        {
            if (writer == null) return; //AOT
            if (records == null) return;

            if (typeof(T) == typeof(Dictionary<string, string>) || typeof(T) == typeof(IDictionary<string, string>))
            {
                CsvDictionaryWriter.Write(writer, (IEnumerable<IDictionary<string, string>>)records);
                return;
            }

            if (typeof(T).IsAssignableFrom(typeof(Dictionary<string, object>))) //also does `object`
            {
                var dynamicList = records.Select(x => x.ToObjectDictionary()).ToList();
                CsvDictionaryWriter.Write(writer, dynamicList);
                return;
            }

            if (OptimizedWriter != null)
            {
                OptimizedWriter(writer, records);
                return;
            }

            var recordsList = records.ToList();

            var headers = Headers;
            var propGetters = PropertyGetters;
            var treatAsSingleRow = typeof(T).IsValueType || typeof(T) == typeof(string);
            
            if (!treatAsSingleRow && JsConfig.ExcludeDefaultValues)
            {
                var hasValues = new bool[headers.Count];
                var defaultValues = new object[headers.Count];
                for (var i = 0; i < PropertyInfos.Count; i++)
                {
                    defaultValues[i] = PropertyInfos[i].PropertyType.GetDefaultValue();
                }

                foreach (var record in recordsList)
                {
                    for (var i = 0; i < propGetters.Count; i++)
                    {
                        var propGetter = propGetters[i];
                        var value = propGetter(record);
                        
                        if (value != null && !value.Equals(defaultValues[i]))
                            hasValues[i] = true;
                    }
                }

                if (hasValues.Any(x => x == false))
                {
                    var newHeaders = new List<string>();
                    var newGetters = new List<GetMemberDelegate<T>>();

                    for (int i = 0; i < hasValues.Length; i++)
                    {
                        if (hasValues[i])
                        {
                            newHeaders.Add(headers[i]);
                            newGetters.Add(propGetters[i]);
                        }
                    }

                    headers = newHeaders;
                    propGetters = newGetters;
                }
            }
            
            if (!CsvConfig<T>.OmitHeaders && headers.Count > 0)
            {
                var ranOnce = false;
                foreach (var header in headers)
                {
                    CsvWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

                    writer.Write(header);
                }
                writer.Write(CsvConfig.RowSeparatorString);
            }

            if (treatAsSingleRow)
            {
                var singleRow = GetSingleRow(recordsList, typeof(T));
                WriteRow(writer, singleRow);
                return;
            }

            var row = new string[headers.Count];
            foreach (var record in recordsList)
            {
                for (var i = 0; i < propGetters.Count; i++)
                {
                    var propGetter = propGetters[i];
                    var value = propGetter(record) ?? "";

                    var strValue = value is string s
                       ? s
                       : TypeSerializer.SerializeToString(value).StripQuotes();

                    row[i] = strValue;
                }
                WriteRow(writer, row);
            }
        }

        public static void WriteRow(TextWriter writer, T row)
        {
            if (writer == null) return; //AOT

            if (row is IEnumerable<KeyValuePair<string, object>> kvps)
            {
                CsvDictionaryWriter.Write(writer, kvps);
            }
            else if (row is IEnumerable<KeyValuePair<string, string>> kvpStrings)
            {
                CsvDictionaryWriter.Write(writer, kvpStrings);
            }
            else
            {
                Write(writer, new[] { row });
            }
        }

        public static void WriteRow(TextWriter writer, IEnumerable<string> row)
        {
            if (writer == null) return; //AOT

            var ranOnce = false;
            foreach (var field in row)
            {
                CsvWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

                writer.Write(field.ToCsvField());
            }
            writer.Write(CsvConfig.RowSeparatorString);
        }

        public static void Write(TextWriter writer, IEnumerable<List<string>> rows)
        {
            if (writer == null) return; //AOT

            if (Headers.Count > 0)
            {
                var ranOnce = false;
                foreach (var header in Headers)
                {
                    CsvWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

                    writer.Write(header);
                }
                writer.Write(CsvConfig.RowSeparatorString);
            }

            foreach (var row in rows)
            {
                WriteRow(writer, row);
            }
        }
    }

}