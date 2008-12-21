namespace ServiceStack.Common.DesignPatterns.Serialization
{
    public interface IXmlSerializer
    {
        string Parse<XmlDto>(XmlDto from);
    }
}