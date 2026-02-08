using Microsoft.AspNetCore.Identity.UI.Services;
using MailKit.Net.Smtp;
using MimeKit;

namespace SheroShayari.API.Services;

/// <summary>
/// Email sender service using MailKit for SMTP communication.
/// </summary>
public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Sends an email using the configured SMTP server.
    /// Throws exceptions if email sending fails - caller must handle appropriately.
    /// </summary>
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // Sanitize user-provided email before logging to prevent log forging
        var sanitizedEmail = (email ?? string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);

        try
        {
            var emailConfig = _configuration.GetSection("Email");
            var smtpServer = emailConfig["SmtpServer"];
            var smtpPort = int.Parse(emailConfig["SmtpPort"] ?? "587");
            var senderEmail = emailConfig["SenderEmail"] ?? "noreply@sheroshayari.com";
            var senderName = emailConfig["SenderName"] ?? "SheroShayari";
            var username = emailConfig["Username"];
            var password = emailConfig["Password"];

            // Validate SMTP configuration
            if (string.IsNullOrEmpty(smtpServer))
            {
                var errorMsg = "SMTP server not configured in appsettings.json";
                _logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            _logger.LogDebug("Preparing email message for {Email} via {SmtpServer}:{SmtpPort}", email, smtpServer, smtpPort);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                // ===================================================================
                // IMPORTANT: StartTls (Port 587) is required for Gmail SMTP
                // Port 465 uses ImplicitTls (deprecated), Port 587 uses StartTls
                // ===================================================================
                _logger.LogInformation("Connecting to SMTP server {SmtpServer}:{SmtpPort} with StartTls...", smtpServer, smtpPort);
                
                try
                {
                    await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    _logger.LogDebug("✅ Successfully connected to SMTP server");
                }
                catch (System.Net.Sockets.SocketException sockEx)
                {
                    _logger.LogError(sockEx, "❌ Socket connection failed to {SmtpServer}:{SmtpPort}. Error Code: {ErrorCode}", 
                        smtpServer, smtpPort, sockEx.SocketErrorCode);
                    throw; // Re-throw for caller to handle
                }

                // Authenticate if credentials provided
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    _logger.LogInformation("Authenticating with username: {Username}", username);
                    try
                    {
                        await client.AuthenticateAsync(username, password);
                        _logger.LogDebug("✅ SMTP authentication successful");
                    }
                    catch (Exception authEx)
                    {
                        _logger.LogError(authEx, "❌ SMTP authentication failed for {Username}. Check credentials.", username);
                        throw; // Re-throw for caller to handle
                    }
                }
                else
                {
                    _logger.LogWarning("No SMTP credentials configured. Attempting anonymous connection.");
                }

                // Send the actual email
                try
                {
                    _logger.LogInformation("Sending email to {Recipient} with subject '{Subject}'", sanitizedEmail, subject);
                    await client.SendAsync(message);
                    _logger.LogInformation("✅ Email sent successfully to {Recipient}", sanitizedEmail);
                }
                catch (Exception sendEx)
                {
                    _logger.LogError(sendEx, "❌ Failed to send email to {Recipient}. Error: {Message}", sanitizedEmail, sendEx.Message);
                    throw; // Re-throw for caller to handle
                }
                finally
                {
                    try
                    {
                        await client.DisconnectAsync(true);
                        _logger.LogDebug("✅ Disconnected from SMTP server");
                    }
                    catch (Exception discEx)
                    {
                        _logger.LogWarning(discEx, "Warning: Failed to gracefully disconnect from SMTP server");
                    }
                }
            }
        }
        catch (System.Net.Sockets.SocketException)
        {
            // Re-throw SocketException to be caught by caller
            throw;
        }
        catch (System.Net.Mail.SmtpException)
        {
            // Re-throw SmtpException to be caught by caller
            throw;
        }
        catch (Exception ex)
        {
            // Log and re-throw unexpected exceptions
            _logger.LogError(ex, "Unexpected error in SendEmailAsync: {ExceptionType}: {Message}", 
                ex.GetType().Name, ex.Message);
            throw;
        }
    }
}
