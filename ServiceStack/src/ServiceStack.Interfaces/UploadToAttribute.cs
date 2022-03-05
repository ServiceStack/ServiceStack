using System;

namespace ServiceStack;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class UploadToAttribute : AttributeBase
{
    public string Location { get; set; }

    public UploadToAttribute(string location) => Location = location;
}