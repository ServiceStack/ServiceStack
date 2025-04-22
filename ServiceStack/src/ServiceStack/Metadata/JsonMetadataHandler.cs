using System;
using ServiceStack.Serialization;

namespace ServiceStack.Metadata;

public class JsonMetadataHandler : BaseMetadataHandler
{
    public override Format Format => Format.Json;

    protected override string CreateMessage(Type dtoType)
    {
        var requestObj = HostContext.GetPlugin<MetadataFeature>()?.CreateExampleObjectFn(dtoType) 
            ?? AutoMappingUtils.PopulateWith(dtoType.CreateInstance());
        return JsonDataContractSerializer.Instance.SerializeToString(requestObj);
    }
}