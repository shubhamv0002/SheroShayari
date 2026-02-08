using System.Net.Http.Json;
using System.Text.Json;
using SheroShayari.Web.Models;

namespace SheroShayari.Web.Services;

/// <summary>
/// Interface for authentication service.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Initializes the service by loading cached auth state.
    /// Call once on app startup.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Registers a new user.
    /// </summary>
    Task<(bool Success, string Message, RegisterResponse? Data)> RegisterAsync(string email, string password, string confirmPassword, string fullName);

    /// <summary>
    /// Logs in a user.
    /// </summary>
    Task<(bool Success, string Message, LoginResponse? Data)> LoginAsync(string email, string password);

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Gets the current authentication token.
    /// </summary>
    string? GetToken();

    /// <summary>
    /// Gets the current user info.
    /// </summary>
    UserInfo? GetCurrentUser();

    /// <summary>
    /// Checks if user is authenticated.
    /// </summary>
    bool IsAuthenticated();

    /// <summary>
    /// Requests password reset email.
    /// </summary>
    Task<(bool Success, string Message)> ForgotPasswordAsync(string email);
}

/// <summary>
/// Implementation of authentication service.
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;
    private readonly LocalStorageService _localStorage;
    private const string AuthTokenKey = "auth_token";
    private const string UserInfoKey = "user_info";
    private const string ApiBaseRoute = "api/auth";

    // Cached values to avoid frequent localStorage access
    private string? _cachedToken;
    private UserInfo? _cachedUser;
    private bool _isInitialized = false;

    public AuthService(HttpClient httpClient, ILogger<AuthService> logger, LocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _logger = logger;
        _localStorage = localStorage;
    }

    /// <summary>
    /// Initializes the service by loading cached auth state from localStorage.
    /// Call this once on app startup.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            _cachedToken = await _localStorage.GetItemAsync(AuthTokenKey);
            var userJson = await _localStorage.GetItemAsync(UserInfoKey);
            
            if (!string.IsNullOrEmpty(userJson))
            {
                _cachedUser = JsonSerializer.Deserialize<UserInfo>(userJson);
            }

            if (!string.IsNullOrEmpty(_cachedToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _cachedToken);
            }

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auth service initialization.");
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    public async Task<(bool Success, string Message, RegisterResponse? Data)> RegisterAsync(
        string email, string password, string confirmPassword, string fullName)
    {
        try
        {
            var request = new RegisterRequest
            {
                Email = email,
                Password = password,
                ConfirmPassword = confirmPassword,
                FullName = fullName
            };

            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseRoute}/register", request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Registration failed: {response.StatusCode}");
                return (false, "Registration failed. Please try again.", null);
            }

            var result = JsonSerializer.Deserialize<RegisterResponse>(json);
            return (true, "Registration successful! Please log in.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration.");
            return (false, $"Registration error: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Logs in a user.
    /// </summary>
    public async Task<(bool Success, string Message, LoginResponse? Data)> LoginAsync(string email, string password)
    {
        try
        {
            var request = new LoginRequest
            {
                Email = email,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseRoute}/login", request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Login failed: {response.StatusCode}");
                return (false, "Invalid email or password.", null);
            }

            var result = JsonSerializer.Deserialize<LoginResponse>(json);
            if (result != null && !string.IsNullOrEmpty(result.AccessToken))
            {
                // Create a default UserInfo if the API didn't provide one
                if (result.User == null)
                {
                    result.User = new UserInfo { Email = email, FullName = email };
                }

                // Store in localStorage and cache
                await _localStorage.SetItemAsync(AuthTokenKey, result.AccessToken);
                await _localStorage.SetItemAsync(UserInfoKey, JsonSerializer.Serialize(result.User));

                // Update cache
                _cachedToken = result.AccessToken;
                _cachedUser = result.User;

                // Update default header for subsequent requests
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.AccessToken);

                return (true, "Login successful!", result);
            }

            return (false, "Login failed. No token received.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login.");
            return (false, $"Login error: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(AuthTokenKey);
        await _localStorage.RemoveItemAsync(UserInfoKey);
        _cachedToken = null;
        _cachedUser = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _logger.LogInformation("User logged out.");
    }

    /// <summary>
    /// Gets the current authentication token.
    /// </summary>
    public string? GetToken()
    {
        return _cachedToken;
    }

    /// <summary>
    /// Gets the current user info.
    /// </summary>
    public UserInfo? GetCurrentUser()
    {
        return _cachedUser;
    }

    /// <summary>
    /// Checks if user is authenticated.
    /// </summary>
    public bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(_cachedToken);
    }

    /// <summary>
    /// Requests password reset email.
    /// </summary>
    public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
    {
        try
        {
            var request = new ForgotPasswordRequest { Email = email };
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseRoute}/forgot-password", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Forgot password failed: {response.StatusCode}");
                return (false, "Failed to send reset email. Please try again.");
            }

            return (true, "Password reset email sent. Please check your inbox.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password.");
            return (false, $"Error: {ex.Message}");
        }
    }
}
