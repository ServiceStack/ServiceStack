using System;
using ServiceStack.Serialization;

namespace ServiceStack.Metadata
{
    public class XmlMetadataHandler : BaseMetadataHandler
    {
        public override Format Format => Format.Xml;

        protected override string CreateMessage(Type dtoType)
        {
            var requestObj = AutoMappingUtils.PopulateWith(dtoType.CreateInstance());
            return DataContractSerializer.Instance.Parse(requestObj, true);
        }
    }
}