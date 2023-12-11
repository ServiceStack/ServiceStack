#if (NETCORE || NET6_0_OR_GREATER) && !NETSTANDARD2_0

using System;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace ServiceStack;

public class Net6PclExport : NetStandardPclExport
{
    public Net6PclExport()
    {
        this.PlatformName = Platforms.Net6;
        ReflectionOptimizer.Instance = EmitReflectionOptimizer.Provider;            
    }

    public override ParseStringDelegate GetJsReaderParseMethod<TSerializer>(Type type)
    {
        if (type.IsAssignableFrom(typeof(System.Dynamic.IDynamicMetaObjectProvider)) ||
            type.HasInterface(typeof(System.Dynamic.IDynamicMetaObjectProvider)))
        {
            return DeserializeDynamic<TSerializer>.Parse;
        }

        return null;
    }

    public override ParseStringSpanDelegate GetJsReaderParseStringSpanMethod<TSerializer>(Type type)
    {
        if (type.IsAssignableFrom(typeof(System.Dynamic.IDynamicMetaObjectProvider)) ||
            type.HasInterface(typeof(System.Dynamic.IDynamicMetaObjectProvider)))
        {
            return DeserializeDynamic<TSerializer>.ParseStringSpan;
        }
        
        return null;
    }
}

#endif