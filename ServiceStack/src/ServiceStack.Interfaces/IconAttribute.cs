using System;

namespace ServiceStack;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class IconAttribute : AttributeBase
{
    public string Svg { get; set; }
    public string Uri { get; set; }
    public string Alt { get; set; }
    public string Cls { get; set; }
}
