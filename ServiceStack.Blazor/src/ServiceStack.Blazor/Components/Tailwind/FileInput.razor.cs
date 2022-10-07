using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class FileInput : TextInputBase, IAsyncDisposable
{
    [Inject] public IJSRuntime JS { get; set; }
    [Parameter] public bool Multiple { get; set; }
    [Parameter] public string? Accept { get; set; }
    [Parameter] public string? Value { get; set; }
    [Parameter] public ICollection<string>? Values { get; set; }
    [Parameter] public UploadedFile? File { get; set; }
    [Parameter] public ICollection<UploadedFile>? Files { get; set; }
    [Parameter] public EventCallback<InputFileChangeEventArgs> OnInput { get; set; }

    public List<UploadedFile> FileList
    {
        get
        {
            if (inputFiles != null)
                return inputFiles;
            var to = new List<UploadedFile>();
            foreach (var value in Values.OrEmpty())
            {
                to.Add(new UploadedFile { FileName = value, ContentType = MimeTypes.GetMimeType(value) });
            }
            foreach (var item in Files.OrEmpty())
            {
                to.Add(item);
            }
            return to;
        }
    }

    string? FallbackSrc { get; set; }
    Dictionary<string, string> FallbackSrcMap = new();

    InputFile? InputFile { get; set; }

    List<UploadedFile>? inputFiles;

    protected virtual async Task OnChange(InputFileChangeEventArgs e)
    {
        FallbackSrc = null;
        FallbackSrcMap.Clear();

        inputFiles = await JS.InvokeAsync<List<UploadedFile>>("Files.inputFiles", InputFile!.Element);
        await OnInput.InvokeAsync(e);
    }

    async Task openFile()
    {
        await JS.InvokeVoidAsync("JS.invoke", InputFile!.Element, "click");
    }

    protected override void OnParametersSet()
    {
        FallbackSrc = null;
        FallbackSrcMap.Clear();

        base.OnParametersSet();
    }

    public async ValueTask DisposeAsync()
    {
        await JS.InvokeVoidAsync("Files.flush");
    }
}
