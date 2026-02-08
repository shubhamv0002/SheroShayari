using System.Text.Json;
using System.Text.Json.Serialization;

namespace SheroShayari.API.Services;

/// <summary>
/// Request model for OpenRouter API chat completions.
/// </summary>
internal class OpenRouterRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("messages")]
    public required List<Message> Messages { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 2000;
}

/// <summary>
/// Message model for OpenRouter API.
/// </summary>
internal class Message
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

/// <summary>
/// Response model for OpenRouter API.
/// </summary>
internal class OpenRouterResponse
{
    [JsonPropertyName("choices")]
    public required List<Choice> Choices { get; set; }

    [JsonPropertyName("error")]
    public ErrorResponse? Error { get; set; }
}

/// <summary>
/// Choice model from OpenRouter response.
/// </summary>
internal class Choice
{
    [JsonPropertyName("message")]
    public required Message Message { get; set; }
}

/// <summary>
/// Error response model from OpenRouter.
/// </summary>
internal class ErrorResponse
{
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

/// <summary>
/// Service for interacting with OpenRouter API to generate Shayaris.
/// </summary>
public interface IAiGenerationService
{
    /// <summary>
    /// Generates a Shayari based on the user prompt.
    /// </summary>
    /// <param name="prompt">The user's request for a Shayari.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated Shayari content and meaning.</returns>
    Task<AiGenerationResult> GenerateShayariAsync(string prompt, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of AI-generated Shayari.
/// </summary>
public class AiGenerationResult
{
    /// <summary>
    /// The generated Shayari content.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// The meaning or explanation of the Shayari.
    /// </summary>
    public string? Meaning { get; set; }

    /// <summary>
    /// Indicates success or failure of generation.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Implementation of AI generation service using OpenRouter API.
/// </summary>
public class AiGenerationService : IAiGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiGenerationService> _logger;
    private const string OpenRouterEndpoint = "https://openrouter.ai/api/v1/chat/completions";
    private const string SystemPrompt = "You are a legendary poet capable of composing Sher-o-Shayari in Hindi, Urdu, and English. When a user asks for poetry, provide the Shayari followed by a brief meaning in English. Format the output elegantly. Separate the Shayari and meaning with '---MEANING---'";

    /// <summary>
    /// Initializes a new instance of the AiGenerationService.
    /// </summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public AiGenerationService(HttpClient httpClient, IConfiguration configuration, ILogger<AiGenerationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Generates a Shayari based on the user prompt.
    /// </summary>
    /// <param name="prompt">The user's request for a Shayari.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated Shayari content and meaning.</returns>
    public async Task<AiGenerationResult> GenerateShayariAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return new AiGenerationResult
                {
                    Content = string.Empty,
                    IsSuccess = false,
                    ErrorMessage = "Prompt cannot be empty."
                };
            }

            var apiKey = _configuration["OpenRouter:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("OpenRouter API key is not configured.");
                return new AiGenerationResult
                {
                    Content = string.Empty,
                    IsSuccess = false,
                    ErrorMessage = "AI service is not properly configured."
                };
            }

            var request = new OpenRouterRequest
            {
                Model = _configuration["OpenRouter:Model"] ?? "meta-llama/llama-2-70b-chat",
                Messages = new List<Message>
                {
                    new() { Role = "system", Content = SystemPrompt },
                    new() { Role = "user", Content = prompt }
                },
                Temperature = 0.7,
                MaxTokens = 1000
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://shayari.local");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "SheroShayari");

            var response = await _httpClient.PostAsync(OpenRouterEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError($"OpenRouter API error: {response.StatusCode} - {errorContent}");
                return new AiGenerationResult
                {
                    Content = string.Empty,
                    IsSuccess = false,
                    ErrorMessage = $"API request failed with status {response.StatusCode}."
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseBody);

            if (openRouterResponse?.Error != null)
            {
                _logger.LogError($"OpenRouter API returned error: {openRouterResponse.Error.Message}");
                return new AiGenerationResult
                {
                    Content = string.Empty,
                    IsSuccess = false,
                    ErrorMessage = openRouterResponse.Error.Message
                };
            }

            if (openRouterResponse?.Choices == null || openRouterResponse.Choices.Count == 0)
            {
                return new AiGenerationResult
                {
                    Content = string.Empty,
                    IsSuccess = false,
                    ErrorMessage = "No response received from AI service."
                };
            }

            var generatedText = openRouterResponse.Choices[0].Message.Content;

            // Parse the response to separate Shayari and meaning
            var parts = generatedText.Split("---MEANING---", StringSplitOptions.TrimEntries);
            var shayariContent = parts[0];
            var meaning = parts.Length > 1 ? parts[1] : null;

            return new AiGenerationResult
            {
                Content = shayariContent,
                Meaning = meaning,
                IsSuccess = true
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling OpenRouter API.");
            return new AiGenerationResult
            {
                Content = string.Empty,
                IsSuccess = false,
                ErrorMessage = "Failed to connect to AI service. Please try again later."
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error.");
            return new AiGenerationResult
            {
                Content = string.Empty,
                IsSuccess = false,
                ErrorMessage = "Invalid response from AI service."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in AI generation service.");
            return new AiGenerationResult
            {
                Content = string.Empty,
                IsSuccess = false,
                ErrorMessage = "An unexpected error occurred."
            };
        }
    }
}
