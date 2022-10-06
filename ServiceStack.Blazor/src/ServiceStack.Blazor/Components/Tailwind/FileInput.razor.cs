using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class FileInput : TextInputBase, IAsyncDisposable
{
    [Inject] public IJSRuntime JS { get; set; }
    [Parameter] public bool IsMultiple { get; set; }
    [Parameter] public string? Value { get; set; }
    [Parameter] public ICollection<string>? Values { get; set; }
    [Parameter] public UploadedFile? File { get; set; }
    [Parameter] public ICollection<UploadedFile>? Files { get; set; }
    [Parameter] public EventCallback<InputFileChangeEventArgs> OnInput { get; set; }

    InputFile? InputFile { get; set; }

    List<UploadedFile>? inputFiles;

    protected virtual async Task OnChange(InputFileChangeEventArgs e)
    {
        inputFiles = await JS.InvokeAsync<List<UploadedFile>>("Files.inputFiles", InputFile!.Element);
        await OnInput.InvokeAsync(e);
    }

    async Task openFile()
    {
        await JS.InvokeVoidAsync("JS.invoke", InputFile!.Element, "click");
    }

    public async ValueTask DisposeAsync()
    {
        await JS.InvokeVoidAsync("Files.flush");
    }
}
