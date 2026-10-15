using GridironFrontOffice.DevHost.Components;
using GridironFrontOffice.UI;

var builder = WebApplication.CreateBuilder(args);

// GFO services, logging and the dev save folder (AppPaths redirect) are wired in M0 WP5 and WP9.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(AppRoot).Assembly);

await app.RunAsync();
