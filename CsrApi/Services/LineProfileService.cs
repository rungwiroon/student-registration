using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CsrApi.Services;

public interface ILineProfileService
{
    Task<string?> GetDisplayNameAsync(string lineUserId);
    Task<Dictionary<string, string>> GetDisplayNamesAsync(IEnumerable<string> lineUserIds);
}

public class LineProfileService : ILineProfileService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LineProfileService> _logger;
    private readonly string _channelAccessToken;

    public LineProfileService(IHttpClientFactory httpClientFactory, ILogger<LineProfileService> logger, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _channelAccessToken = config["Line:ChannelAccessToken"] ?? "";
    }

    public async Task<string?> GetDisplayNameAsync(string lineUserId)
    {
        if (string.IsNullOrWhiteSpace(_channelAccessToken) || string.IsNullOrWhiteSpace(lineUserId))
            return null;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _channelAccessToken);
            var response = await client.GetAsync($"https://api.line.me/v2/bot/profile/{lineUserId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch LINE profile for {UserId}: {Status}", lineUserId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("displayName").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching LINE profile for {UserId}", lineUserId);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetDisplayNamesAsync(IEnumerable<string> lineUserIds)
    {
        var result = new Dictionary<string, string>();
        foreach (var userId in lineUserIds)
        {
            var name = await GetDisplayNameAsync(userId);
            if (name != null)
                result[userId] = name;
        }
        return result;
    }
}
