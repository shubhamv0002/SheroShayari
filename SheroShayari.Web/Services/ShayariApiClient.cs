using System.Net.Http.Json;
using System.Text.Json;
using SheroShayari.Web.Models;

namespace SheroShayari.Web.Services;

/// <summary>
/// Interface for Shayari API client communication.
/// </summary>
public interface IShayariApiClient
{
    /// <summary>
    /// Gets user's Shayaris (requires auth).
    /// </summary>
    /// <returns>List of user's Shayaris.</returns>
    Task<List<ShayariDto>> GetMyShayarisAsync();

    /// <summary>
    /// Generates a new Shayari (requires auth).
    /// </summary>
    /// <param name="request">Generation request with prompt, language, category, and optional context.</param>
    /// <returns>Generated Shayari or null on error.</returns>
    Task<GenerateShayariResponse?> GenerateAsync(GenerateShayariRequest request);
}

/// <summary>
/// HTTP client implementation for Shayari API communication.
/// </summary>
public class ShayariApiClient : IShayariApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShayariApiClient> _logger;
    private readonly IAuthService _authService;
    private const string ApiBaseRoute = "api";

    /// <summary>
    /// Initializes a new instance of the ShayariApiClient.
    /// </summary>
    public ShayariApiClient(HttpClient httpClient, ILogger<ShayariApiClient> logger, IAuthService authService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authService = authService;
    }

    /// <summary>
    /// Gets user's Shayaris (requires auth).
    /// </summary>
    public async Task<List<ShayariDto>> GetMyShayarisAsync()
    {
        try
        {
            if (!_authService.IsAuthenticated())
            {
                _logger.LogWarning("User not authenticated. Cannot fetch Shayaris.");
                return new List<ShayariDto>();
            }

            var token = _authService.GetToken();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseRoute}/shayari/my-shayaris");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"GetMyShayaris failed with status {response.StatusCode}");
                return new List<ShayariDto>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<ShayariDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? new List<ShayariDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user's Shayaris.");
            return new List<ShayariDto>();
        }
    }

    /// <summary>
    /// Generates a new Shayari (requires auth).
    /// </summary>
    public async Task<GenerateShayariResponse?> GenerateAsync(GenerateShayariRequest request)
    {
        try
        {
            if (!_authService.IsAuthenticated())
            {
                _logger.LogWarning("User not authenticated. Cannot generate Shayari.");
                return null;
            }

            var token = _authService.GetToken();
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseRoute}/shayari/generate");
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(httpRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Generation failed: {response.StatusCode} - {errorContent}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GenerateShayariResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during generation request.");
            return null;
        }
    }
}
