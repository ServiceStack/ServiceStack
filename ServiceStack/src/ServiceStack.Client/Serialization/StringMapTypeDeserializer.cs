using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Serialization;

/// <summary>
/// Serializer cache of delegates required to create a type from a string map (e.g. for REST urls)
/// </summary>
public class StringMapTypeDeserializer
{
    private static ILog Log = LogManager.GetLogger(typeof(StringMapTypeDeserializer));

    public static Dictionary<string,IStringSerializer> ContentTypeStringSerializers { get; } = new ()
    {
        [MimeTypes.Json] = new JsonStringSerializer(),
        [MimeTypes.Jsv] = new JsvStringSerializer(),
        [MimeTypes.Csv] = new CsvStringSerializer(),
    };

    public static ConcurrentDictionary<Type,IStringSerializer> TypeStringSerializers { get; } = new ();

    public class PropertySerializerEntry(GetMemberDelegate propertyGetFn, SetMemberDelegate propertySetFn, ParseStringDelegate propertyParseStringFn)
    {
        public readonly GetMemberDelegate PropertyGetFn = propertyGetFn;
        public readonly SetMemberDelegate PropertySetFn = propertySetFn;
        public readonly ParseStringDelegate PropertyParseStringFn = propertyParseStringFn;
        public Type PropertyType;
    }

    private readonly Type type;
    private readonly Dictionary<string, PropertySerializerEntry> propertySetterMap = new(StringComparer.OrdinalIgnoreCase);
    
    public PropertySerializerEntry GetPropertySerializer(string propertyName)
    {
        propertySetterMap.TryGetValue(propertyName, out var propertySerializer);
        return propertySerializer;
    }

    internal StringMapTypeDeserializer(Type type, ILog log) : this(type)
    {
        Log = log;
    }

    private static ParseStringDelegate ResolveStringParseFn(Type propType, MultiPartFieldAttribute attr)
    {
        if (attr != null)
        {
            if (attr.ContentType != null)
            {
                if (!ContentTypeStringSerializers.TryGetValue(attr.ContentType, out var serializer))
                    throw new NotSupportedException($"ContentType '{attr.ContentType}' not found in {nameof(StringMapTypeDeserializer)}.{nameof(ContentTypeStringSerializers)}");
                return s => serializer.DeserializeFromString(s, propType);
            }
            if (attr.StringSerializer != null)
            {
                var serializer = TypeStringSerializers.GetOrAdd(attr.StringSerializer, 
                    type => type.CreateInstance<IStringSerializer>());
                return s => serializer.DeserializeFromString(s, propType);
            }
        }

        //Don't JSV-decode string values for string properties
        if (propType == typeof(string))
            return s => s;
        return JsvReader.GetParseFn(propType);
    }

    public StringMapTypeDeserializer(Type type)
    {
        this.type = type;
            
        foreach (var propertyInfo in type.GetSerializableProperties())
        {
            var propertyGetFn = propertyInfo.CreateGetter();
            var propertySetFn = propertyInfo.CreateSetter();
            var propertyType = propertyInfo.PropertyType;
            var propertySerializer = new PropertySerializerEntry(propertyGetFn, propertySetFn, 
                ResolveStringParseFn(propertyType, propertyInfo.FirstAttribute<MultiPartFieldAttribute>()))
            {
                PropertyType = propertyType
            };

            var attr = propertyInfo.FirstAttribute<DataMemberAttribute>();
            if (attr?.Name != null)
            {
                propertySetterMap[attr.Name] = propertySerializer;
            }
            propertySetterMap[propertyInfo.Name] = propertySerializer;
        }

        if (JsConfig.IncludePublicFields)
        {
            foreach (var fieldInfo in type.GetSerializableFields())
            {
                var filedGetFn = fieldInfo.CreateGetter();
                var fieldSetFn = fieldInfo.CreateSetter();
                var fieldType = fieldInfo.FieldType;
                var fieldSerializer = new PropertySerializerEntry(filedGetFn, fieldSetFn,
                    ResolveStringParseFn(fieldType, fieldInfo.FirstAttribute<MultiPartFieldAttribute>()))
                {
                    PropertyType = fieldType
                };

                propertySetterMap[fieldInfo.Name] = fieldSerializer;
            }
        }
    }

