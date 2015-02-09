using System;
using ServiceStack.Serialization;
using ServiceStack.Text;

namespace ServiceStack.Metadata
{
    public class XmlMetadataHandler : BaseMetadataHandler
    {
        public override Format Format { get { return Format.Xml; } }

        protected override string CreateMessage(Type dtoType)
        {
            var requestObj = AutoMappingUtils.PopulateWith(dtoType.CreateInstance());
            return DataContractSerializer.Instance.Parse(requestObj, true);
        }
    }
}