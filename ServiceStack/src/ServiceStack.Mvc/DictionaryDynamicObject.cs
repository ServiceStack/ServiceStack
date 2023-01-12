using System;
using System.Collections.Generic;

namespace ServiceStack.Mvc;

public class DictionaryDynamicObject : System.Dynamic.DynamicObject
{
    Dictionary<string, object> Object { get; }
    public DictionaryDynamicObject(Dictionary<string, object> obj) => Object = obj;

    public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result)
    {
        if (!Object.TryGetValue(binder.Name, out var ret))
            throw new InvalidOperationException(binder.Name);

        var modelType = ret?.GetType();
        if (modelType is { IsPublic: false } 
            && modelType.BaseType == typeof(object) 
            && modelType.DeclaringType == null)
        {
            result = new DictionaryDynamicObject(ret.ToObjectDictionary());
        }
        else
        {
            result = ret;
        }

        return true;
    }
}