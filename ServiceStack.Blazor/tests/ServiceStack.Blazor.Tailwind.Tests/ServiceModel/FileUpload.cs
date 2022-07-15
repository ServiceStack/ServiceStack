using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

public class CreateFileUpload : ICreateDb<FileUpload>, IReturn<IdResponse>
{
    [Input(Type = "file"), UploadTo("fs")]
    public string FilePath { get; set; }
    
    public string MyVal { get; set; }
}

public class QueryFileUpload : QueryDb<FileUpload>
{
    
}

public class FileUpload
{
    [AutoIncrement]
    public int Id { get; set; }
    
    public string FilePath { get; set; }
    public string MyVal { get; set; }
}