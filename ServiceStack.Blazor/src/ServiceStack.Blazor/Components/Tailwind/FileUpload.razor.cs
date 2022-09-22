using System.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class FileUpload<TReq> : UiComponentBase
{
    [Parameter, EditorRequired]
    public TReq Request { get; set; }

    [Parameter]
    public string FilePropertyName { get; set; }

    [Inject] JsonApiClient Client { get; set; }

    ResponseStatus? errorStatus;

    private bool uploading = false;
    private string selectedFile = "";
    // Hack to clear files, avoids issues upload file with same name.
    bool toggleInputClear = false;
    ElementReference fileDropContainer;

    string progress = "Waiting";

    [Parameter]
    public EventCallback OnUploadStarted { get; set; }

    [Parameter]
    public EventCallback OnUploadComplete { get; set; }

    private async Task OnChange(InputFileChangeEventArgs e)
    {
        try
        {
            uploading = true;
            progress = "Started...";
            await UploadFile(e);
        }
        catch (Exception ex)
        {
            if (errorStatus == null)
            {
                errorStatus = new ResponseStatus
                {
                    Errors = new List<ResponseError>()
                    {
                        new()
                        {
                            Message = ex.Message
                        }
                    }
                };
            }
            progress = $"Failed...";
        }
        finally
        {
            StateHasChanged();
            await Task.Delay(3000);
            uploading = false;
            toggleInputClear = !toggleInputClear;
            StateHasChanged();
        }
    }

    long maxFileSize = 1024 * 1024 * 15;

    async Task UploadFile(InputFileChangeEventArgs e)
    {
        await OnUploadStarted.InvokeAsync();

        using var content = new MultipartFormDataContent()
            .AddParams(Request);

        var file = e.File;
        selectedFile = file.Name;
        progress = $"Uploading {selectedFile}...";
        if (file.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new Exception("Invalid file name.");
        }
        using var stream = file.OpenReadStream(maxFileSize);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;
        content.AddFile(FilePropertyName, file.Name, ms, file.ContentType);

        var api = await Client.ApiFormAsync<TReq>(
            Request.GetType().ToApiUrl(), content
        );
        if (!api.Succeeded)
            errorStatus = api.Error;

        StateHasChanged();
        progress = "Upload complete!";

        await OnUploadComplete.InvokeAsync();
        StateHasChanged();
    }
}
