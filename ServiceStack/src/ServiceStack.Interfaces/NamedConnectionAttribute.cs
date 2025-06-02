#nullable enable

using System;

namespace ServiceStack;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class NamedConnectionAttribute(string name) : AttributeBase
{
    public string Name { get; set; } = name;
}