using System;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests;

[TestFixture]
public class XmlSerializerTests
{
    public void Serialize<T>(T data)
    {
        //TODO: implement serializer and test properly
        var xml = XmlSerializer.SerializeToString(data);
        Console.WriteLine(xml);
    }

    [Test]
    public void Can_Serialize_Movie()
    {
        Serialize(MoviesData.Movies[0]);
    }

    [Test]
    public void Can_Serialize_Movies()
    {
        Serialize(MoviesData.Movies);
    }

    [Test]
    public void Can_Serialize_MovieResponse_Dto()
    {
        Serialize(new MovieResponse { Movie = MoviesData.Movies[0] });
    }
}