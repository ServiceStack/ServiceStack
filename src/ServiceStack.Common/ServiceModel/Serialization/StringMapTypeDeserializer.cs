using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Jsv;
using System.Linq;

namespace ServiceStack.ServiceModel.Serialization
{
    /// <summary>
    /// Serializer cache of delegates required to create a type from a string map (e.g. for REST urls)
    /// </summary>
    public class StringMapTypeDeserializer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StringMapTypeDeserializer));

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
            = new Dictionary<string, PropertySerializerEntry>(Text.StringExtensions.InvariantComparerIgnoreCase());

        public StringMapTypeDeserializer(Type type)
        {
            this.type = type;

            foreach (var propertyInfo in type.GetSerializableProperties())
            {
                var propertySetFn = JsvDeserializeType.GetSetPropertyMethod(type, propertyInfo);
                var propertyType = propertyInfo.PropertyType;
                var propertyParseStringFn = JsvReader.GetParseFn(propertyType);
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
			        var fieldSerializer = new PropertySerializerEntry(fieldSetFn, fieldParseStringFn) {PropertyType = fieldType};

			        propertySetterMap[fieldInfo.Name] = fieldSerializer;
		        }
	        }

        }

        public object PopulateFromMap(object instance, IDictionary<string, string> keyValuePairs)
        {
            string propertyName = null;
            string propertyTextValue = null;
            PropertySerializerEntry propertySerializerEntry = null;

            try
            {
                if (instance == null) instance = type.CreateInstance();

                foreach (var pair in keyValuePairs.Where(x => !string.IsNullOrEmpty(x.Value)))
                {
                    propertyName = pair.Key;
                    propertyTextValue = pair.Value;

                    if (!propertySetterMap.TryGetValue(propertyName, out propertySerializerEntry))
                    {
                        if (propertyName != "format" && propertyName != "callback" && propertyName != "debug")
                        {
                            Log.WarnFormat("Property '{0}' does not exist on type '{1}'", propertyName, type.FullName);
                        }
                        continue;
                    }

                    if (propertySerializerEntry.PropertySetFn == null)
                    {
                        Log.WarnFormat("Could not set value of read-only property '{0}' on type '{1}'", propertyName, type.FullName);
                        continue;
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
                return instance;

            }
            catch (Exception ex)
            {
                var serializationException = new SerializationException("KeyValueDataContractDeserializer: Error converting to type: " + ex.Message, ex);
                if (propertyName != null) {
                    serializationException.Data.Add("propertyName", propertyName);
                }
                if (propertyTextValue != null) {
                    serializationException.Data.Add("propertyValueString", propertyTextValue);
                }
                if (propertySerializerEntry != null && propertySerializerEntry.PropertyType != null) {
                    serializationException.Data.Add("propertyType", propertySerializerEntry.PropertyType);
                }
                throw serializationException;
            }
        }

        public object CreateFromMap(IDictionary<string, string> keyValuePairs)
        {
            return PopulateFromMap(null, keyValuePairs);
        }
    }
}