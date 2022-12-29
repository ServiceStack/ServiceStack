using ServiceStack;
using ServiceStack.DataAnnotations;
using TalentBlazor.ServiceModel;

namespace MyApp.ServiceModel;

public enum FileAccessType
{
    Public,
    Team,
    Private,
}

[Tag("Files")]
public class QueryFileSystemItems : QueryDb<FileSystemItem>
{
    public int? AppUserId { get; set; }
    public FileAccessType? FileAccessType { get; set; }
}

[Tag("Files")]
public class QueryFileSystemFiles : QueryDb<FileSystemFile> {}

[Tag("Files")]
[AutoPopulate(nameof(FileSystemItem.AppUserId), Eval = "userAuthId")]
public class CreateFileSystemItem : ICreateDb<FileSystemItem>, IReturn<FileSystemItem>, IFileItem
{
    public FileAccessType? FileAccessType { get; set; }
    
    [Input(Type = "file"), UploadTo("fs")]
    public FileSystemFile File { get; set; }
}

public class FileSystemItem : IFileItem
{
    [AutoIncrement] 
    public int Id { get; set; }

    public FileAccessType? FileAccessType { get; set; }

    [Reference] 
    public FileSystemFile File { get; set; }

    [Ref(Model = nameof(AppUser), RefId = nameof(AppUser.Id), RefLabel = nameof(AppUser.DisplayName))]
    public int AppUserId { get; set; }
}

public class FileSystemFile : IFile
{
    [AutoIncrement] public int Id { get; set; }
        
    public string FileName { get; set; }

    [Format(FormatMethods.Attachment)] 
    public string FilePath { get; set; }
    public string ContentType { get; set; }

    [Format(FormatMethods.Bytes)] 
    public long ContentLength { get; set; }

    [References(typeof(FileSystemItem))] 
    public int FileSystemItemId { get; set; }
}

public interface IFileItem
{
    public FileAccessType? FileAccessType { get; set; }
}
public interface IFile
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string ContentType { get; set; }
    public long ContentLength { get; set; }
}
