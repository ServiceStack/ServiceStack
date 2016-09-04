using System;

namespace ServiceStack.Metadata
{
    public class IndexMetadataHandler : BaseSoapMetadataHandler
    {
        public override Format Format => Format.Soap12;

        protected override string CreateMessage(Type dtoType) => null;
    }
}