using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack;

/// <summary>
/// void methods that still need to return error information can return an EmptyResponse
/// </summary>
[DataContract]
public class EmptyResponse : IHasResponseStatus
{
    [DataMember(Order = 1)]
    public ResponseStatus ResponseStatus { get; set; }
}
    
[DataContract]
public class IdResponse : IHasResponseStatus
{
    [DataMember(Order = 1)]
    public string Id { get; set; }

    [DataMember(Order = 2)]
    public ResponseStatus ResponseStatus { get; set; }
}
    
[DataContract]
public class StringsResponse : IHasResponseStatus, IMeta
{
    [DataMember(Order = 1)]
    public List<string> Results { get; set; }

    [DataMember(Order = 2)]
    public Dictionary<string, string> Meta { get; set; }

    [DataMember(Order = 3)]
    public ResponseStatus ResponseStatus { get; set; }
}
     
[DataContract]
public class StringResponse : IHasResponseStatus, IMeta
{
    [DataMember(Order = 1)]
    public string Result { get; set; }

    [DataMember(Order = 2)]
    public Dictionary<string, string> Meta { get; set; }

    [DataMember(Order = 3)]
    public ResponseStatus ResponseStatus { get; set; }
}
    
[DataContract]
public class IntResponse : IHasResponseStatus, IMeta
{
    [DataMember(Order = 1)]
    public int Result { get; set; }

    [DataMember(Order = 2)]
    public Dictionary<string, string> Meta { get; set; }

    [DataMember(Order = 3)]
    public ResponseStatus ResponseStatus { get; set; }
}
     
[DataContract]
public class BoolResponse : IHasResponseStatus, IMeta
{
    [DataMember(Order = 1)]
    public bool Result { get; set; }

    [DataMember(Order = 2)]
    public Dictionary<string, string> Meta { get; set; }

    [DataMember(Order = 3)]
    public ResponseStatus ResponseStatus { get; set; }
}