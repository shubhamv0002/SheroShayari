using Microsoft.JSInterop;

namespace SheroShayari.Web.Services;

/// <summary>
/// Service for managing browser local storage in Blazor WebAssembly.
/// </summary>
public class LocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IJSRuntime jsRuntime, ILogger<LocalStorageService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Gets an item from local storage.
    /// </summary>
    public async Task<string?> GetItemAsync(string key)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting item '{key}' from local storage.");
            return null;
        }
    }

    /// <summary>
    /// Sets an item in local storage.
    /// </summary>
    public async Task SetItemAsync(string key, string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error setting item '{key}' in local storage.");
        }
    }

    /// <summary>
    /// Removes an item from local storage.
    /// </summary>
    public async Task RemoveItemAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error removing item '{key}' from local storage.");
        }
    }

    /// <summary>
    /// Clears all items from local storage.
    /// </summary>
    public async Task ClearAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.clear");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing local storage.");
        }
    }
}

