using System;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack
{
    /// <summary>
    /// Creates a Unified Resource Name (URN) with the following formats:
    /// 
    ///		- urn:{TypeName}:{IdFieldValue}						e.g. urn:UserSession:1
    ///		- urn:{TypeName}:{IdFieldName}:{IdFieldValue}		e.g. urn:UserSession:UserId:1			
    /// 
    /// </summary>
    public class UrnId
    {
        private const char FieldSeperator = ':';
        private const char FieldPartsSeperator = '/';
        public string TypeName { get; private set; }
        public string IdFieldValue { get; private set; }
        public string IdFieldName { get; private set; }

        const int HasNoIdFieldName = 3;
        const int HasIdFieldName = 4;

        private UrnId() { }

        public static UrnId Parse(string urnId)
        {
            var urnParts = urnId.Split(FieldSeperator);
            if (urnParts.Length == HasNoIdFieldName)
                return new UrnId { TypeName = urnParts[1], IdFieldValue = urnParts[2] };

            if (urnParts.Length == HasIdFieldName)
                return new UrnId { TypeName = urnParts[1], IdFieldName = urnParts[2], IdFieldValue = urnParts[3] };

            throw new ArgumentException("Cannot parse invalid urn: '{0}'", urnId);
        }

        public static string Create(string objectTypeName, string idFieldValue)
        {
            if (objectTypeName.Contains(FieldSeperator.ToString()))
                throw new ArgumentException("objectTypeName cannot have the illegal characters: ':'", "objectTypeName");

            if (idFieldValue.Contains(FieldSeperator.ToString()))
                throw new ArgumentException("idFieldValue cannot have the illegal characters: ':'", "idFieldValue");

            return $"urn:{objectTypeName}:{idFieldValue}";
        }

        public static string CreateWithParts(string objectTypeName, params string[] keyParts)
        {
            if (objectTypeName.Contains(FieldSeperator.ToString()))
                throw new ArgumentException("objectTypeName cannot have the illegal characters: ':'", "objectTypeName");

            var sb = StringBuilderCache.Allocate();
            foreach (var keyPart in keyParts)
            {
                if (sb.Length > 0)
                    sb.Append(FieldPartsSeperator);
                sb.Append(keyPart);
            }

            return $"urn:{objectTypeName}:{StringBuilderCache.ReturnAndFree(sb)}";
        }

        public static string CreateWithParts<T>(params string[] keyParts)
        {
            return CreateWithParts(typeof(T).Name, keyParts);
        }

        public static string Create<T>(string idFieldValue)
        {
            return Create(typeof(T), idFieldValue);
        }

        public static string Create<T>(object idFieldValue)
        {
            return Create(typeof(T), idFieldValue.ToString());
        }

        public static string Create(Type objectType, string idFieldValue)
        {
            if (idFieldValue.Contains(FieldSeperator.ToString()))
                throw new ArgumentException("idFieldValue cannot have the illegal characters: ':'", "idFieldValue");

            return $"urn:{objectType.Name}:{idFieldValue}";
        }

        public static string Create<T>(string idFieldName, string idFieldValue)
        {
            return Create(typeof(T), idFieldName, idFieldValue);
        }

        public static string Create(Type objectType, string idFieldName, string idFieldValue)
        {
            if (idFieldValue.Contains(FieldSeperator.ToString()))
                throw new ArgumentException("idFieldValue cannot have the illegal characters: ':'", "idFieldValue");

            if (idFieldName.Contains(FieldSeperator.ToString()))
                throw new ArgumentException("idFieldName cannot have the illegal characters: ':'", "idFieldName");

            return $"urn:{objectType.Name}:{idFieldName}:{idFieldValue}";
        }

        public static string GetStringId(string urn)
        {
            return Parse(urn).IdFieldValue;
        }

        public static Guid GetGuidId(string urn)
        {
            return new Guid(Parse(urn).IdFieldValue);
        }

        public static long GetLongId(string urn)
        {
            return long.Parse(Parse(urn).IdFieldValue);
        }
    }
}