namespace ServiceStack.Redis.Support
{
    public interface ISerializer
    {

        byte[] Serialize(object value);
        object Deserialize(byte[] someBytes);
      
    }
}