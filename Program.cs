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
builder.Services.AddHttpContextAccessor();

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
		options.ApiUrl = "http://supervisor/core";
	}
});

// Register IHomeAssistantClient - use real client if token is available, mock otherwise
var supervisorToken = Environment.GetEnvironmentVariable("SUPERVISOR_TOKEN");
var useRealClient = !string.IsNullOrEmpty(supervisorToken);
Console.WriteLine($"[HA Integration] SUPERVISOR_TOKEN present: {useRealClient}");
if (useRealClient)
{
	Console.WriteLine("[HA Integration] Using REAL HomeAssistantClient");
	builder.Services.AddHttpClient<IHomeAssistantClient, HomeAssistantClient>();
}
else
{
	Console.WriteLine("[HA Integration] Using MOCK HomeAssistantClient (no token found)");
	builder.Services.AddScoped<IHomeAssistantClient, MockHomeAssistantClient>();
}

// Store client type for UI access
builder.Services.AddSingleton(new HomeAssistantClientInfo { IsRealClient = useRealClient });

// Action Log Service - stores execution history
var actionLogPath = builder.Environment.IsDevelopment()
	? Path.Combine(Directory.GetCurrentDirectory(), "data", "action_log.yaml")
	: "/data/action_log.yaml";
builder.Services.AddSingleton<IActionLogService>(new YamlActionLogService(actionLogPath, maxEntries: 100));

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

// Home Assistant Ingress detection middleware
// HA Ingress proxies requests with the original path, we need to detect and extract the base path
app.Use(async (context, next) =>
{
	// Check for X-Ingress-Path header (some HA versions)
	var ingressPath = context.Request.Headers["X-Ingress-Path"].FirstOrDefault();
	
	// If no header, try to detect from Referer or other means
	if (string.IsNullOrEmpty(ingressPath))
	{
		// Check if request path looks like an ingress path
		var path = context.Request.Path.Value ?? "";
		var referer = context.Request.Headers["Referer"].FirstOrDefault() ?? "";
		
		// HA Ingress paths look like: /api/hassio_ingress/<token>/
		if (referer.Contains("/api/hassio_ingress/"))
		{
			var uri = new Uri(referer);
			var segments = uri.AbsolutePath.Split('/');
			// Find the ingress token segment
			for (int i = 0; i < segments.Length - 1; i++)
			{
				if (segments[i] == "hassio_ingress" && i > 0)
				{
					ingressPath = string.Join("/", segments.Take(i + 2));
					break;
				}
			}
		}
	}
	
	if (!string.IsNullOrEmpty(ingressPath))
	{
		context.Request.PathBase = new PathString(ingressPath);
	}
	
	await next();
});

// Static environment variable fallback for path base
var envIngressPath = Environment.GetEnvironmentVariable("INGRESS_PATH");
if (!string.IsNullOrEmpty(envIngressPath))
{
	app.UsePathBase(new PathString(envIngressPath));
	Console.WriteLine($"Using Ingress path base from env: {envIngressPath}");
}

// Don't use HTTPS redirection in add-on (Ingress handles it)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Configure static files with proper MIME types
var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
// Ensure common web file types have correct MIME types
provider.Mappings[".css"] = "text/css";
provider.Mappings[".js"] = "application/javascript";
provider.Mappings[".json"] = "application/json";
provider.Mappings[".woff"] = "font/woff";
provider.Mappings[".woff2"] = "font/woff2";
provider.Mappings[".ttf"] = "font/ttf";
provider.Mappings[".svg"] = "image/svg+xml";
provider.Mappings[".png"] = "image/png";
provider.Mappings[".ico"] = "image/x-icon";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
