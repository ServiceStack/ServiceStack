using System;
using ServiceStack.Text;

namespace ServiceStack.Metadata;

public class JsvMetadataHandler : BaseMetadataHandler
{
    public override Format Format => Format.Jsv;

    protected override string CreateMessage(Type dtoType)
    {
        var requestObj = HostContext.GetPlugin<MetadataFeature>()?.CreateExampleObjectFn(dtoType) 
            ?? AutoMappingUtils.PopulateWith(dtoType.CreateInstance());
        return requestObj.SerializeAndFormat();
    }
}