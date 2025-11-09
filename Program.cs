using SwampTimers.Components;
using SwampTimers.Services;
using SwampTimers.Models;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs - listen on all interfaces for Docker/Add-on use
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://0.0.0.0:8080");
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Configure storage options from appsettings.json
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection("Storage"));

// Timer Service Registration - uses factory pattern with configuration
builder.Services.AddScoped<ITimerService>(sp =>
{
    var config = builder.Configuration.GetSection("Storage").Get<StorageOptions>()
        ?? new StorageOptions();
    return TimerServiceFactory.Create(config);
});

var app = builder.Build();

// Initialize the timer service database on startup
using (var scope = app.Services.CreateScope())
{
    var timerService = scope.ServiceProvider.GetRequiredService<ITimerService>();
    await timerService.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // Don't use HSTS in add-on (Ingress handles HTTPS)
    // app.UseHsts();
}

// For Home Assistant Ingress support - handle base path
var ingressPath = Environment.GetEnvironmentVariable("INGRESS_PATH");
if (!string.IsNullOrEmpty(ingressPath))
{
    app.UsePathBase(new PathString(ingressPath));
}

// Don't use HTTPS redirection in add-on (Ingress handles it)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
