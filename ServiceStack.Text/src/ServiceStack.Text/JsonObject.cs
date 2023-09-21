using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;

namespace ServiceStack.Text;

public static class JsonExtensions
{
    public static T JsonTo<T>(this Dictionary<string, string> map, string key)
    {
        return Get<T>(map, key);
    }

    /// <summary>
    /// Get JSON string value converted to T
    /// </summary>
    public static T Get<T>(this Dictionary<string, string> map, string key, T defaultValue = default)
    {
        if (map == null)
            return default;
        return map.TryGetValue(key, out var strVal) ? JsonSerializer.DeserializeFromString<T>(strVal) : defaultValue;
    }

    public static T[] GetArray<T>(this Dictionary<string, string> map, string key)
    {
        if (map == null)
            return TypeConstants<T>.EmptyArray;
        return map.TryGetValue(key, out var value) 
            ? (map is JsonObject obj ? value.FromJson<T[]>() : value.FromJsv<T[]>()) 
            : TypeConstants<T>.EmptyArray;
    }

    /// <summary>
    /// Get JSON string value
    /// </summary>
    public static string Get(this Dictionary<string, string> map, string key)
    {
        if (map == null)
            return null;
        return map.TryGetValue(key, out var strVal) 
            ? JsonTypeSerializer.Instance.UnescapeString(strVal) 
            : null;
    }

    public static JsonArrayObjects ArrayObjects(this string json)
    {
        return Text.JsonArrayObjects.Parse(json);
    }

    public static List<T> ConvertAll<T>(this JsonArrayObjects jsonArrayObjects, Func<JsonObject, T> converter)
    {
        var results = new List<T>();

        foreach (var jsonObject in jsonArrayObjects)
        {
            results.Add(converter(jsonObject));
        }

        return results;
    }

    public static T ConvertTo<T>(this JsonObject jsonObject, Func<JsonObject, T> convertFn)
    {
        return jsonObject == null
            ? default
            : convertFn(jsonObject);
    }

    public static Dictionary<string, string> ToDictionary(this JsonObject jsonObject)
    {
        return jsonObject == null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(jsonObject);
    }
}

public class JsonObject : Dictionary<string, string>, IEnumerable<KeyValuePair<string, string>>
{
    /// <summary>
    /// Get JSON string value
    /// </summary>
    public new string this[string key]
    {
        get => this.Get(key);
        set => base[key] = value;
    }

    public new Enumerator GetEnumerator()
    {
        var to = new Dictionary<string, string>();
        foreach (var key in Keys)
        {
            to[key] = this[key];
        }
        return to.GetEnumerator();
    }

    IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        => GetEnumerator();

    public Dictionary<string, string> ToUnescapedDictionary()
    {
        var to = new Dictionary<string, string>();
        var enumerateAsConcreteDict = (Dictionary<string, string>)this;
        foreach (var entry in enumerateAsConcreteDict)
        {
            to[entry.Key] = entry.Value;
        }
        return to;
    }

    public static JsonObject Parse(string json)
    {
        return JsonSerializer.DeserializeFromString<JsonObject>(json);
    }

    public static JsonArrayObjects ParseArray(string json)
    {
        return JsonArrayObjects.Parse(json);
    }

    public JsonArrayObjects ArrayObjects(string propertyName)
    {
        return this.TryGetValue(propertyName, out var strValue)
            ? JsonArrayObjects.Parse(strValue)
            : null;
    }

    public JsonObject Object(string propertyName)
    {
        return this.TryGetValue(propertyName, out var strValue)
            ? Parse(strValue)
            : null;
    }

    /// <summary>
    /// Get unescaped string value
    /// </summary>
    public string GetUnescaped(string key)
    {
        return base.TryGetValue(key, out var value)
            ? value
            : null;
    }

    /// <summary>
    /// Get unescaped string value
    /// </summary>
    public string Child(string key)
    {
        return base.TryGetValue(key, out var value)
            ? value
            : null;
    }

    /// <summary>
    /// Write JSON Array, Object, bool or number values as raw string
    /// </summary>
    public static void WriteValue(TextWriter writer, object value)
    {
        var strValue = value as string;
        if (!string.IsNullOrEmpty(strValue))
        {
            var firstChar = strValue[0];
            var lastChar = strValue[strValue.Length - 1];
            if ((firstChar == JsWriter.MapStartChar && lastChar == JsWriter.MapEndChar)
                || (firstChar == JsWriter.ListStartChar && lastChar == JsWriter.ListEndChar)
                || JsonUtils.True == strValue
                || JsonUtils.False == strValue
                || IsJavaScriptNumber(strValue))
            {
                writer.Write(strValue);
                return;
            }
        }
        JsonUtils.WriteString(writer, strValue);
    }

    private static bool IsJavaScriptNumber(string strValue)
    {
        var firstChar = strValue[0];
        if (firstChar == '0')
        {
            if (strValue.Length == 1)
                return true;
            if (!strValue.Contains("."))
                return false;
        }

        if (!strValue.Contains("."))
        {
            if (long.TryParse(strValue, out var longValue))
            {
                return longValue < JsonUtils.MaxInteger && longValue > JsonUtils.MinInteger;
            }
            return false;
        }

        if (double.TryParse(strValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return doubleValue < JsonUtils.MaxInteger && doubleValue > JsonUtils.MinInteger;
        }
        return false;
    }

    public T ConvertTo<T>()
    {
        return (T)this.ConvertTo(typeof(T));
    }

    public object ConvertTo(Type type)
    {
        var map = new Dictionary<string, object>();

        foreach (var entry in this)
        {
            map[entry.Key] = entry.Value;
        }

        return map.FromObjectDictionary(type);
    }
}

public class JsonArrayObjects : List<JsonObject>
{
    public static JsonArrayObjects Parse(string json)
    {
        return JsonSerializer.DeserializeFromString<JsonArrayObjects>(json);
    }
}

public interface IValueWriter
{
    void WriteTo(ITypeSerializer serializer, TextWriter writer);
}

public struct JsonValue : IValueWriter
{
    private readonly string json;

    public JsonValue(string json)
    {
        this.json = json;
    }

    public T As<T>() => JsonSerializer.DeserializeFromString<T>(json);

    public override string ToString() => json;

    public void WriteTo(ITypeSerializer serializer, TextWriter writer) => writer.Write(json ?? JsonUtils.Null);
}