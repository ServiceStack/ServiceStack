using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// Indicates that the request dto, which is associated with this attribute,
    /// can only execute, if the user has specific permissions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class RequiredPermissionAttribute : Attribute
    {
        public List<string> RequiredPermissions { get; set; }
        public ApplyTo ApplyTo { get; set; }

        public RequiredPermissionAttribute(params string[] permissions)
        {
            this.RequiredPermissions = permissions.ToList();
            this.ApplyTo = ApplyTo.All;
        }

        public RequiredPermissionAttribute(ApplyTo applyTo, params string[] permissions)
        {
            this.RequiredPermissions = permissions.ToList();
            this.ApplyTo = applyTo;
        }
    }
}
