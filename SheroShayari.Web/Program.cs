using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using SheroShayari.Web;
using SheroShayari.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register MudBlazor services
builder.Services.AddMudServices();

// Register custom services
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Configure HttpClient for API
builder.Services.AddScoped<HttpClient>(sp =>
{
    var apiBaseAddress = builder.HostEnvironment.IsDevelopment() 
        ? "http://localhost:5057/" 
        : "https://your-api-domain:7057/";
    return new HttpClient { BaseAddress = new Uri(apiBaseAddress) };
});

// Register Shayari API client with dependency injection
builder.Services.AddScoped<IShayariApiClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var logger = sp.GetRequiredService<ILogger<ShayariApiClient>>();
    var authService = sp.GetRequiredService<IAuthService>();
    return new ShayariApiClient(httpClient, logger, authService);
});

await builder.Build().RunAsync();

