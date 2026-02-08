namespace SheroShayari.API.Models.Dtos;

/// <summary>
/// Request model for user registration.
/// </summary>
public class RegisterRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string ConfirmPassword { get; set; }
    public string? FullName { get; set; }
}

/// <summary>
/// Response model after successful registration.
/// </summary>
public class RegisterResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? UserId { get; set; }
}

/// <summary>
/// Request model for user login.
/// </summary>
public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

/// <summary>
/// Response model after successful login.
/// </summary>
public class LoginResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
}

/// <summary>
/// Request model for forgot password.
/// </summary>
public class ForgotPasswordRequest
{
    public required string Email { get; set; }
}

/// <summary>
/// Response model for forgot password request.
/// </summary>
public class ForgotPasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Request model for password reset.
/// </summary>
public class ResetPasswordRequest
{
    public required string Email { get; set; }
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmPassword { get; set; }
}

/// <summary>
/// Response model for password reset.
/// </summary>
public class ResetPasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
