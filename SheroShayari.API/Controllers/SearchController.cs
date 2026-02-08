using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SheroShayari.API.Data;
using SheroShayari.API.Models;
using System.Security.Claims;

namespace SheroShayari.API.Controllers;

/// <summary>
/// Search controller for authenticated users to search their own Shayaris.
/// Requires authentication - only returns current user's Shayaris.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SearchController> _logger;

    public SearchController(AppDbContext context, ILogger<SearchController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user's ID from JWT claims.
    /// </summary>
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Gets all Shayaris for the current authenticated user.
    /// </summary>
    [HttpGet("public")]
    public async Task<ActionResult<IEnumerable<Shayari>>> GetUserShayaris(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token." });
            }

            var shayaris = await _context.Shayaris
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(shayaris);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user's Shayaris");
            return StatusCode(500, new { error = "An error occurred while fetching Shayaris." });
        }
    }

    /// <summary>
    /// Searches the current user's Shayaris by keyword (content, poet, or category).
    /// Only returns results matching the current user and search criteria.
    /// </summary>
    [HttpGet("public/search")]
    public async Task<ActionResult<IEnumerable<Shayari>>> SearchUserShayaris(
        [FromQuery] string? query = "",
        [FromQuery] string? language = "",
        [FromQuery] string? category = "",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token." });
            }

            var shayaris = _context.Shayaris
                .Where(s => s.UserId == userId); // Only current user's shayaris

            // Filter by query (search in content, poet, or meaning)
            if (!string.IsNullOrWhiteSpace(query))
            {
                shayaris = shayaris.Where(s =>
                    EF.Functions.Like(s.Content, $"%{query}%") ||
                    EF.Functions.Like(s.Poet, $"%{query}%") ||
                    EF.Functions.Like(s.Meaning, $"%{query}%"));
            }

            // Filter by language
            if (!string.IsNullOrWhiteSpace(language))
            {
                shayaris = shayaris.Where(s => s.Language == language);
            }

            // Filter by category
            if (!string.IsNullOrWhiteSpace(category))
            {
                shayaris = shayaris.Where(s => s.Category == category);
            }

            var results = await shayaris
                .OrderByDescending(s => s.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching user's Shayaris");
            return StatusCode(500, new { error = "An error occurred while searching." });
        }
    }

    /// <summary>
    /// Gets a specific Shayari by ID (only if it belongs to the current user).
    /// </summary>
    [HttpGet("public/{id}")]
    public async Task<ActionResult<Shayari>> GetUserShayariById(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token." });
            }

            var shayari = await _context.Shayaris
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (shayari == null)
            {
                return NotFound("Shayari not found.");
            }

            return Ok(shayari);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Shayari {Id}", id);
            return StatusCode(500, new { error = "An error occurred while fetching the Shayari." });
        }
    }

    /// <summary>
    /// Gets available languages for the current user's Shayaris.
    /// </summary>
    [HttpGet("filters/languages")]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableLanguages()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token." });
            }

            var languages = await _context.Shayaris
                .Where(s => s.UserId == userId)
                .Select(s => s.Language)
                .Distinct()
                .ToListAsync();

            return Ok(languages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching languages");
            return StatusCode(500, new { error = "An error occurred." });
        }
    }

    /// <summary>
    /// Gets available categories for the current user's Shayaris.
    /// </summary>
    [HttpGet("filters/categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableCategories()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token." });
            }

            var categories = await _context.Shayaris
                .Where(s => s.UserId == userId)
                .Select(s => s.Category)
                .Distinct()
                .ToListAsync();

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching categories");
            return StatusCode(500, new { error = "An error occurred." });
        }
    }
}
