using BlazorMudApp.Components;
using BlazorMudApp.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Timer Service Registration - now using Scoped for Blazor Server
builder.Services.AddScoped<ITimerService>(sp => new SqliteTimerService("timers.db"));

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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
