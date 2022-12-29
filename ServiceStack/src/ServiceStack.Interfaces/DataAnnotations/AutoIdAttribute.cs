using System;

namespace ServiceStack.DataAnnotations 
{
    /// <summary>
    /// Auto populate Property with RDBMS generated UUID if supported otherwise with a new C# GUID
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class AutoIdAttribute : AttributeBase
    {
    }
}