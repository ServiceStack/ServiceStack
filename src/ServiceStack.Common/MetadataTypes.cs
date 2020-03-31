using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;

namespace ServiceStack
{
    public class MetadataTypesConfig
    {
        public MetadataTypesConfig(
            string baseUrl = null,
            bool makePartial = true,
            bool makeVirtual = true,
            bool addReturnMarker = true,
            bool convertDescriptionToComments = true,
            bool addDataContractAttributes = false,
            bool addIndexesToDataMembers = false,
            bool addGeneratedCodeAttributes = false,
            string addDefaultXmlNamespace = null,
            string baseClass = null,
            string package = null,
            bool addResponseStatus = false,
            bool addServiceStackTypes = true,
            bool addModelExtensions = true,
            bool addPropertyAccessors = true,
            bool excludeGenericBaseTypes = false,
            bool settersReturnThis = true,
            bool makePropertiesOptional = true,
            bool makeDataContractsExtensible = false,
            bool initializeCollections = true,
            int? addImplicitVersion = null)
        {
            BaseUrl = baseUrl;
            MakePartial = makePartial;
            MakeVirtual = makeVirtual;
            AddReturnMarker = addReturnMarker;
            AddDescriptionAsComments = convertDescriptionToComments;
            AddDataContractAttributes = addDataContractAttributes;
            AddDefaultXmlNamespace = addDefaultXmlNamespace;
            BaseClass = baseClass;
            Package = package;
            MakeDataContractsExtensible = makeDataContractsExtensible;
            AddIndexesToDataMembers = addIndexesToDataMembers;
            AddGeneratedCodeAttributes = addGeneratedCodeAttributes;
            InitializeCollections = initializeCollections;
            AddResponseStatus = addResponseStatus;
            AddServiceStackTypes = addServiceStackTypes;
            AddModelExtensions = addModelExtensions;
            AddPropertyAccessors = addPropertyAccessors;
            ExcludeGenericBaseTypes = excludeGenericBaseTypes;
            SettersReturnThis = settersReturnThis;
            MakePropertiesOptional = makePropertiesOptional;
            AddImplicitVersion = addImplicitVersion;
        }

        public string BaseUrl { get; set; }
        public string UsePath { get; set; }
        public bool MakePartial { get; set; }
        public bool MakeVirtual { get; set; }
        public bool MakeInternal { get; set; }
        public string BaseClass { get; set; }
        public string Package { get; set; }
        public bool AddReturnMarker { get; set; }
        public bool AddDescriptionAsComments { get; set; }
        public bool AddDataContractAttributes { get; set; }
        public bool AddIndexesToDataMembers { get; set; }
        public bool AddGeneratedCodeAttributes { get; set; }
        public int? AddImplicitVersion { get; set; }
        public bool AddResponseStatus { get; set; }
        public bool AddServiceStackTypes { get; set; }
        public bool AddModelExtensions { get; set; }
        public bool AddPropertyAccessors { get; set; }
        public bool ExcludeGenericBaseTypes { get; set; }
        public bool SettersReturnThis { get; set; }
        public bool MakePropertiesOptional { get; set; }
        public bool ExportAsTypes { get; set; }
        public bool ExcludeImplementedInterfaces { get; set; }
        public string AddDefaultXmlNamespace { get; set; }
        public bool MakeDataContractsExtensible { get; set; }
        public bool InitializeCollections { get; set; }
        public List<string> AddNamespaces { get; set; }
        public List<string> DefaultNamespaces { get; set; }
        public List<string> DefaultImports { get; set; }
        public List<string> IncludeTypes { get; set; }
        public List<string> ExcludeTypes { get; set; }
        public List<string> TreatTypesAsStrings { get; set; }
        public bool ExportValueTypes { get; set; }

        public string GlobalNamespace { get; set; }
        public bool ExcludeNamespace { get; set; }

        public HashSet<Type> IgnoreTypes { get; set; }
        public HashSet<Type> ExportTypes { get; set; }
        public HashSet<Type> ExportAttributes { get; set; }
        public List<string> IgnoreTypesInNamespaces { get; set; }
    }

    [Exclude(Feature.Soap)]
    public class MetadataTypes
    {
        public MetadataTypes()
        {
            Types = new List<MetadataType>();
            Operations = new List<MetadataOperationType>();
            Namespaces = new List<string>();
        }

        public MetadataTypesConfig Config { get; set; }
        public List<string> Namespaces { get; set; }
        public List<MetadataType> Types { get; set; }
        public List<MetadataOperationType> Operations { get; set; }
    }

