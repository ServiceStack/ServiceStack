using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Razor.Json
{
    /// <summary>
    /// Also in .NET 4.0 of ServiceStack.Text
    /// </summary>
    public class DynamicJson : DynamicObject
    {
        private readonly IDictionary<string, object> _hash = new Dictionary<string, object>();

        public static string Serialize(dynamic instance)
        {
            var json = JsonSerializer.SerializeToString(instance);
            return json;
        }

        public static dynamic Deserialize(string json)
        {
            // Support arbitrary nesting by using JsonObject
            var deserialized = JsonSerializer.DeserializeFromString<JsonObject>(json);
            var hash = deserialized.ToDictionary<KeyValuePair<string, string>, string, object>(entry => entry.Key, entry => entry.Value);
            return new DynamicJson(hash);
        }

        public DynamicJson(IEnumerable<KeyValuePair<string, object>> hash)
        {
            _hash.Clear();
            foreach (var entry in hash)
            {
                _hash.Add(Underscored(entry.Key), entry.Value);
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var name = Underscored(binder.Name);
            _hash[name] = value;
            return _hash[name] == value;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = Underscored(binder.Name);
            return YieldMember(name, out result);
        }

        public override string ToString()
        {
            return JsonSerializer.SerializeToString(_hash);
        }

        private bool YieldMember(string name, out object result)
        {
            if (_hash.ContainsKey(name))
            {
                var json = _hash[name].ToString();
                if (json.TrimStart(' ').StartsWith("{"))
                {
                    result = Deserialize(json);
                    return true;
                }
                result = json;
                return _hash[name] == result;
            }
            result = null;
            return false;
        }

        internal static string Underscored(IEnumerable<char> pascalCase)
        {
            var sb = new StringBuilder();
            var i = 0;
            foreach (var c in pascalCase)
            {
                if (char.IsUpper(c) && i > 0)
                {
                    sb.Append("_");
                }
                sb.Append(c);
                i++;
            }
            return sb.ToString().ToLowerInvariant();
        }
    }
}