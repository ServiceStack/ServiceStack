using System.Collections.Generic;

namespace ServiceStack.Common.ServiceModel
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
            bool makeDataContractsExtensible = false,
            bool addIndexesToDataMembers = false,
            string addDefaultXmlNamespace = null,
            bool initializeCollections = true,
            bool addResponseStatus = false,
            int? addImplicitVersion = null)
        {
            BaseUrl = baseUrl;
            MakePartial = makePartial;
            MakeVirtual = makeVirtual;
            AddReturnMarker = addReturnMarker;
            AddDescriptionAsComments = convertDescriptionToComments;
            AddDataContractAttributes = addDataContractAttributes;
            AddDefaultXmlNamespace = addDefaultXmlNamespace;
            MakeDataContractsExtensible = makeDataContractsExtensible;
            AddIndexesToDataMembers = addIndexesToDataMembers;
            InitializeCollections = initializeCollections;
            AddResponseStatus = addResponseStatus;
            AddImplicitVersion = addImplicitVersion;

            DefaultNamespaces = new List<string> 
            {
                "System",
                "System.Collections",
                "System.ComponentModel",
                "System.Collections.Generic",
                "System.Runtime.Serialization",
                "ServiceStack.ServiceHost",
                "ServiceStack.ServiceInterface.ServiceModel",
            };
        }

        public string BaseUrl { get; set; }
        public bool MakePartial { get; set; }
        public bool MakeVirtual { get; set; }
        public bool AddReturnMarker { get; set; }
        public bool AddDescriptionAsComments { get; set; }
        public bool AddDataContractAttributes { get; set; }
        public bool MakeDataContractsExtensible { get; set; }
        public bool AddIndexesToDataMembers { get; set; }
        public bool InitializeCollections { get; set; }
        public int? AddImplicitVersion { get; set; }
        public bool AddResponseStatus { get; set; }
        public string AddDefaultXmlNamespace { get; set; }
        public List<string> DefaultNamespaces { get; set; }
    }

    public class MetadataTypes
    {
        public MetadataTypes()
        {
            Types = new List<MetadataType>();
            Operations = new List<MetadataOperationType>();
            Version = 1;
        }

        public int Version { get; set; }
        public MetadataTypesConfig Config { get; set; }
        public List<MetadataType> Types { get; set; }
        public List<MetadataOperationType> Operations { get; set; }
    }

    public class MetadataOperationType
    {
        public List<string> Actions { get; set; }
        public MetadataType Request { get; set; }
        public MetadataType Response { get; set; }
    }

    public class MetadataType
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string[] GenericArgs { get; set; }
        public string Inherits { get; set; }
        public string[] InheritsGenericArgs { get; set; }
        public string Description { get; set; }
        public bool ReturnVoidMarker { get; set; }
        public string[] ReturnMarkerGenericArgs { get; set; }

        public List<MetadataRoute> Routes { get; set; }
        public MetadataDataContract DataContract { get; set; }

        public List<MetadataPropertyType> Properties { get; set; }

        public List<MetadataAttribute> Attributes { get; set; }
    }

    public class MetadataRoute
    {
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
        public string Name { get; set; }
        public string Type { get; set; }
        public string[] GenericArgs { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public MetadataDataMember DataMember { get; set; }

        public List<MetadataAttribute> Attributes { get; set; }
    }

    public class MetadataAttribute
    {
        public string Name { get; set; }
        public List<MetadataPropertyType> ConstructorArgs { get; set; }
        public List<MetadataPropertyType> Args { get; set; }
    }
}