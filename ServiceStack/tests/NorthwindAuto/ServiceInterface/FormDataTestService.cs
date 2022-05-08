using ServiceStack;

namespace MyApp.ServiceInterface;

public enum Colors
{
    Transparent,
    Red,
    Green,
    Blue,
}

public class FormDataTest : IReturn<FormDataTest>
{
    public bool Hidden { get; set; }
    public string? String { get; set; }
    public int Int { get; set; }
    public DateTime DateTime { get; set; }
    public DateOnly DateOnly { get; set; }
    public TimeSpan TimeSpan { get; set; }
    public TimeOnly TimeOnly { get; set; }
    public string? Password { get; set; }
    public string[]? CheckboxString { get; set; }
    public string? RadioString { get; set; }
    public Colors RadioColors { get; set; }
    public Colors[]? CheckboxColors { get; set; }
    public Colors SelectColors { get; set; }
    public Colors[]? MultiSelectColors { get; set; }
    
    [Input(Type = "file"), UploadTo("profiles")]
    public string? ProfileUrl { get; set; }

    [Input(Type = "file"), UploadTo("applications")]
    public List<Attachment> Attachments { get; set; }
}

public class Attachment
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string ContentType { get; set; }
    public long ContentLength { get; set; }
}

public class FormDataTestService : Service
{
    public object Any(FormDataTest request) => request;
}