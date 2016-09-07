// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Reflection;

namespace ServiceStack
{
    internal abstract class RouteMember
    {
        public bool IgnoreInQueryString { get; set; }

        public abstract object GetValue(object target, bool excludeDefault = false);

        public abstract Type GetMemberType();
    }

    internal class FieldRouteMember : RouteMember
    {
        private readonly FieldInfo field;

        public FieldRouteMember(FieldInfo field)
        {
            this.field = field;
        }

        public override object GetValue(object target, bool excludeDefault)
        {
            var v = field.GetValue(target);
            if (excludeDefault && Equals(v, field.FieldType.GetDefaultValue())) return null;
            return v;
        }

        public override Type GetMemberType() => field.FieldType;
    }

    internal class PropertyRouteMember : RouteMember
    {
        private readonly PropertyInfo property;

        public PropertyRouteMember(PropertyInfo property)
        {
            this.property = property;
        }

        public override object GetValue(object target, bool excludeDefault)
        {
            var v = property.GetValue(target, null);
            if (excludeDefault && Equals(v, property.PropertyType.GetDefaultValue())) return null;
            return v;
        }

        public override Type GetMemberType() => property.PropertyType;
    }
}