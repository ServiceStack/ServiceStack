{{*

ServiceStack.Blazor Components are easily customizable by modifying a local copy their *.razor UI markup that can be copied from:
https://github.com/ServiceStack/ServiceStack/tree/master/src/ServiceStack.Blazor/Components/Tailwind

This README is executable, to copy all ServiceStack.Blazor Tailwind components into this folder, run:

    $ x run README.ss

Then to use your local version, in your _Imports.razor replace:

    @using ServiceStack.Blazor.Components.Tailwind

with:

    @using MyApp.Client.Shared.Components

Available themes: Bootstrap, Tailwind

> PRs welcome!

*}}


```code
var theme = 'Tailwind' 
var fs = vfsFileSystem('.')
#each name in 'AlertSuccess,CheckboxInput,DateTimeInput,DynamicInput,ErrorSummary,SelectInput,TextAreaInput,TextInput'.split(',')
    var url = `https://raw.githubusercontent.com/ServiceStack/ServiceStack/master/src/ServiceStack.Blazor/Components/${theme}/${name}.razor`
    url |> urlContents |> to => contents
    fs.writeFile(`${name}.razor`, contents)
/each
```
