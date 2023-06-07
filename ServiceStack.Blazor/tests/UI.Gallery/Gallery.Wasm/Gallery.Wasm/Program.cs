using ServiceStack;
using MyApp;
using Microsoft.Net.Http.Headers;

Licensing.RegisterLicense("OSS BSD-3-Clause 2023 https://github.com/NetCoreApps/BlazorGallery WZ4hSuvzmoeRsZSygYGa3b7y1F+ohVZhYvHsfbRBw3hr0Jhyk/xSrPOl86g8St+H9zll7ehDjG5D5176JvP9baU3zIoZKek3+RvDbr+Th/COMaXrnByIA/pBjIR7aApF6l8tLXA4qV05rEE7wNxla74QGCupdH5NU2r2algvLTU=");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();

var cacheFileExts = new[] { ".png", ".jpg", ".svg" };
app.UseStaticFiles(new StaticFileOptions {
    OnPrepareResponse = ctx => {
        if (cacheFileExts.Any(x => ctx.File.Name.EndsWith(x)))
        {
            ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + (60 * 60);
        }
    }
});

app.UseRouting();

app.UseServiceStack(new AppHost());

app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
    endpoints.MapControllers();
    endpoints.MapFallbackToFile("index.html");
});

app.Run();
