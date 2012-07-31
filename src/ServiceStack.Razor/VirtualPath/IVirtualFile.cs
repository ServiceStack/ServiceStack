using System;
using System.IO;

namespace ServiceStack.Razor.VirtualPath
{
    public interface IVirtualFile : IVirtualNode
    {
        String GetFileHash();

        Stream OpenRead();
        StreamReader OpenText();
        String ReadAllText();

        #region Properties

        DateTime LastModified { get; }

        #endregion
    }
}
