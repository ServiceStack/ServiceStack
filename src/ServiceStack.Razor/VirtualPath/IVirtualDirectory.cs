using System;
using System.Collections.Generic;

namespace ServiceStack.Razor.VirtualPath
{
    public interface IVirtualDirectory : IVirtualNode, IEnumerable<IVirtualNode>
    {
        IVirtualFile GetFile(String virtualPath);
        IVirtualFile GetFile(Stack<String> virtualPath);

        IVirtualDirectory GetDirectory(String virtualPath);
        IVirtualDirectory GetDirectory(Stack<String> virtualPath);

        IEnumerable<IVirtualFile> GetAllMatchingFiles(String globPattern, int maxDepth = Int32.MaxValue);

        #region Properties

        IEnumerable<IVirtualFile> Files { get; }
        IEnumerable<IVirtualDirectory> Directories { get; }

        bool IsRoot { get; }

        #endregion
    }
}
