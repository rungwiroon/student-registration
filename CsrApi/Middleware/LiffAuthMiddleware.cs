using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CsrApi.Middleware;

public class LiffAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LiffAuthMiddleware> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _liffChannelId;
    private readonly bool _useMockAuth;
    private readonly string _mockUserId;

    public LiffAuthMiddleware(RequestDelegate next, ILogger<LiffAuthMiddleware> logger, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _liffChannelId = config["Line:LiffChannelId"] ?? "";
        _useMockAuth = config.GetValue<bool>("Line:UseMockAuth", false);
        _mockUserId = config["Line:MockUserId"] ?? "mock-line-user-id";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth for Swagger or specific endpoints if needed
        var path = context.Request.Path.Value;
        if (path != null && (path.StartsWith("/swagger") || path.StartsWith("/health") || path.StartsWith("/api/backoffice/dev")))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing Authorization Header");
            return;
        }

        var token = authHeader.ToString().Replace("Bearer ", "").Trim();

        if (_useMockAuth)
        {
            _logger.LogInformation("Using mock authentication for token: {Token}", token);
            context.Items["LineUserId"] = _mockUserId;
            await _next(context);
            return;
        }

        bool isValid = await VerifyLineTokenAsync(token);
        if (!isValid)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid LINE access token");
            return;
        }

        var userId = await GetLineUserIdAsync(token);
        if (userId != null)
        {
            context.Items["LineUserId"] = userId;
        }

        await _next(context);
    }

    private async Task<bool> VerifyLineTokenAsync(string token)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://api.line.me/oauth2/v2.1/verify?access_token={token}");
            
            if (!response.IsSuccessStatusCode)
                return false;

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var clientId = doc.RootElement.GetProperty("client_id").GetString();

            return clientId == _liffChannelId;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> GetLineUserIdAsync(string token)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("https://api.line.me/v2/profile");
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("userId").GetString();
        }
        catch
        {
            return null;
        }
    }
}
