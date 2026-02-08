using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheroShayari.API.Models;
using SheroShayari.API.Repositories;
using SheroShayari.API.Services;
using System.Security.Claims;

namespace SheroShayari.API.Controllers;

/// <summary>
/// API controller for user's private Shayari operations.
/// Requires authentication for generation and creation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShayariController : ControllerBase
{
    private readonly IShayariRepository _repository;
    private readonly IAiGenerationService _aiService;
    private readonly ILogger<ShayariController> _logger;

    /// <summary>
    /// Initializes a new instance of the ShayariController.
    /// </summary>
    /// <param name="repository">Shayari repository.</param>
    /// <param name="aiService">AI generation service.</param>
    /// <param name="logger">Logger instance.</param>
    public ShayariController(
        IShayariRepository repository,
        IAiGenerationService aiService,
        ILogger<ShayariController> logger)
    {
        _repository = repository;
        _aiService = aiService;
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
    /// Gets all private Shayaris for the current user.
    /// </summary>
    [HttpGet("my-shayaris")]
    public async Task<ActionResult<IEnumerable<Shayari>>> GetMyShayaris(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            _logger.LogInformation("Fetching Shayaris for user {UserId}", userId);
            var shayaris = await _repository.GetUserShayarisAsync(userId, cancellationToken);
            return Ok(shayaris);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user's Shayaris");
            return StatusCode(500, new { error = "An error occurred while fetching your Shayaris." });
        }
    }

    /// <summary>
    /// Gets a specific Shayari owned by the current user.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Shayari>> GetById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var shayari = await _repository.GetByIdAsync(id, cancellationToken);
            
            if (shayari == null)
            {
                return NotFound(new { error = "Shayari not found." });
            }

            // Verify ownership
            if (shayari.UserId != userId && !shayari.IsPublic)
            {
                return Forbid("You do not have permission to access this Shayari.");
            }

            return Ok(shayari);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching Shayari with ID {id}.");
            return StatusCode(500, new { error = "An error occurred while fetching the Shayari." });
        }
    }

    /// <summary>
    /// Generates a new Shayari using AI and saves it to the current user's collection.
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<GenerateShayariResponse>> Generate(
        [FromBody] GenerateShayariRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            if (string.IsNullOrWhiteSpace(request?.Prompt))
            {
                return BadRequest(new { error = "Prompt cannot be empty." });
            }

            if (string.IsNullOrWhiteSpace(request?.Language))
            {
                return BadRequest(new { error = "Language must be selected." });
            }

            if (string.IsNullOrWhiteSpace(request?.Category))
            {
                return BadRequest(new { error = "Category must be selected." });
            }

            // Validate context for "Other" category
            if (request.Category == "Other" && string.IsNullOrWhiteSpace(request?.AdditionalContext))
            {
                return BadRequest(new { error = "Context is required when category is 'Other'." });
            }

            _logger.LogInformation("User {UserId} generating Shayari with prompt: {Prompt}, Language: {Language}, Category: {Category}", 
                userId, request.Prompt, request.Language, request.Category);

            var result = await _aiService.GenerateShayariAsync(request.Prompt, cancellationToken);

            if (!result.IsSuccess)
            {
                return StatusCode(500, new { error = result.ErrorMessage ?? "Failed to generate Shayari." });
            }

            // Create a Shayari object with user association
            var shayari = new Shayari
            {
                Content = result.Content,
                Poet = "AI Generator",
                Language = request.Language,
                Category = request.Category,
                Meaning = result.Meaning,
                IsAiGenerated = true,
                IsPublic = false, // All user-generated shayaris are private
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Save to database
            try
            {
                await _repository.AddAsync(shayari, cancellationToken);
                _logger.LogInformation("Shayari generated and saved with ID: {Id} for user {UserId}", shayari.Id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving generated Shayari.");
                return StatusCode(500, new { error = "Failed to save generated Shayari." });
            }

            return Ok(new GenerateShayariResponse
            {
                Shayari = shayari,
                WasSaved = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during generation.");
            return StatusCode(500, new { error = "An error occurred while generating the Shayari." });
        }
    }

    /// <summary>
    /// Creates a new manual Shayari for the current user.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Shayari>> Create(
        [FromBody] Shayari shayari,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            if (string.IsNullOrWhiteSpace(shayari?.Content))
            {
                return BadRequest(new { error = "Shayari content cannot be empty." });
            }

            // Set user ID and timestamp
            shayari.UserId = userId;
            shayari.CreatedAt = DateTime.UtcNow;
            shayari.IsAiGenerated = false;

            _logger.LogInformation("User {UserId} creating a new Shayari.", userId);
            var created = await _repository.AddAsync(shayari, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Shayari.");
            return StatusCode(500, new { error = "An error occurred while creating the Shayari." });
        }
    }

    /// <summary>
    /// Updates an existing Shayari (only if owned by current user).
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Shayari>> Update(
        int id,
        [FromBody] Shayari shayari,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var existing = await _repository.GetByIdAsync(id, cancellationToken);
            if (existing == null)
            {
                return NotFound(new { error = "Shayari not found." });
            }

            // Verify ownership
            if (existing.UserId != userId)
            {
                return Forbid("You can only update your own Shayaris.");
            }

            shayari.Id = id;
            shayari.UserId = userId;
            shayari.CreatedAt = existing.CreatedAt;
            
            _logger.LogInformation("User {UserId} updating Shayari with ID {Id}.", userId, id);
            var updated = await _repository.UpdateAsync(shayari, cancellationToken);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating Shayari with ID {id}.");
            return StatusCode(500, new { error = "An error occurred while updating the Shayari." });
        }
    }

    /// <summary>
    /// Deletes a Shayari (only if owned by current user).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var shayari = await _repository.GetByIdAsync(id, cancellationToken);
            if (shayari == null)
            {
                return NotFound(new { error = "Shayari not found." });
            }

            // Verify ownership
            if (shayari.UserId != userId)
            {
                return Forbid("You can only delete your own Shayaris.");
            }

            _logger.LogInformation("User {UserId} deleting Shayari with ID {Id}.", userId, id);
            var success = await _repository.DeleteAsync(id, cancellationToken);

            if (!success)
            {
                return NotFound(new { error = "Shayari not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting Shayari with ID {id}.");
            return StatusCode(500, new { error = "An error occurred while deleting the Shayari." });
        }
    }

    /// <summary>
    /// Detects the language of the given text.
    /// </summary>
    private static string DetectLanguage(string text)
    {
        int hindiCount = 0;
        int urduCount = 0;

        foreach (char c in text)
        {
            if (c >= '\u0900' && c <= '\u097F') // Devanagari script (Hindi)
                hindiCount++;
            else if (c >= '\u0600' && c <= '\u06FF') // Arabic/Persian script (Urdu)
                urduCount++;
        }

        if (hindiCount > urduCount)
            return "Hindi";
        else if (urduCount > hindiCount)
            return "Urdu";
        else
            return "English";
    }

    /// <summary>
    /// Extracts category from the prompt.
    /// </summary>
    private static string ExtractCategory(string prompt)
    {
        var keywords = new Dictionary<string, string>
        {
            { "love", "Love" },
            { "pyaar", "Love" },
            { "ishq", "Love" },
            { "philosophy", "Philosophy" },
            { "nature", "Nature" },
            { "hope", "Hope" },
            { "sacrifice", "Sacrifice" },
            { "life", "Life" },
            { "death", "Death" }
        };

        var lowerPrompt = prompt.ToLower();
        foreach (var keyword in keywords)
        {
            if (lowerPrompt.Contains(keyword.Key))
                return keyword.Value;
        }

        return "General";
    }
}

/// <summary>
/// Request model for generating Shayari.
/// </summary>
public class GenerateShayariRequest
{
    /// <summary>
    /// The prompt for AI generation.
    /// </summary>
    public required string Prompt { get; set; }

    /// <summary>
    /// The language for the generated Shayari.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// The category for the generated Shayari.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Optional additional context or scenario for the generation.
    /// Required if Category is "Other".
    /// </summary>
    public string? AdditionalContext { get; set; }
}

/// <summary>
/// Response model for generation.
/// </summary>
public class GenerateShayariResponse
{
    /// <summary>
    /// The generated Shayari.
    /// </summary>
    public required Shayari Shayari { get; set; }

    /// <summary>
    /// Whether the Shayari was saved to database.
    /// </summary>
    public bool WasSaved { get; set; }
}
