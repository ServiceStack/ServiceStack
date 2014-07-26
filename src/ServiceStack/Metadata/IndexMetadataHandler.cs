using System;

namespace ServiceStack.Metadata
{
    public class IndexMetadataHandler : BaseSoapMetadataHandler
    {
        public override Format Format { get { return Format.Soap12; } }

        protected override string CreateMessage(Type dtoType)
        {
            return null;
            //throw new System.NotImplementedException();
        }
    }
}