    public object PopulateFromMap(object instance, IDictionary<string, string> keyValuePairs, HashSet<string> ignoredWarningsOnPropertyNames = null)
    {
        var errors = new List<RequestBindingError>();

        instance ??= type.CreateInstance();

        foreach (var pair in keyValuePairs)
        {
            if (!string.IsNullOrEmpty(pair.Value))
            {
                instance = PopulateFromKeyValue(instance, pair.Key, pair.Value,
                    out PropertySerializerEntry _, errors, ignoredWarningsOnPropertyNames);
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

    public object PopulateFromMap(object instance, NameValueCollection nameValues, HashSet<string> ignoredWarningsOnPropertyNames = null)
    {
        var errors = new List<RequestBindingError>();

        instance ??= type.CreateInstance();

        foreach (var key in nameValues.AllKeys)
        {
            if (key == null) continue; //.NET Framework NameValueCollection can contain null keys
            var value = nameValues[key];
            if (!string.IsNullOrEmpty(value))
            {
                instance = PopulateFromKeyValue(instance, key, value,
                    out PropertySerializerEntry _, errors, ignoredWarningsOnPropertyNames);
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


    private object PopulateFromKeyValue(object instance, string propertyName, string propertyTextValue, out PropertySerializerEntry propertySerializerEntry,  List<RequestBindingError> errors, 
        HashSet<string> ignoredWarningsOnPropertyNames = null)
    {
        propertySerializerEntry = null;

        try
        {
            if (!propertySetterMap.TryGetValue(propertyName, out propertySerializerEntry))
            {
                if (propertyName == "v")
                {
                    if (instance is IHasVersion hasVersion && int.TryParse(propertyTextValue, out var version))
                    {
                        hasVersion.Version = version;
                    }
                    return instance;
                }

                if (ignoredWarningsOnPropertyNames != null && !ignoredWarningsOnPropertyNames.Contains(propertyName)
                                                           && !type.HasAttributeCached<FallbackRouteAttribute>())
                {
                    Log.WarnFormat("Property '{0}' does not exist on type '{1}'", propertyName, type.FullName);
                }
                return instance;
            }

            if (propertySerializerEntry.PropertySetFn == null)
            {
                Log.WarnFormat("Could not set value of read-only property '{0}' on type '{1}'", propertyName,
                    type.FullName);
                return instance;
            }

            if (propertySerializerEntry.PropertyType == typeof(bool))
            {
                //InputExtensions.cs#530 MVC Checkbox helper emits extra hidden input field, generating 2 values, first is the real value
                propertyTextValue = propertyTextValue.LeftPart(',');
            }

            var value = propertySerializerEntry.PropertyParseStringFn(propertyTextValue);
            if (value == null)
            {
                Log.WarnFormat("Could not create instance on '{0}' for property '{1}' with text value '{2}'",
                    instance, propertyName, propertyTextValue);
                return instance;
            }
            propertySerializerEntry.PropertySetFn(instance, value);
        }
        catch (Exception ex)
        {
            var error = new RequestBindingError { ErrorMessage = ex.Message };

            if (propertyName != null)
                error.PropertyName = propertyName;

            if (propertyTextValue != null)
                error.PropertyValueString = propertyTextValue;

            if (propertySerializerEntry is { PropertyType: not null })
                error.PropertyType = propertySerializerEntry.PropertyType;

            errors.Add(error);
        }

        return instance;
    }
        

    public object CreateFromMap(IDictionary<string, string> keyValuePairs)
    {
        return PopulateFromMap(null, keyValuePairs, null);
    }

    public object CreateFromMap(NameValueCollection nameValues)
    {
        return PopulateFromMap(null, nameValues, null);
    }

}

public class RequestBindingError
{
    public string PropertyName { get; set; }
    public string PropertyValueString { get; set; }
    public Type PropertyType { get; set; }
    public string ErrorMessage { get; set; }
}