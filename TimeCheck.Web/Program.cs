using TimeCheck.Web.Components;
using TimeCheck.Shared.Services;
using TimeCheck.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add device-specific services used by the TimeCheck.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
// TimeCheck PWA services
builder.Services.AddScoped<ITtsService, TtsService>();
builder.Services.AddScoped<ISettingsService, BrowserSettingsService>();
builder.Services.AddScoped<ITimeCheckService, TimeCheckService>();
builder.Services.AddScoped<IEncouragementService, EncouragementService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(TimeCheck.Shared._Imports).Assembly);

app.Run();
