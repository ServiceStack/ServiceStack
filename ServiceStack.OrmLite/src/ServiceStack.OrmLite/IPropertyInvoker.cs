//
// ServiceStack.OrmLite: Light-weight POCO ORM for .NET and Mono
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Reflection;

namespace ServiceStack.OrmLite
{
    public interface IPropertyInvoker
    {
        Func<object, Type, object> ConvertValueFn { get; set; }

        void SetPropertyValue(PropertyInfo propertyInfo, Type fieldType, object onInstance, object withValue);

        object GetPropertyValue(PropertyInfo propertyInfo, object fromInstance);
    }
}