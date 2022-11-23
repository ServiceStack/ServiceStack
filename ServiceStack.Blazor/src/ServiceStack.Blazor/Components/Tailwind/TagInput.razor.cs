using System;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class TagInput : TextInputBase<List<string>>
{
    [Inject] public IJSRuntime JS { get; set; }

    ElementReference txtInput;

    string InputValue = "";
    string Key = "";

    [Parameter] public char[] Delimiters { get; set; } = new[] { ',' };

    public async Task RemoveTagAsync(string tag)
    {
        var newValue = Value.OrEmpty().Where(x => x != tag).ToList();
        await ValueChanged.InvokeAsync(newValue);
    }

    async Task handleClick() => await JS.InvokeVoidAsync("JS.elInvoke", txtInput, "focus");

    async Task KeyDownAsync(KeyboardEventArgs args)
    {
        Key = args.Key;
        if (args.Key == "Backspace" && string.IsNullOrEmpty(InputValue))
        {
            var lastTag = Value?.LastOrDefault();
            if (!string.IsNullOrEmpty(lastTag))
            {
                await RemoveTagAsync(lastTag);
            }
            return;
        }
    }

    async Task KeyPressAsync(KeyboardEventArgs args)
    {
        if (string.IsNullOrEmpty(InputValue))
            return;

        var tag = InputValue.Trim().TrimEnd(',').Trim();
        if (string.IsNullOrEmpty(tag))
            return;

        var isEnter = args.Key is "Enter" or "NumpadEnter";
        if (isEnter || args.Key.Length == 1 && Delimiters.Contains(args.Key[0]))
        {
            var newValue = new List<string>(Value.OrEmpty());
            newValue.AddIfNotExists(tag);
            await ValueChanged.InvokeAsync(newValue);

            InputValue = string.Empty;
            if (!isEnter)
            {
                //need to wait and overwrite oninput update
                await Task.Delay(1);
                InputValue = string.Empty;
            }
        }
    }

    async Task OnPasteAsync(ClipboardEventArgs e)
    {
        try
        {
            var clipboardText = await JS.InvokeAsync<string>("navigator.clipboard.readText");
            await HandlePastedTextAsync(clipboardText);
            return;
        }
        catch
        {
            // No permission to read from clipboard, wait for 2-way binding to update txtValue
        }

        // Need to wait for oninput to fire and update txtValue
        await Task.Delay(1);
        await HandlePastedTextAsync(InputValue);
    }

    protected virtual async Task HandlePastedTextAsync(string? txt)
    {
        if (string.IsNullOrEmpty(txt))
            return;

        var delims = new List<char>(Delimiters) {
            '\n', '\t',
        }.ToArray();

        Value ??= new();
        Value.AddDistinctRange(txt.Split(delims, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x)));

        await Task.Delay(1);
        InputValue = string.Empty;
        StateHasChanged();
    }
}
