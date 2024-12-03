using System.IO;
using NUnit.Framework;

namespace ServiceStack.Text.Tests;

public class StringSerializerTests
    : TestBase
{
    [Test]
    public void Can_serialize_null_object_to_Stream()
    {
        using (var ms = new MemoryStream())
        {
            JsonSerializer.SerializeToStream((object)null, ms);
            TypeSerializer.SerializeToStream((object)null, ms);
            XmlSerializer.SerializeToStream((object)null, ms);
        }
    }
}
