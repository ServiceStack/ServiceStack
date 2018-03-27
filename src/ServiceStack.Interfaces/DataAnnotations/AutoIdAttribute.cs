using System;

namespace ServiceStack.DataAnnotations 
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class AutoIdAttribute : AttributeBase
    {
    }
}