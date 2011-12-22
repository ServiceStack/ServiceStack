using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.ServiceInterface
{
    [Flags]
    public enum ApplyTo
    {
        None = 0,
        All = Get | Post | Put | Delete | Patch | Options | Head,
        Get = 1 << 0,
        Post = 1 << 1,
        Put = 1 << 2,
        Delete = 1 << 3,
        Patch = 1 << 4,
        Options = 1 << 5,
        Head = 1 << 6
    }

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
