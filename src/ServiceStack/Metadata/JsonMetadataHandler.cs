using System;
using ServiceStack.Serialization;
using ServiceStack.Text;

namespace ServiceStack.Metadata
{
    public class JsonMetadataHandler : BaseMetadataHandler
    {
        public override Format Format { get { return Format.Json; } }

        protected override string CreateMessage(Type dtoType)
        {
            var requestObj = AutoMappingUtils.PopulateWith(dtoType.CreateInstance());
            return JsonDataContractSerializer.Instance.SerializeToString(requestObj);
        }
    }
}