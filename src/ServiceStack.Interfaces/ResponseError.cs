//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack
{
    /// <summary>
    /// Error information pertaining to a particular named field.
    /// Used for returning multiple field validation errors.s
    /// </summary>
    [DataContract]
    public class ResponseError : IMeta
    {
        [DataMember(IsRequired = false, EmitDefaultValue = false, Order = 1)]
        public string ErrorCode { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false, Order = 2)]
        public string FieldName { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false, Order = 3)]
        public string Message { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false, Order = 4)]
        public Dictionary<string, string> Meta { get; set; }
    }
}
