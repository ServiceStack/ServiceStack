To use Tailwind.* components you'll need to include tailwind classes.

Either by including the minified css with all the classes used:

 - https://raw.githubusercontent.com/ServiceStack/ServiceStack/master/src/ServiceStack.Blazor/dist/tailwind.css

 Or to bundle it together with your Tailwind app, copy a concatenation of all Component markup into your /wwwroot so Tailwind purge can find it:

 - https://raw.githubusercontent.com/ServiceStack/ServiceStack/master/src/ServiceStack.Blazor/dist/tailwind.html

