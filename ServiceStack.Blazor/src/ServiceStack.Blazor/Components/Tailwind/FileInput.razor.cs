using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Linq.Expressions;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// File Input Control
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/FileInput.png)
/// </remarks>
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

    /// <summary>
    /// Gets or sets an expression that identifies the bound value.
    /// </summary>
    [Parameter] public Expression<Func<string>>? ValueExpression { get; set; }

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

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (!hasInitializedParameters)
        {
            // This is the first run
            // Could put this logic in OnInit, but its nice to avoid forcing people who override OnInit to call base.OnInit()
            if (ValueExpression == null && Id == null)
            {
                throw new InvalidOperationException($"{GetType()} requires a value for the 'ValueExpression' " +
                                                    $"parameter. Normally this is provided automatically when using 'bind-Value'.");
            }
            
            Id ??= (FieldIdentifier = Microsoft.AspNetCore.Components.Forms.FieldIdentifier.Create(ValueExpression!)).FieldName;

            nullableUnderlyingType = Nullable.GetUnderlyingType(typeof(string));
            hasInitializedParameters = true;
        }

        // For derived components, retain the usual lifecycle with OnInit/OnParametersSet/etc.
        return base.SetParametersAsync(ParameterView.Empty);
    }

    public async ValueTask DisposeAsync()
    {
        await JS.InvokeVoidAsync("Files.flush");
    }
}
