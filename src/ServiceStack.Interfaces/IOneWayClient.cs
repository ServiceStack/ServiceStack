namespace ServiceStack
{
    public interface IOneWayClient
    {
        void SendOneWay(object requestDto);

        void SendOneWay(string relativeOrAbsoluteUri, object requestDto);
    }
}