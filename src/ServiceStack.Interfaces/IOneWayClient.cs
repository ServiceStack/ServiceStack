using System.Collections.Generic;

namespace ServiceStack
{
    public interface IOneWayClient
    {
        void SendOneWay(object requestDto);

        void SendOneWay(string relativeOrAbsoluteUri, object requestDto);

        void SendAllOneWay(IEnumerable<object> requests);
    }
}