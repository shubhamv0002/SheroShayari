namespace SheroShayari.Web.Models;

/// <summary>
/// Represents a Shayari for display in the frontend.
/// </summary>
public class ShayariDto
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public required string Poet { get; set; }
    public required string Language { get; set; }
    public required string Category { get; set; }
    public string? Meaning { get; set; }
    public bool IsAiGenerated { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UserId { get; set; }
    public bool IsPublic { get; set; }
}

/// <summary>
/// Request to generate a new Shayari.
/// </summary>
public class GenerateShayariRequest
{
    public required string Prompt { get; set; }
    public string? Language { get; set; }
    public string? Category { get; set; }
    public string? AdditionalContext { get; set; }
}

/// <summary>
/// Response from generation request.
/// </summary>
public class GenerateShayariResponse
{
    public required ShayariDto Shayari { get; set; }
    public bool WasSaved { get; set; }
}

/// <summary>
/// Constants for languages and categories.
/// </summary>
public static class ShayariConstants
{
    public static readonly string[] Languages = new[]
    {
        "Hindi",
        "English",
        "Urdu",
        "Hinglish",
        "Punjabi",
        "Marathi"
    };

    public static readonly string[] Categories = new[]
    {
        "Love",
        "Sad",
        "Motivation",
        "Attitude",
        "Friendship",
        "Life",
        "Birthday",
        "Funny",
        "Romantic",
        "Breakup",
        "Morning",
        "Night",
        "Alone",
        "Success",
        "Nature",
        "God",
        "Parents",
        "Festival",
        "Patriotic",
        "Other"
    };
}
