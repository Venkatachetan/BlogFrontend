using BlogFrontend;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlogFrontend.Services;
using Blazored.LocalStorage;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7183") };
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
builder.Services.AddScoped(sp => httpClient);

var customResolver = new CustomJsonTypeInfoResolver();

builder.Services.AddBlazoredLocalStorage(config =>
{
    var options = config.JsonSerializerOptions;
    options.PropertyNameCaseInsensitive = true;
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.WriteIndented = false;
    options.AllowTrailingCommas = true;
    options.ReadCommentHandling = JsonCommentHandling.Skip;
    options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    options.TypeInfoResolver = customResolver;
});

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNameCaseInsensitive = true;
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.WriteIndented = false;
    options.AllowTrailingCommas = true;
    options.ReadCommentHandling = JsonCommentHandling.Skip;
    options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    options.TypeInfoResolver = customResolver;
});

builder.Services.AddSingleton(new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    ReferenceHandler = ReferenceHandler.IgnoreCycles,
    WriteIndented = false,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    TypeInfoResolver = customResolver
});

builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AIContentService>();
builder.Services.AddSingleton<NotificationService>(); 
builder.Services.AddMudServices();

var host = builder.Build();

var navManager = host.Services.GetRequiredService<NavigationManager>();
var uri = new Uri(navManager.Uri);

if (uri.Fragment.Contains("type=recovery"))
{
    var fragment = uri.Fragment.TrimStart('#');
    var parameters = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(fragment);

    if (parameters.TryGetValue("access_token", out var token))
    {
        navManager.NavigateTo($"/reset-password?access_token={token}", forceLoad: true);
        return; 
    }
}
await host.RunAsync();

public class CustomJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

        if (jsonTypeInfo != null)
        {
            foreach (JsonPropertyInfo propertyInfo in jsonTypeInfo.Properties)
            {
                propertyInfo.IsRequired = false;
            }
        }

        return jsonTypeInfo;
    }
}