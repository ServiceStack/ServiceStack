// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Linq;
using System.Reflection;

namespace ServiceStack;

/// <summary>
/// Required in both ServiceStack .Common + .Client
/// </summary>
public static class AttributeExtensions
{
    static TAttr FirstAttribute<TAttr>(Type type) where TAttr : class => (TAttr)type.GetCustomAttributes(typeof(TAttr),true).FirstOrDefault();
    static TAttribute FirstAttribute<TAttribute>(MemberInfo memberInfo) => AllAttributes<TAttribute>(memberInfo).FirstOrDefault();
    static TAttribute FirstAttribute<TAttribute>(ParameterInfo pi) => AllAttributes<TAttribute>(pi).FirstOrDefault();
    static TAttr[] AllAttributes<TAttr>(MemberInfo mi) => AllAttributes(mi, typeof(TAttr)).Cast<TAttr>().ToArray();
    static object[] AllAttributes(MemberInfo memberInfo, Type attrType) => memberInfo.GetCustomAttributes(attrType, true);
    static TAttr[] AllAttributes<TAttr>(ParameterInfo pi) => pi.GetCustomAttributes(typeof(TAttr), true).Cast<TAttr>().ToArray();

    public static string GetNotes(this Type type) => FirstAttribute<NotesAttribute>(type)?.Notes;
    public static string GetDescription(this Type type)
    {
        var apiAttr = FirstAttribute<ApiAttribute>(type);
        if (apiAttr != null)
            return apiAttr.Description;

        var componentDescAttr = FirstAttribute<System.ComponentModel.DescriptionAttribute>(type);
        if (componentDescAttr != null)
            return componentDescAttr.Description;

        var ssDescAttr = FirstAttribute<ServiceStack.DataAnnotations.DescriptionAttribute>(type);
        return ssDescAttr?.Description;
    }

    public static string GetDescription(this MemberInfo mi)
    {
        var apiAttr = FirstAttribute<ApiMemberAttribute>(mi);
        if (apiAttr != null)
            return apiAttr.Description;

        var componentDescAttr = FirstAttribute<System.ComponentModel.DescriptionAttribute>(mi);
        if (componentDescAttr != null)
            return componentDescAttr.Description;

        var ssDescAttr = FirstAttribute<ServiceStack.DataAnnotations.DescriptionAttribute>(mi);
        return ssDescAttr?.Description;
    }

    public static string GetDescription(this ParameterInfo pi)
    {
        var componentDescAttr = FirstAttribute<System.ComponentModel.DescriptionAttribute>(pi);
        if (componentDescAttr != null)
            return componentDescAttr.Description;

        var ssDescAttr = FirstAttribute<ServiceStack.DataAnnotations.DescriptionAttribute>(pi);
        return ssDescAttr?.Description;
    }
}
