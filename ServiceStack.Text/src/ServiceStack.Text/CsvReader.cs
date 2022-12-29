using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text.Common;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text
{
    public class CsvReader
    {
        public static List<string> ParseLines(string csv)
        {
            var rows = new List<string>();
            if (string.IsNullOrEmpty(csv))
                return rows;

            var withinQuotes = false;
            var lastPos = 0;

            var i = -1;
            var len = csv.Length;
            while (++i < len)
            {
                var c = csv[i];
                if (c == JsWriter.QuoteChar)
                {
                    var isLiteralQuote = i + 1 < len && csv[i + 1] == JsWriter.QuoteChar;
                    if (isLiteralQuote)
                    {
                        i++;
                        continue;
                    }

                    withinQuotes = !withinQuotes;
                }

                if (withinQuotes)
                    continue;

                if (c == JsWriter.LineFeedChar)
                {
                    var str = i > 0 && csv[i - 1] == JsWriter.ReturnChar
                        ? csv.Substring(lastPos, i - lastPos - 1)
                        : csv.Substring(lastPos, i - lastPos);

                    if (str.Length > 0)
                        rows.Add(str);
                    lastPos = i + 1;
                }
            }

            if (i > lastPos)
            {
                var str = csv.Substring(lastPos, i - lastPos);
                if (str.Length > 0)
                    rows.Add(str);
            }

            return rows;
        }

        public static List<string> ParseFields(string line) => ParseFields(line, null);
        public static List<string> ParseFields(string line, Func<string,string> parseFn)
        {
            var to = new List<string>();
            if (string.IsNullOrEmpty(line))
                return to;

            var i = -1;
            var len = line.Length;
            while (++i <= len)
            {
                var value = EatValue(line, ref i);
                to.Add(parseFn != null ? parseFn(value.FromCsvField()) : value.FromCsvField());
            }

            return to;
        }

        //Originally from JsvTypeSerializer.EatValue()
        public static string EatValue(string value, ref int i)
        {
            var tokenStartPos = i;
            var valueLength = value.Length;
            if (i == valueLength) return null;

            var valueChar = value[i];
            var withinQuotes = false;
            var endsToEat = 1;
            var itemSeperator = CsvConfig.ItemSeperatorString.Length == 1
                ? CsvConfig.ItemSeperatorString[0]
                : JsWriter.ItemSeperator;

            if (valueChar == itemSeperator || valueChar == JsWriter.MapEndChar)
                return null;

            if (valueChar == JsWriter.QuoteChar) //Is Within Quotes, i.e. "..."
            {
                while (++i < valueLength)
                {
                    valueChar = value[i];

                    if (valueChar != JsWriter.QuoteChar) continue;

                    var isLiteralQuote = i + 1 < valueLength && value[i + 1] == JsWriter.QuoteChar;

                    i++; //skip quote
                    if (!isLiteralQuote)
                        break;
                }
                return value.Substring(tokenStartPos, i - tokenStartPos);
            }
            if (valueChar == JsWriter.MapStartChar) //Is Type/Map, i.e. {...}
            {
                while (++i < valueLength && endsToEat > 0)
                {
                    valueChar = value[i];

                    if (valueChar == JsWriter.QuoteChar)
                        withinQuotes = !withinQuotes;

                    if (withinQuotes)
                        continue;

                    if (valueChar == JsWriter.MapStartChar)
                        endsToEat++;

                    if (valueChar == JsWriter.MapEndChar)
                        endsToEat--;
                }
                if (endsToEat > 0)
                { 
                    //Unmatched start and end char, give up
                    i = tokenStartPos;
                    valueChar = value[i];
                }
                else
                    return value.Substring(tokenStartPos, i - tokenStartPos);
            }
            if (valueChar == JsWriter.ListStartChar) //Is List, i.e. [...]
            {
                while (++i < valueLength && endsToEat > 0)
                {
                    valueChar = value[i];

                    if (valueChar == JsWriter.QuoteChar)
                        withinQuotes = !withinQuotes;

                    if (withinQuotes)
                        continue;

                    if (valueChar == JsWriter.ListStartChar)
                        endsToEat++;

                    if (valueChar == JsWriter.ListEndChar)
                        endsToEat--;
                }
                if (endsToEat > 0)
                {
                    //Unmatched start and end char, give up
                    i = tokenStartPos;
                    valueChar = value[i];
                }
                else
                    return value.Substring(tokenStartPos, i - tokenStartPos);
            }

            //if value starts with MapStartChar, check MapEndChar to terminate
            char specEndChar = itemSeperator;
            if (value[tokenStartPos] == JsWriter.MapStartChar)
                specEndChar = JsWriter.MapEndChar;

            while (++i < valueLength) //Is Value
            {
                valueChar = value[i];

                if (valueChar == itemSeperator || valueChar == specEndChar)
                {
                    break;
                }
            }

            return value.Substring(tokenStartPos, i - tokenStartPos);
        }
    }

    public class CsvReader<T>
    {
        public static List<string> Headers { get; set; }

        internal static List<SetMemberDelegate<T>> PropertySetters;
        internal static Dictionary<string, SetMemberDelegate<T>> PropertySettersMap;

        internal static List<ParseStringDelegate> PropertyConverters;
        internal static Dictionary<string, ParseStringDelegate> PropertyConvertersMap;

        static CsvReader()
        {
            Reset();
        }

        internal static void Reset()
        {
            Headers = new List<string>();

            PropertySetters = new List<SetMemberDelegate<T>>();
            PropertySettersMap = new Dictionary<string, SetMemberDelegate<T>>(PclExport.Instance.InvariantComparerIgnoreCase);

            PropertyConverters = new List<ParseStringDelegate>();
            PropertyConvertersMap = new Dictionary<string, ParseStringDelegate>(PclExport.Instance.InvariantComparerIgnoreCase);

            foreach (var propertyInfo in TypeConfig<T>.Properties)
            {
                if (!propertyInfo.CanWrite || propertyInfo.GetSetMethod(nonPublic:true) == null) continue;
                if (!TypeSerializer.CanCreateFromString(propertyInfo.PropertyType)) continue;

                var propertyName = propertyInfo.Name;
                var setter = propertyInfo.CreateSetter<T>();
                PropertySetters.Add(setter);

                var converter = JsvReader.GetParseFn(propertyInfo.PropertyType);
                PropertyConverters.Add(converter);

                var dcsDataMemberName = propertyInfo.GetDataMemberName();
                if (dcsDataMemberName != null)
                    propertyName = dcsDataMemberName;

                Headers.Add(propertyName);
                PropertySettersMap[propertyName] = setter;
                PropertyConvertersMap[propertyName] = converter;
            }
        }

        internal static void ConfigureCustomHeaders(Dictionary<string, string> customHeadersMap)
        {
            Reset();

            for (var i = Headers.Count - 1; i >= 0; i--)
            {
                var oldHeader = Headers[i];
                if (!customHeadersMap.TryGetValue(oldHeader, out var newHeader))
                {
                    Headers.RemoveAt(i);
                    PropertySetters.RemoveAt(i);
                }
                else
                {
                    Headers[i] = newHeader;

                    PropertySettersMap.TryGetValue(oldHeader, out var setter);
                    PropertySettersMap.Remove(oldHeader);
                    PropertySettersMap[newHeader] = setter;
                    
                    PropertyConvertersMap.TryGetValue(oldHeader, out var converter);
                    PropertyConvertersMap.Remove(oldHeader);
                    PropertyConvertersMap[newHeader] = converter;
                }
            }
        }

        private static List<T> GetSingleRow(IEnumerable<string> rows, Type recordType)
        {
            var row = new List<T>();
            foreach (var value in rows)
            {
                var to = recordType == typeof(string)
                   ? (T)(object)value
                   : TypeSerializer.DeserializeFromString<T>(value);

                row.Add(to);
            }
            return row;
        }

        public static List<T> GetRows(IEnumerable<string> records)
        {
            var rows = new List<T>();

            if (records == null) return rows;

            if (typeof(T).IsValueType || typeof(T) == typeof(string))
            {
                return GetSingleRow(records, typeof(T));
            }

            foreach (var record in records)
            {
                var to = typeof(T).CreateInstance<T>();
                foreach (var propertySetter in PropertySetters)
                {
                    propertySetter(to, record);
                }
                rows.Add(to);
            }

            return rows;
        }

        public static object ReadObject(string csv)
        {
            if (csv == null) return null; //AOT

            return Read(CsvReader.ParseLines(csv));
        }

        public static object ReadObjectRow(string csv)
        {
            if (csv == null) return null; //AOT

            return ReadRow(csv);
        }

        public static List<Dictionary<string, string>> ReadStringDictionary(IEnumerable<string> rows)
        {
            if (rows == null) return null; //AOT

            var to = new List<Dictionary<string, string>>();

            List<string> headers = null;
            foreach (var row in rows)
            {
                if (headers == null)
                {
                    headers = CsvReader.ParseFields(row);
                    continue;
                }

                var values = CsvReader.ParseFields(row);
                var map = new Dictionary<string, string>();
                for (int i = 0; i < headers.Count; i++)
                {
                    var header = headers[i];
                    map[header] = values[i];
                }

                to.Add(map);
            }

            return to;
        }

        public static List<T> Read(List<string> rows)
        {
            var to = new List<T>();
            if (rows == null || rows.Count == 0) return to; //AOT

            if (typeof(T).IsAssignableFrom(typeof(Dictionary<string, string>)))
            {
                return ReadStringDictionary(rows).ConvertAll(x => (T)(object)x);
            }

            if (typeof(T).IsAssignableFrom(typeof(List<string>)))
            {
                return new List<T>(rows.Select(x => (T)(object)CsvReader.ParseFields(x)));
            }

            List<string> headers = null;
            if (!CsvConfig<T>.OmitHeaders || Headers.Count == 0)
            {
                headers = CsvReader.ParseFields(rows[0], s => s.Trim());
            }

            if (typeof(T).IsValueType || typeof(T) == typeof(string))
            {
                return rows.Count == 1
                    ? GetSingleRow(CsvReader.ParseFields(rows[0]), typeof(T))
                    : GetSingleRow(rows, typeof(T));
            }

            for (var rowIndex = headers == null ? 0 : 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var o = typeof(T).CreateInstance<T>();

                var fields = CsvReader.ParseFields(row);
                for (int i = 0; i < fields.Count; i++)
                {
                    var setter = i < PropertySetters.Count ? PropertySetters[i] : null;
                    if (headers != null)
                        PropertySettersMap.TryGetValue(headers[i], out setter);

                    if (setter == null)
                        continue;

                    var converter = i < PropertyConverters.Count ? PropertyConverters[i] : null;
                    if (headers != null)
                        PropertyConvertersMap.TryGetValue(headers[i], out converter);

                    if (converter == null)
                        continue;

                    var field = fields[i];
                    var convertedValue = converter(field);
                    setter(o, convertedValue);
                }

                to.Add(o);
            }

            return to;
        }

        public static T ReadRow(string value)
        {
            if (value == null) return default(T); //AOT

            return Read(CsvReader.ParseLines(value)).FirstOrDefault();
        }

    }
}