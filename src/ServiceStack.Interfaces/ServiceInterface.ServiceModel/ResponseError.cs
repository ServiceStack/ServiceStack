//
// ServiceStack
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack
//
// Licensed under the new BSD license.
//

using System.Runtime.Serialization;

namespace ServiceStack.ServiceInterface.ServiceModel
{
    /// <summary>
    /// Error information pertaining to a particular named field.
    /// Used for returning multiple field validation errors.s
    /// </summary>
    [DataContract]
    public class ResponseError
    {
        [DataMember(Order = 1)]
        public string ErrorCode { get; set; }

        [DataMember(Order = 2)]
        public string FieldName { get; set; }

        [DataMember(Order = 3)]
        public string Message { get; set; }
    }
}