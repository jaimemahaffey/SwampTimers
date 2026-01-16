using SwampTimers.Components;
using SwampTimers.Services;
using SwampTimers.Services.HomeAssistant;
using SwampTimers.Models;
using SwampTimers.Models.HomeAssistant;
using MudBlazor.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs - listen on all interfaces for Docker/Add-on use
if (!builder.Environment.IsDevelopment())
{
	builder.WebHost.UseUrls("http://0.0.0.0:8080");
}

// Configure Data Protection for containerized environments
// Persist keys to /data directory to survive container restarts
var dataProtectionPath = builder.Environment.IsDevelopment()
	? Path.Combine(Directory.GetCurrentDirectory(), "data", "keys")
	: "/data/keys";

Directory.CreateDirectory(dataProtectionPath);

builder.Services.AddDataProtection()
	.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
	.SetApplicationName("SwampTimers");

// Configure forwarded headers for Ingress proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
	options.KnownNetworks.Clear();
	options.KnownProxies.Clear();
});

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

// Home Assistant Integration
builder.Services.Configure<HomeAssistantOptions>(options =>
{
	var section = builder.Configuration.GetSection("HomeAssistant");
	section.Bind(options);

	// Override with environment variables if present (for HA add-on)
	var supervisorToken = Environment.GetEnvironmentVariable("SUPERVISOR_TOKEN");
	if (!string.IsNullOrEmpty(supervisorToken))
	{
		options.SupervisorToken = supervisorToken;
		options.ApiUrl = "http://supervisor/core/api";
	}
});

// Register IHomeAssistantClient - use real client if token is available, mock otherwise
var supervisorToken = Environment.GetEnvironmentVariable("SUPERVISOR_TOKEN");
if (!string.IsNullOrEmpty(supervisorToken))
{
	builder.Services.AddHttpClient<IHomeAssistantClient, HomeAssistantClient>();
}
else
{
	builder.Services.AddScoped<IHomeAssistantClient, MockHomeAssistantClient>();
}

// Background Timer Monitoring Service
builder.Services.AddHostedService<TimerMonitoringService>();

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

// Use forwarded headers from Ingress proxy
app.UseForwardedHeaders();

// For Home Assistant Ingress support - handle base path
var ingressPath = Environment.GetEnvironmentVariable("INGRESS_PATH");
if (!string.IsNullOrEmpty(ingressPath))
{
	app.UsePathBase(new PathString(ingressPath));
	Console.WriteLine($"Using Ingress path base: {ingressPath}");
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
