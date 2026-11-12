using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Alcedo.Frontend.Core.HttpClient;

public record AlcedoBackendApplicationInfo
{
    public string CoreVersion { get; init; } = null!;
    
    public string AppVersion { get; init; } = null!;
    
    public IReadOnlyDictionary<string, string>  ModuleVersions { get; init; } = null!;
}

public class AlcedoHealthClient
{
    private readonly System.Net.Http.HttpClient _httpClient;
    private readonly ILogger<AlcedoHealthClient>? _logger;

    public AlcedoHealthClient(System.Net.Http.HttpClient httpClient, ILogger<AlcedoHealthClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get /health/info
    /// </summary>
    public async Task<AlcedoBackendApplicationInfo?> GetSystemInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AlcedoBackendApplicationInfo>("/health/info", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to retrieve backend system info from /info endpoint.");
            return null;
        }
    }

    /// <summary>
    /// Get /health/ready
    /// </summary>
    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health/ready", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}