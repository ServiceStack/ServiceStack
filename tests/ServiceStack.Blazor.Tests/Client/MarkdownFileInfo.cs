using System;

namespace MyApp.Client;
public class MarkdownFileInfo
{
    public string? Path { get; set; }
    public string? FileName { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public DateTime? Date { get; set; }
    public string? Content { get; set; }
    public string? Preview { get; set; }
}
