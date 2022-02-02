using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Api.OpenApi.Specification
{
    //from https://github.com/OAI/OpenAPI-Specification/blob/master/versions/2.0.md#user-content-parameterMaximum
    [DataContract]
    public abstract class OpenApiDataTypeSchema
    {
        [DataMember(Name = "$ref")]
        public string Ref { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "type")]
        public string Type { get; set; }
        [DataMember(Name = "format")]
        public string Format { get; set; }
        [DataMember(Name = "items")]
        public Dictionary<string, object> Items { get; set; }
        [DataMember(Name = "collectionFormat")]
        public string CollectionFormat { get; set; }
        [DataMember(Name = "default")]
        public string Default { get; set; }
        [DataMember(Name = "maximum")]
        public double? Maximum { get; set; }
        [DataMember(Name = "exclusiveMaximum")]
        public bool? ExclusiveMaximum { get; set; }
        [DataMember(Name = "minimum")]
        public double? Minimum { get; set; }
        [DataMember(Name = "exclusiveMinimum")]
        public bool? ExclusiveMinimum { get; set; }
        [DataMember(Name = "maxLength")]
        public int? MaxLength { get; set; }
        [DataMember(Name = "minLength")]
        public int? MinLength { get; set; }
        [DataMember(Name = "pattern")]
        public string Pattern { get; set; }
        [DataMember(Name = "maxItems")]
        public int? MaxItems { get; set; }
        [DataMember(Name = "minItems")]
        public int? MinItems { get; set; }
        [DataMember(Name = "uniqueItems")]
        public bool? UniqueItems { get; set; }
        [DataMember(Name = "maxProperties")]
        public string MaxProperties { get; set; }
        [DataMember(Name = "minProperties")]
        public string MinProperties { get; set; }
        [DataMember(Name = "required")]
        public bool? Required { get; set; }
        [DataMember(Name = "enum")]
        public string[] Enum { get; set; }
        [DataMember(Name = "multipleOf")]
        public double? MultipleOf { get; set; }
        [DataMember(Name = "x-nullable")]
        public bool? Nullable { get; set; }
    }
}
