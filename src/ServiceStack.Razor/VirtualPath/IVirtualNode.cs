using System;

namespace ServiceStack.Razor.VirtualPath
{
    public interface IVirtualNode
    {
        #region Properties

        String Name { get; }
        String VirtualPath { get;}
        String RealPath { get; }
        Boolean IsDirectory { get; }

        #endregion
    }
}
