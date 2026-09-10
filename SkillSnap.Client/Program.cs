using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SkillSnap.Client;
using SkillSnap.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

builder.RootComponents.Add<HeadOutlet>(
    "head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException(
        "ApiBaseUrl configuration is required.");

var hostBaseAddress =
    new Uri(builder.HostEnvironment.BaseAddress);

var apiBaseAddress =
    new Uri(hostBaseAddress, apiBaseUrl);

builder.Services.AddScoped(sp =>
{
    var handler = new ApiRequestHandler(
        sp.GetRequiredService<AntiforgeryTokenStore>())
    {
        InnerHandler = new HttpClientHandler()
    };

    return new HttpClient(handler)
    {
        BaseAddress = apiBaseAddress
    };
});

builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SkillService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<AntiforgeryTokenStore>();

await builder.Build().RunAsync();
