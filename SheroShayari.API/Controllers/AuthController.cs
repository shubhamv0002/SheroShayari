using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using SheroShayari.API.Models;
using SheroShayari.API.Models.Dtos;

namespace SheroShayari.API.Controllers;

/// <summary>
/// Authentication and authorization controller for user management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (request.Password != request.ConfirmPassword)
            {
                return BadRequest(new RegisterResponse
                {
                    Success = false,
                    Message = "Passwords do not match."
                });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName ?? request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, errors);
                return BadRequest(new RegisterResponse
                {
                    Success = false,
                    Message = $"Registration failed: {errors}"
                });
            }

            // Send confirmation email
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action("ConfirmEmail", "Auth",
                new { userId = user.Id, code = code }, protocol: Request.Scheme);

            var confirmationEmailBody = $@"
                <h2>Welcome to SheroShayari!</h2>
                <p>Thank you for registering. Please confirm your email by clicking the link below:</p>
                <p><a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Confirm Email</a></p>
                <p>If you did not register, please ignore this email.</p>";

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email", confirmationEmailBody);

            _logger.LogInformation("User {Email} registered successfully", request.Email);

            return Ok(new RegisterResponse
            {
                Success = true,
                Message = "Registration successful. Please check your email to confirm your account.",
                UserId = user.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new RegisterResponse
            {
                Success = false,
                Message = "An error occurred during registration."
            });
        }
    }

    /// <summary>
    /// Confirms email address via token.
    /// </summary>
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<ActionResult> ConfirmEmail(string userId, string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                return BadRequest("Invalid email confirmation request.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                return Ok("Email confirmed successfully. You can now log in.");
            }

            return BadRequest("Email confirmation failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email");
            return StatusCode(500, "An error occurred during email confirmation.");
        }
    }

    /// <summary>
    /// Authenticates a user and returns JWT token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning("Failed login attempt for {Email}", request.Email);
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "Invalid email or password."
                });
            }

            // In production, uncomment the email confirmation check below
            // if (!user.EmailConfirmed)
            // {
            //     return BadRequest(new LoginResponse
            //     {
            //         Success = false,
            //         Message = "Please confirm your email before logging in."
            //     });
            // }

            var token = GenerateJwtToken(user);

            _logger.LogInformation("User {Email} logged in successfully", user.Email);

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "Login successful.",
                AccessToken = token,
                UserId = user.Id,
                Email = user.Email
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new LoginResponse
            {
                Success = false,
                Message = "An error occurred during login."
            });
        }
    }

    /// <summary>
    /// Initiates password reset flow by sending email with reset token.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.Email))
            {
                return BadRequest(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Email is required."
                });
            }

            var user = await _userManager.FindByEmailAsync(request.Email);

            // Always return success to prevent user enumeration, but only if email sent
            if (user == null)
            {
                _logger.LogWarning("Forgot password request for non-existent user: {Email}", request.Email);
                return Ok(new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "If an account with that email exists, you will receive a password reset email shortly."
                });
            }

            try
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:5160";
            var callbackUrl = $"{frontendUrl}/reset-password?email={System.Web.HttpUtility.UrlEncode(user.Email)}&code={System.Web.HttpUtility.UrlEncode(code)}";
                var resetEmailBody = $@"
                    <h2>Password Reset Request</h2>
                    <p>Hello {System.Web.HttpUtility.HtmlEncode(user.UserName)},</p>
                    <p>We received a request to reset your password. Click the link below to create a new password:</p>
                    <p><a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Reset Password</a></p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you did not request a password reset, please ignore this email.</p>
                    <p>Best regards,<br/>SheroShayari Team</p>";

                // ============================================================
                // STRICT EMAIL VALIDATION - Fail fast if email service is down
                // ============================================================
                try
                {
                    _logger.LogInformation("Attempting to send password reset email to {Email}", request.Email);
                    await _emailSender.SendEmailAsync(user.Email, "Reset your password - SheroShayari", resetEmailBody);
                    _logger.LogInformation("✅ SUCCESS: Password reset email sent to {Email}", request.Email);

                    return Ok(new ForgotPasswordResponse
                    {
                        Success = true,
                        Message = "Password reset email sent successfully! Please check your inbox and spam folder within the next few minutes."
                    });
                }
                catch (System.Net.Sockets.SocketException sockEx)
                {
                    // SMTP server connection failed - infrastructure issue
                    _logger.LogError(sockEx, "❌ SOCKET ERROR: Cannot connect to SMTP server for email to {Email}. Details: {Message}", 
                        request.Email, sockEx.Message);
                    
                    return StatusCode(503, new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "Email service is currently unavailable. Please try again in a few minutes."
                    });
                }
                catch (System.Net.Mail.SmtpException smtpEx)
                {
                    // SMTP authentication or command failed
                    _logger.LogError(smtpEx, "❌ SMTP ERROR: Failed to send email to {Email}. Check SMTP credentials and configuration. Details: {Message}", 
                        request.Email, smtpEx.Message);
                    
                    return StatusCode(503, new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "Email service is temporarily unavailable. Please try again later."
                    });
                }
                catch (Exception emailEx)
                {
                    // Unexpected error during email sending
                    _logger.LogError(emailEx, "❌ UNEXPECTED ERROR: Failed to send password reset email to {Email}. Exception: {ExceptionType}: {Message}", 
                        request.Email, emailEx.GetType().Name, emailEx.Message);
                    
                    return StatusCode(500, new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "An unexpected error occurred while sending the email. Please try again later."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset token for {Email}", request.Email);
                
                return StatusCode(500, new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "An error occurred while processing your request. Please try again later."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in forgot password flow");
            return StatusCode(500, new ForgotPasswordResponse
            {
                Success = false,
                Message = "An error occurred. Please try again later."
            });
        }
    }

    /// <summary>
    /// Resets user password using token from email.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new ResetPasswordResponse
                {
                    Success = false,
                    Message = "Passwords do not match."
                });
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new ResetPasswordResponse
                {
                    Success = false,
                    Message = "Invalid email address."
                });
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successful for {Email}", request.Email);
                return Ok(new ResetPasswordResponse
                {
                    Success = true,
                    Message = "Your password has been reset successfully. You can now log in with your new password."
                });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new ResetPasswordResponse
            {
                Success = false,
                Message = $"Password reset failed: {errors}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new ResetPasswordResponse
            {
                Success = false,
                Message = "An error occurred during password reset."
            });
        }
    }

    /// <summary>
    /// Validates a password reset token without displaying a form.
    /// Called by frontend to check if token is still valid.
    /// </summary>
    [HttpGet("validate-reset-token")]
    [AllowAnonymous]
    public async Task<ActionResult> ValidateResetToken([FromQuery] string email, [FromQuery] string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { success = false, message = "Missing email or code." });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Validate reset token: User not found for email {Email}", email);
                return BadRequest(new { success = false, message = "Invalid email address." });
            }

            // Decode the token - it comes URL encoded from the email link
            var decodedCode = System.Net.WebUtility.UrlDecode(code);
            
            _logger.LogInformation("Attempting to validate reset token for {Email}. Original length: {OriginalLength}, Decoded length: {DecodedLength}", 
                email, code.Length, decodedCode.Length);

            // Verify the token
            var result = await _userManager.VerifyUserTokenAsync(user, 
                _userManager.Options.Tokens.PasswordResetTokenProvider, 
                "ResetPassword", decodedCode);

            if (!result)
            {
                _logger.LogWarning("Invalid or expired password reset token for {Email}. This could mean: 1) Token already used, 2) Token expired (24 hours), 3) Token corrupted", email);
                return BadRequest(new { success = false, message = "Password reset link is invalid or has expired. Please request a new one." });
            }

            _logger.LogInformation("✅ Valid password reset token verified for {Email}", email);
            return Ok(new { success = true, message = "Token is valid." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password reset token for {Email}: {ExceptionType}: {Message}", 
                email, ex.GetType().Name, ex.Message);
            return StatusCode(500, new { success = false, message = "Error validating token." });
        }
    }

    /// <summary>
    /// GET endpoint to validate password reset token and display HTML form.
    /// Called from the email link clicked by the user.
    /// </summary>
    [HttpGet("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ResetPasswordGet([FromQuery] string email, [FromQuery] string code)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                var errorHtml = GetErrorHtml("Invalid password reset link. Missing email or code. Please request a new password reset.");
                return Content(errorHtml, "text/html; charset=utf-8");
            }

            // Find user
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var errorHtml = GetErrorHtml("Invalid email address. Please check and try again.");
                return Content(errorHtml, "text/html; charset=utf-8");
            }

            // ============================================================
            // Validate the password reset token
            // ============================================================
            // Decode the token (it's URL-encoded in the link)
            var decodedCode = System.Net.WebUtility.UrlDecode(code);
            
            try
            {
                // Try to create a test password to validate the token
                // This doesn't actually change the password, just validates the token
                var result = await _userManager.VerifyUserTokenAsync(user, 
                    _userManager.Options.Tokens.PasswordResetTokenProvider, 
                    "ResetPassword", decodedCode);

                if (!result)
                {
                    _logger.LogWarning("Invalid or expired password reset token for {Email}", email);
                    var errorHtml = GetErrorHtml("Password reset link is invalid or has expired. Please request a new password reset.");
                    return Content(errorHtml, "text/html; charset=utf-8");
                }

                // ============================================================
                // Token is valid - return HTML form for password reset
                // ============================================================
                _logger.LogInformation("Valid password reset token provided for {Email}", email);

                var htmlForm = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Reset Password - SheroShayari</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }}
        .container {{ 
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
            width: 100%;
            max-width: 400px;
        }}
        h1 {{ 
            text-align: center;
            color: #333;
            margin-bottom: 10px;
            font-size: 28px;
        }}
        .subtitle {{
            text-align: center;
            color: #666;
            margin-bottom: 30px;
            font-size: 14px;
        }}
        .form-group {{ 
            margin-bottom: 20px;
        }}
        label {{ 
            display: block;
            margin-bottom: 8px;
            color: #333;
            font-weight: 500;
            font-size: 14px;
        }}
        input, textarea {{ 
            width: 100%;
            padding: 12px;
            border: 1px solid #ddd;
            border-radius: 5px;
            font-size: 14px;
            font-family: inherit;
        }}
        input:focus, textarea:focus {{
            outline: none;
            border-color: #667eea;
            box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
        }}
        button {{ 
            width: 100%;
            padding: 12px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            border: none;
            border-radius: 5px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            margin-top: 10px;
        }}
        button:hover {{ 
            opacity: 0.9;
        }}
        button:active {{
            transform: scale(0.98);
        }}
        .message {{ 
            margin-top: 20px;
            padding: 12px;
            border-radius: 5px;
            text-align: center;
            font-size: 14px;
        }}
        .success {{
            background: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }}
        .error {{
            background: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }}
        .info {{
            background: #d1ecf1;
            color: #0c5460;
            border: 1px solid #bee5eb;
        }}
        #loadingSpinner {{
            display: none;
            text-align: center;
        }}
        .spinner {{
            border: 4px solid #f3f3f3;
            border-top: 4px solid #667eea;
            border-radius: 50%;
            width: 30px;
            height: 30px;
            animation: spin 1s linear infinite;
            margin: 0 auto;
        }}
        @keyframes spin {{
            0% {{ transform: rotate(0deg); }}
            100% {{ transform: rotate(360deg); }}
        }}
        .password-requirement {{
            font-size: 12px;
            color: #666;
            margin-top: 8px;
            line-height: 1.6;
        }}
        .requirement {{
            color: #999;
        }}
        .requirement.met {{
            color: #28a745;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>🔐 Reset Password</h1>
        <p class=""subtitle"">Create a new password for your SheroShayari account</p>

        <form id=""resetForm"">
            <input type=""hidden"" id=""email"" name=""email"" value=""{HtmlEncoder.Default.Encode(email)}"">
            <input type=""hidden"" id=""code"" name=""code"" value=""{HtmlEncoder.Default.Encode(decodedCode)}"">

            <div class=""form-group"">
                <label for=""newPassword"">New Password</label>
                <input type=""password"" id=""newPassword"" name=""newPassword"" placeholder=""Enter your new password"" required minlength=""6"">
                <div class=""password-requirement"">
                    Password must be at least 6 characters long
                </div>
            </div>

            <div class=""form-group"">
                <label for=""confirmPassword"">Confirm Password</label>
                <input type=""password"" id=""confirmPassword"" name=""confirmPassword"" placeholder=""Confirm your new password"" required minlength=""6"">
            </div>

            <button type=""submit"">Reset Password</button>

            <div id=""message"" class=""message"" style=""display: none;""></div>
            <div id=""loadingSpinner"">
                <div class=""spinner""></div>
                <p style=""margin-top: 10px; color: #667eea;"">Resetting password...</p>
            </div>
        </form>

        <div style=""margin-top: 20px; text-align: center; color: #666; font-size: 12px;"">
            <p>This link expires in 24 hours</p>
        </div>
    </div>

    <script>
        const form = document.getElementById('resetForm');
        const messageDiv = document.getElementById('message');
        const loadingSpinner = document.getElementById('loadingSpinner');

        form.addEventListener('submit', async (e) => {{
            e.preventDefault();

            const newPassword = document.getElementById('newPassword').value;
            const confirmPassword = document.getElementById('confirmPassword').value;
            const email = document.getElementById('email').value;
            const code = document.getElementById('code').value;

            // Validate
            if (newPassword !== confirmPassword) {{
                showMessage('Passwords do not match!', 'error');
                return;
            }}

            if (newPassword.length < 6) {{
                showMessage('Password must be at least 6 characters long!', 'error');
                return;
            }}

            // Show loading
            loadingSpinner.style.display = 'block';
            messageDiv.style.display = 'none';

            try {{
                const response = await fetch('/api/auth/reset-password', {{
                    method: 'POST',
                    headers: {{
                        'Content-Type': 'application/json',
                    }},
                    body: JSON.stringify({{
                        email: email,
                        token: code,
                        newPassword: newPassword,
                        confirmPassword: confirmPassword
                    }})
                }});

                const data = await response.json();

                loadingSpinner.style.display = 'none';

                if (response.ok && data.success) {{
                    showMessage('✅ ' + data.message, 'success');
                    form.style.display = 'none';
                    setTimeout(() => {{
                        window.location.href = '/login';
                    }}, 3000);
                }} else {{
                    showMessage('❌ ' + (data.message || 'Failed to reset password'), 'error');
                }}
            }} catch (error) {{
                loadingSpinner.style.display = 'none';
                showMessage('❌ An error occurred: ' + error.message, 'error');
            }}
        }});

        function showMessage(text, type) {{
            messageDiv.textContent = text;
            messageDiv.className = 'message ' + type;
            messageDiv.style.display = 'block';
        }}
    </script>
</body>
</html>";

                return Content(htmlForm, "text/html; charset=utf-8");
            }
            catch (Exception tokenEx)
            {
                _logger.LogError(tokenEx, "Error validating password reset token for {Email}", email);
                var errorHtml = GetErrorHtml("An error occurred while validating the reset link. Please request a new password reset.");
                return Content(errorHtml, "text/html; charset=utf-8");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in password reset GET endpoint for {Email}", email);
            var errorHtml = GetErrorHtml("An error occurred while processing your password reset request. Please try again.");
            return Content(errorHtml, "text/html; charset=utf-8");
        }
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        try
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out");
            return Ok(new { Success = true, Message = "Logged out successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { Success = false, Message = "An error occurred during logout." });
        }
    }

    /// <summary>
    /// Generates a JWT token for the authenticated user.
    /// </summary>
    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? "SheroShayari";
        var audience = jwtSettings["Audience"] ?? "SheroShayariUsers";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim("FullName", user.FullName ?? "")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates an HTML error page for password reset errors.
    /// </summary>
    private string GetErrorHtml(string errorMessage)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Reset Error - SheroShayari</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }}
        .container {{ 
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
            width: 100%;
            max-width: 500px;
            text-align: center;
        }}
        .icon {{
            font-size: 48px;
            margin-bottom: 15px;
        }}
        h1 {{ 
            color: #d9534f;
            margin-bottom: 10px;
            font-size: 28px;
        }}
        .error-message {{
            color: #666;
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 30px;
        }}
        .button-group {{
            display: flex;
            gap: 10px;
            justify-content: center;
            flex-wrap: wrap;
        }}
        a, button {{ 
            padding: 12px 24px;
            border-radius: 5px;
            font-size: 14px;
            font-weight: 600;
            text-decoration: none;
            cursor: pointer;
            border: none;
            transition: all 0.3s ease;
        }}
        .btn-primary {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
        }}
        .btn-primary:hover {{
            opacity: 0.9;
            transform: translateY(-2px);
        }}
        .btn-secondary {{
            background: #f0f0f0;
            color: #333;
            border: 1px solid #ddd;
        }}
        .btn-secondary:hover {{
            background: #e8e8e8;
        }}
        .info-box {{
            background: #f9f9f9;
            padding: 15px;
            border-left: 4px solid #667eea;
            margin-top: 25px;
            text-align: left;
            border-radius: 5px;
        }}
        .info-box h3 {{
            color: #667eea;
            font-size: 14px;
            margin-bottom: 8px;
        }}
        .info-box p {{
            color: #666;
            font-size: 13px;
            line-height: 1.6;
            margin: 5px 0;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""icon"">❌</div>
        <h1>Password Reset Failed</h1>
        <p class=""error-message"">{HtmlEncoder.Default.Encode(errorMessage)}</p>

        <div class=""button-group"">
            <a href=""/forgot-password"" class=""btn-primary"">Request New Link</a>
            <a href=""/"" class=""btn-secondary"">Back to Home</a>
        </div>

        <div class=""info-box"">
            <h3>ℹ️ What to do next:</h3>
            <p><strong>1.</strong> Go to the login page</p>
            <p><strong>2.</strong> Click the ""Forgot Password"" button</p>
            <p><strong>3.</strong> Enter your email address</p>
            <p><strong>4.</strong> Check your email for a new password reset link</p>
            <p><strong>5.</strong> Password reset links expire after 24 hours</p>
        </div>
    </div>
</body>
</html>";
    }
}
