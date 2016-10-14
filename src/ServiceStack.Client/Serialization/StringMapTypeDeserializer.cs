using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Jsv;
using System.Linq;

namespace ServiceStack.Serialization
{
    /// <summary>
    /// Serializer cache of delegates required to create a type from a string map (e.g. for REST urls)
    /// </summary>
    public class StringMapTypeDeserializer
    {
        private static ILog Log = LogManager.GetLogger(typeof(StringMapTypeDeserializer));

        internal class PropertySerializerEntry
        {
            public PropertySerializerEntry(SetPropertyDelegate propertySetFn, ParseStringDelegate propertyParseStringFn)
            {
                PropertySetFn = propertySetFn;
                PropertyParseStringFn = propertyParseStringFn;
            }

            public SetPropertyDelegate PropertySetFn;
            public ParseStringDelegate PropertyParseStringFn;
            public Type PropertyType;
        }

        private readonly Type type;
        private readonly Dictionary<string, PropertySerializerEntry> propertySetterMap
            = new Dictionary<string, PropertySerializerEntry>(PclExport.Instance.InvariantComparerIgnoreCase);

        internal StringMapTypeDeserializer(Type type, ILog log) : this(type)
        {
            Log = log;
        }

        public ParseStringDelegate GetParseFn(Type propertyType)
        {
            //Don't JSV-decode string values for string properties
            if (propertyType == typeof(string))
                return s => s;

            return JsvReader.GetParseFn(propertyType);
        }

        public StringMapTypeDeserializer(Type type)
        {
            this.type = type;

            foreach (var propertyInfo in type.GetSerializableProperties())
            {
                var propertySetFn = JsvDeserializeType.GetSetPropertyMethod(type, propertyInfo);
                var propertyType = propertyInfo.PropertyType;
                var propertyParseStringFn = GetParseFn(propertyType);
                var propertySerializer = new PropertySerializerEntry(propertySetFn, propertyParseStringFn) { PropertyType = propertyType };

                var attr = propertyInfo.FirstAttribute<DataMemberAttribute>();
                if (attr != null && attr.Name != null)
                {
                    propertySetterMap[attr.Name] = propertySerializer;
                }
                propertySetterMap[propertyInfo.Name] = propertySerializer;
            }

            if (JsConfig.IncludePublicFields)
            {
                foreach (var fieldInfo in type.GetSerializableFields())
                {
                    var fieldSetFn = JsvDeserializeType.GetSetFieldMethod(type, fieldInfo);
                    var fieldType = fieldInfo.FieldType;
                    var fieldParseStringFn = JsvReader.GetParseFn(fieldType);
                    var fieldSerializer = new PropertySerializerEntry(fieldSetFn, fieldParseStringFn) { PropertyType = fieldType };

                    propertySetterMap[fieldInfo.Name] = fieldSerializer;
                }
            }
        }

        public object PopulateFromMap(object instance, IDictionary<string, string> keyValuePairs, List<string> ignoredWarningsOnPropertyNames = null)
        {
            var errors = new List<RequestBindingError>();

            string propertyName = null;
            string propertyTextValue = null;
            PropertySerializerEntry propertySerializerEntry = null;

            if (instance == null)
                instance = type.CreateInstance();

            foreach (var pair in keyValuePairs.Where(x => !string.IsNullOrEmpty(x.Value)))
            {
                try
                {
                    propertyName = pair.Key;
                    propertyTextValue = pair.Value;

                    if (!propertySetterMap.TryGetValue(propertyName, out propertySerializerEntry))
                    {
                        if (propertyName == "v")
                        {
                            int version;
                            var hasVersion = instance as IHasVersion;
                            if (hasVersion != null && int.TryParse(pair.Value, out version))
                            {
                                hasVersion.Version = version;
                            }
                            continue;
                        }

                        var ignoredProperty = propertyName.ToLowerInvariant();
                        if (ignoredWarningsOnPropertyNames == null ||
                            !ignoredWarningsOnPropertyNames.Contains(ignoredProperty))
                        {
                            Log.WarnFormat("Property '{0}' does not exist on type '{1}'", ignoredProperty, type.FullName);
                        }
                        continue;
                    }

                    if (propertySerializerEntry.PropertySetFn == null)
                    {
                        Log.WarnFormat("Could not set value of read-only property '{0}' on type '{1}'", propertyName,
                                       type.FullName);
                        continue;
                    }

                    if (propertySerializerEntry.PropertyType == typeof (bool))
                    {
                        //InputExtensions.cs#530 MVC Checkbox helper emits extra hidden input field, generating 2 values, first is the real value
                        propertyTextValue = propertyTextValue.LeftPart(',');
                    }

                    var value = propertySerializerEntry.PropertyParseStringFn(propertyTextValue);
                    if (value == null)
                    {
                        Log.WarnFormat("Could not create instance on '{0}' for property '{1}' with text value '{2}'",
                                       instance, propertyName, propertyTextValue);
                        continue;
                    }
                    propertySerializerEntry.PropertySetFn(instance, value);
                }
                catch (Exception ex)
                {
                    var error = new RequestBindingError();

                    if (propertyName != null)
                        error.PropertyName = propertyName;

                    if (propertyTextValue != null)
                        error.PropertyValueString = propertyTextValue;

                    if (propertySerializerEntry != null && propertySerializerEntry.PropertyType != null)
                        error.PropertyType = propertySerializerEntry.PropertyType;

                    errors.Add(error);
                }
            }

            if (errors.Count > 0)
            {
                var serializationException = new SerializationException($"Unable to bind to request '{type.Name}'");
                serializationException.Data.Add("errors", errors);
                throw serializationException;
            }

            return instance;
        }

        public object CreateFromMap(IDictionary<string, string> keyValuePairs)
        {
            return PopulateFromMap(null, keyValuePairs, null);
        }
    }

    public class RequestBindingError
    {
        public string PropertyName { get; set; }
        public string PropertyValueString { get; set; }
        public Type PropertyType { get; set; }
    }
}
