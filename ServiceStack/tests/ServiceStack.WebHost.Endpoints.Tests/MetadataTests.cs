using System.Linq;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.NativeTypes;
using ServiceStack.Testing;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class MetadataTests
{
    public class BaseType
    {
        public int Id { get; set; }
    }
    
    public class SubType : BaseType, IReturn<SubType>
    {
        public string Name { get; set; }
    }
    
    public class MyServices : Service
    {
        public object Any(SubType request) => request;
    }

    [Test]
    public void Does_get_metadata_properties()
    {
        using var appHost = new BasicAppHost
        {
            Plugins =
            {
                new MetadataFeature(),
                new NativeTypesFeature(),
            },
            ConfigureAppHost = host => host.RegisterService<MyServices>()
        }.Init();
        var metadata = appHost.Resolve<INativeTypesMetadata>();

        var appMetadata = metadata.ToAppMetadata(new BasicHttpRequest
        {
            AbsoluteUri = Config.AbsoluteBaseUri,
        });
        var metaType = appMetadata.GetType(nameof(SubType));
        
        var props = appMetadata.GetAllProperties(metaType);
        Assert.That(props.Select(x => x.Name), Is.EquivalentTo(new[] { nameof(BaseType.Id), nameof(SubType.Name) }));
        Assert.That(props.GetPrimaryKey().Name, Is.EqualTo(nameof(BaseType.Id)));
    }
}
