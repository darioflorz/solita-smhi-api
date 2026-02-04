namespace SmhiApi.Common.Middleware;

public class ApiKeyAuthMiddleware
{
    private const string ApiKeyHeader = "X-API-Key";
    private readonly RequestDelegate _next;

    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration config)
    {
        // Skip authentication for OpenAPI and Scalar endpoints
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (path.StartsWith("/openapi") || path.StartsWith("/scalar"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API key is required. Provide it via X-API-Key header.");
            return;
        }

        var validKeys = config.GetSection("ApiKeys:ValidKeys").Get<string[]>() ?? [];
        if (!validKeys.Contains(providedKey.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Invalid API key.");
            return;
        }

        await _next(context);
    }
}

public static class ApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
