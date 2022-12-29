using System;

namespace ServiceStack.Text
{
    /// <summary>
    /// Allow Type to be deserialized into late-bound object Types using __type info
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RuntimeSerializableAttribute : Attribute {}

    /// <summary>
    /// Allow Type to be deserialized into late-bound object Types using __type info
    /// </summary>
    public interface IRuntimeSerializable { }
}