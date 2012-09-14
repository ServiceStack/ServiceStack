using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Razor
{
    public class DynamicJson : DynamicObject
    {
        private readonly IDictionary<string, object> hash = new Dictionary<string, object>();

        public static string Serialize(dynamic instance)
        {
            var json = JsonSerializer.SerializeToString(instance);
            return json;
        }

        public static dynamic Deserialize(string json)
        {
            var hash = JsonSerializer.DeserializeFromString<IDictionary<string, object>>(json);
            return new DynamicJson(hash);
        }

        public DynamicJson(IEnumerable<KeyValuePair<string, object>> hash)
        {
            this.hash.Clear();
            foreach (var entry in hash)
            {
                this.hash.Add(Underscored(entry.Key), entry.Value);
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var name = Underscored(binder.Name);
            hash[name] = value;
            return hash[name] == value;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = Underscored(binder.Name);
            return YieldMember(name, out result);
        }

        public override string ToString()
        {
            return JsonSerializer.SerializeToString(hash);
        }

        private bool YieldMember(string name, out object result)
        {
            if (hash.ContainsKey(name))
            {
                var json = hash[name].ToString();
                if (json.TrimStart(' ').StartsWith("{"))
                {
                    var nested = JsonSerializer.DeserializeFromString<IDictionary<string, object>>(json);
                    result = new DynamicJson(nested);
                    return true;
                }
                result = json;
                return hash[name] == result;
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