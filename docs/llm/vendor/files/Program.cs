using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CollabsKus.BlazorWebAssembly;
using CollabsKus.BlazorWebAssembly.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register our services
builder.Services.AddScoped<KathmanduCalendarService>();
builder.Services.AddScoped<MoonPhaseService>();
builder.Services.AddScoped<ApiLoggerService>();

await builder.Build().RunAsync();
