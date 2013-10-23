// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Reflection;

namespace ServiceStack
{
    public static class AttributeExtensions
    {
        public static string GetDescription(this Type type)
        {
            var componentDescAttr = type.FirstAttribute<System.ComponentModel.DescriptionAttribute>();
            if (componentDescAttr != null)
            {
                return componentDescAttr.Description;
            }

            var ssDescAttr = type.FirstAttribute<ServiceStack.DataAnnotations.DescriptionAttribute>();
            if (ssDescAttr != null)
            {
                return ssDescAttr.Description;
            }

            return null;
        }

        public static string GetDescription(this MemberInfo pi)
        {
            var componentDescAttr = pi.FirstAttribute<System.ComponentModel.DescriptionAttribute>();
            if (componentDescAttr != null)
            {
                return componentDescAttr.Description;
            }

            var ssDescAttr = pi.FirstAttribute<ServiceStack.DataAnnotations.DescriptionAttribute>();
            if (ssDescAttr != null)
            {
                return ssDescAttr.Description;
            }

            return null;
        }

        public static string GetDescription(this ParameterInfo pi)
        {
            var componentDescAttr = pi.FirstAttribute<System.ComponentModel.DescriptionAttribute>();
            if (componentDescAttr != null)
            {
                return componentDescAttr.Description;
            }

            var ssDescAttr = pi.FirstAttribute<ServiceStack.DataAnnotations.DescriptionAttribute>();
            if (ssDescAttr != null)
            {
                return ssDescAttr.Description;
            }

            return null;
        }
    }
}