    public class MetadataOperationType
    {
        public List<string> Actions { get; set; }
        public MetadataType Request { get; set; }
        public MetadataType Response { get; set; }
    }

    public class MetadataType : IMeta
    {
        [IgnoreDataMember]
        public Type Type { get; set; }
        [IgnoreDataMember]
        public Dictionary<string, object> Items { get; set; }
        
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string[] GenericArgs { get; set; }
        public MetadataTypeName Inherits { get; set; }
        public MetadataTypeName[] Implements { get; set; }
        public string DisplayType { get; set; }
        public string Description { get; set; }
        public bool ReturnVoidMarker { get; set; }
        public bool? IsNested { get; set; }
        public bool? IsEnum { get; set; }
        public bool? IsEnumInt { get; set; }
        public bool? IsInterface { get; set; }
        public bool? IsAbstract { get; set; }

        public MetadataTypeName ReturnMarkerTypeName { get; set; }

        public List<MetadataRoute> Routes { get; set; }

        public MetadataDataContract DataContract { get; set; }

        public List<MetadataPropertyType> Properties { get; set; }

        public List<MetadataAttribute> Attributes { get; set; }

        public List<MetadataTypeName> InnerTypes { get; set; }

        public List<string> EnumNames { get; set; }
        public List<string> EnumValues { get; set; }
        public List<string> EnumMemberValues { get; set; }
        public List<string> EnumDescriptions { get; set; }

        public Dictionary<string, string> Meta { get; set; }

        public string GetFullName() => Namespace + "." + Name;
    }

    public class MetadataTypeName
    {
        [IgnoreDataMember]
        public Type Type { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string[] GenericArgs { get; set; }
    }

    public class MetadataRoute
    {
        [IgnoreDataMember]
        public RouteAttribute RouteAttribute { get; set; }
        public string Path { get; set; }
        public string Verbs { get; set; }
        public string Notes { get; set; }
        public string Summary { get; set; }
    }

    public class MetadataDataContract
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
    }

    public class MetadataDataMember
    {
        public string Name { get; set; }
        public int? Order { get; set; }
        public bool? IsRequired { get; set; }
        public bool? EmitDefaultValue { get; set; }
    }

    public class MetadataPropertyType
    {
        [IgnoreDataMember]
        public Type PropertyType { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool? IsValueType { get; set; }
        public bool? IsSystemType { get; set; }
        public bool? IsEnum { get; set; }
        public string TypeNamespace { get; set; }
        public string[] GenericArgs { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public MetadataDataMember DataMember { get; set; }
        public bool? ReadOnly { get; set; }

        public string ParamType { get; set; }
        public string DisplayType { get; set; }
        public bool? IsRequired { get; set; }
        public string[] AllowableValues { get; set; }
        public int? AllowableMin { get; set; }
        public int? AllowableMax { get; set; }

        public List<MetadataAttribute> Attributes { get; set; }
    }

    public class MetadataAttribute
    {
        [IgnoreDataMember]
        public Attribute Attribute { get; set; }
        public string Name { get; set; }
        public List<MetadataPropertyType> ConstructorArgs { get; set; }
        public List<MetadataPropertyType> Args { get; set; }
    }

    public static class MetadataTypeExtensions
    {
        public static bool InheritsAny(this MetadataType type, params string[] typeNames) =>
            type.Inherits != null && typeNames.Contains(type.Inherits.Name);

        public static bool ImplementsAny(this MetadataType type, params string[] typeNames) =>
            type.Implements != null && type.Implements.Any(i => typeNames.Contains(i.Name));

        public static bool ReferencesAny(this MetadataOperationType op, params string[] typeNames) =>
            (op.Request.Inherits != null && (typeNames.Contains(op.Request.Inherits.Name) ||
                                             op.Request.Inherits.GenericArgs?.Length > 0 &&
                                             op.Request.Inherits.GenericArgs.Any(typeNames.Contains))) 
            ||
            (op.Response != null && (typeNames.Contains(op.Response.Name) ||
                                     op.Response.GenericArgs?.Length > 0 &&
                                     op.Response.GenericArgs.Any(typeNames.Contains))) 
            ||
            (op.Request.Implements != null && op.Request.Implements.Any(i =>
                 i.GenericArgs?.Length > 0 && i.GenericArgs.Any(typeNames.Contains))) 
            ||
            (op.Response?.Inherits != null && (typeNames.Contains(op.Response.Inherits.Name) ||
                                               op.Response.Inherits.GenericArgs?.Length > 0 &&
                                               op.Response.Inherits.GenericArgs.Any(typeNames.Contains)));
    }
}