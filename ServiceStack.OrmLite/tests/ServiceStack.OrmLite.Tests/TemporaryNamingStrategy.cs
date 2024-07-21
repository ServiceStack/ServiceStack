using System;
using System.Diagnostics;

namespace ServiceStack.OrmLite.Tests;

public class TemporaryNamingStrategy : IDisposable
{
    private readonly IOrmLiteDialectProvider _dialectProvider;
    private readonly INamingStrategy _previous;
    private bool _disposed;

    public TemporaryNamingStrategy(IOrmLiteDialectProvider dialectProvider, INamingStrategy temporary)
    {
        _dialectProvider = dialectProvider;
        _previous = _dialectProvider.NamingStrategy;
        _dialectProvider.NamingStrategy = temporary;
    }

#if DEBUG
    ~TemporaryNamingStrategy()
    {
        Debug.Assert(_disposed, "TemporaryNamingStrategy was not disposed of - previous naming strategy was not restored");
    }
#endif

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _dialectProvider.NamingStrategy = _previous;

#if DEBUG
            GC.SuppressFinalize(this);
#endif
        }
    }
}