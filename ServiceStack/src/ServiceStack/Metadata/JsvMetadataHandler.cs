using System;
using ServiceStack.Text;

namespace ServiceStack.Metadata
{
    public class JsvMetadataHandler : BaseMetadataHandler
    {
        public override Format Format => Format.Jsv;

        protected override string CreateMessage(Type dtoType)
        {
            var requestObj = AutoMappingUtils.PopulateWith(Activator.CreateInstance(dtoType));
            return requestObj.SerializeAndFormat();
        }
    }
}