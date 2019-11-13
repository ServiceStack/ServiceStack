using System.Runtime.Serialization;

namespace ServiceStack
{
    /// <summary>
    /// void methods that still need to return error information can return an EmptyResponse
    /// </summary>
    [DataContract]
    public class EmptyResponse : IHasResponseStatus
    {
        [DataMember(Order = 1)]
        public ResponseStatus ResponseStatus { get; set; }
    }
}