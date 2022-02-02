using System;
using System.Collections.Generic;

namespace ServiceStack.OrmLite
{
    public interface IDynamicRow
    {
        Type Type { get; }
    }

    public interface IDynamicRow<T> : IDynamicRow
    {
        T Fields { get; }
    }

    public struct DictionaryRow : IDynamicRow<Dictionary<string,object>>
    {
        public Type Type { get; }
        public Dictionary<string, object> Fields { get; }

        public DictionaryRow(Type type, Dictionary<string, object> fields)
        {
            Type = type;
            Fields = fields;
        }
    }

    public struct ObjectRow : IDynamicRow<object>
    {
        public Type Type { get; }
        public object Fields { get; }

        public ObjectRow(Type type, object fields)
        {
            Type = type;
            Fields = fields;
        }
    }

    public static class DynamicRowUtils
    {
        internal static object ToFilterType<T>(this object row) => ToFilterType(row, typeof(T));

        internal static object ToFilterType(this object row, Type type) => row == null
            ? null
            : type.IsInstanceOfType(row)
                ? row
                : row switch {
                    Dictionary<string, object> obj => new DictionaryRow(type, obj),
                    _ => new ObjectRow(type, row),
                };
    }
}