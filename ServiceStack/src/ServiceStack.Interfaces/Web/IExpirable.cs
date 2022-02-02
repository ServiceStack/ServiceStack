using System;

namespace ServiceStack.Web
{
    public interface IExpirable
    {
        DateTime? LastModified { get; }
    }
}