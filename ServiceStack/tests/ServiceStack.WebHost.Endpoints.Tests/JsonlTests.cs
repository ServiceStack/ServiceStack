using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;
using ServiceStack.Text.Json;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class JsonlTests
{
    private static List<Rockstar> SeedData => UnitTestExample.SeedData;
    
    [Test]
    public void Can_serialize_List_Rockstars()
    {
        var rockstars = SeedData;

        var jsonl = JsonlSerializer.SerializeToString(rockstars);
        var lines = jsonl.ReadLines().ToList();
        Assert.That(lines.Count, Is.EqualTo(SeedData.Count));

        var fromJsonl = JsonlSerializer.DeserializeFromString<List<Rockstar>>(jsonl);
        Assert.That(fromJsonl, Is.EquivalentTo(rockstars));
    }
    
    [Test]
    public void Can_serialize_AutoQuery_Rockstars()
    {
        var rockstars = new QueryResponse<Rockstar> {
            Results = SeedData
        };

        var jsonl = JsonlSerializer.SerializeToString(rockstars);
        var lines = jsonl.ReadLines().ToList();
        Assert.That(lines.Count, Is.EqualTo(SeedData.Count));

        var fromJsonl = JsonlSerializer.DeserializeFromString<QueryResponse<Rockstar>>(jsonl);
        Assert.That(fromJsonl.Results, Is.EquivalentTo(rockstars.Results));
    }
    
    [Test]
    public void Can_serialize_single_Rockstar()
    {
        var rockstar = SeedData[0];

        var jsonl = JsonlSerializer.SerializeToString(rockstar);
        var lines = jsonl.ReadLines().ToList();
        Assert.That(lines.Count, Is.EqualTo(1));

        var fromJsonl = JsonlSerializer.DeserializeFromString<Rockstar>(jsonl);
        Assert.That(fromJsonl, Is.EqualTo(rockstar));
    }
    
}