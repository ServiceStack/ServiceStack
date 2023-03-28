using System;

namespace ServiceStack.Metadata;

public class IndexMetadataHandler : BaseSoapMetadataHandler //TODO: refactor out SOAP base class
{
    public override Format Format => Format.Soap12;

    protected override string CreateMessage(Type dtoType) => null;
}