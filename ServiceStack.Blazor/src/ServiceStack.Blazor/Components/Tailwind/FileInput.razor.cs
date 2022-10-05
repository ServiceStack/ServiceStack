using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class FileInput<TValue> : TextInputBase<TValue>, IAsyncDisposable
{
    [Inject] public IJSRuntime JS { get; set; }
    [Parameter] public bool IsMultiple { get; set; }
    InputFile? InputFile { get; set; }

    IReadOnlyList<IBrowserFile>? browserFiles;

    List<UploadedFile>? inputFiles;

    protected virtual async Task OnChange(InputFileChangeEventArgs e)
    {
        browserFiles = e.GetMultipleFiles(int.MaxValue);
        inputFiles = await JS.InvokeAsync<List<UploadedFile>>("Files.inputFiles", InputFile!.Element);
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
