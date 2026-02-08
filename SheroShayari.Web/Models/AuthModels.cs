namespace SheroShayari.Web.Models;

/// <summary>
/// Request model for user registration.
/// </summary>
public class RegisterRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string ConfirmPassword { get; set; }
    public required string FullName { get; set; }
}

/// <summary>
/// Response model for user registration.
/// </summary>
public class RegisterResponse
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Request model for user login.
/// </summary>
public class LoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

/// <summary>
/// Response model for user login.
/// </summary>
public class LoginResponse
{
    public string? AccessToken { get; set; }
    public UserInfo? User { get; set; }
}

/// <summary>
/// User information contained in login response.
/// </summary>
public class UserInfo
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
}

/// <summary>
/// Request model for forgot password.
/// </summary>
public class ForgotPasswordRequest
{
    public required string Email { get; set; }
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
public class ResetResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Represents authentication state.
/// </summary>
public class AuthState
{
    public bool IsAuthenticated { get; set; }
    public string? Token { get; set; }
    public UserInfo? User { get; set; }
}
