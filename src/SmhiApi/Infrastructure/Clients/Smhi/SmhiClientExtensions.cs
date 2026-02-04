using System.Net;
using Microsoft.Extensions.Http.Resilience;

namespace SmhiApi.Infrastructure.Clients.Smhi;

/// <summary>
/// Extension methods for registering SMHI client services
/// </summary>
public static class SmhiClientExtensions
{
    /// <summary>
    /// Adds SMHI HTTP client with resilience policies to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="config">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSmhiClient(this IServiceCollection services, IConfiguration config)
    {
        var baseUrl = config.GetValue<string>("Smhi:BaseUrl") 
            ?? "https://opendata-download-metobs.smhi.se/";

        services.AddHttpClient<ISmhiClient, SmhiClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        .AddStandardResilienceHandler();

        return services;
    }
}
