namespace ServiceStack.IO
{
    public interface IHasVirtualFiles
    {
        bool IsDirectory { get; }
        bool IsFile { get; }

        IVirtualFile GetFile();
        
        IVirtualDirectory GetDirectory();
    }
}