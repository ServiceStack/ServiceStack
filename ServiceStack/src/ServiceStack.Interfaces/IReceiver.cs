namespace ServiceStack
{
    //Marker interface for Receiver classes, akin to IService
    public interface IReceiver
    {
        void NoSuchMethod(string selector, object message);
    }
}