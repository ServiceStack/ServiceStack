using System;
using System.Collections.Generic;

namespace ServiceStack.DataAnnotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class CompositeKeyAttribute : AttributeBase
{
    public CompositeKeyAttribute()
    {
        this.FieldNames = [];
    }

    public CompositeKeyAttribute(params string[] fieldNames)
    {
        this.FieldNames = [..fieldNames];
    }

    public List<string> FieldNames { get; set; }
}