using ServiceStack;
using MyApp;

Licensing.RegisterLicense("OSS BSD-3-Clause 2022 https://github.com/NetCoreApps/BlazorGalleryWasm Jkk6ELaIZcg1lgFFzn5XmYeazEeVDZeS2jytwjIWOM3Z00vmnZ3BDyZx0tyPX1tcI5T6yH4mbbI9ndC262D/qHFaTMb5eVv4KrSOdYPvgsjINN8JSZqxvMT4Xwemw4QOnSrSFyhil/H1G6+WnjTtcFPRl9x5h/ZIaQBpfXeFOR4=");

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
app.UseStaticFiles();

app.UseRouting();

app.UseServiceStack(new AppHost());

app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
    endpoints.MapControllers();
    endpoints.MapFallbackToFile("index.html");
});

app.Run();
