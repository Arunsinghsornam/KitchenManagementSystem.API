using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KitchenManagementSystem.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, string? replyTo = null, string? fromDisplayName = null)
    {
        var enableSmtp = _config.GetValue<bool>("Smtp:EnableSmtp");
        
        _logger.LogInformation("==========================================");
        _logger.LogInformation("SENDING EMAIL:");
        _logger.LogInformation("To: {To}", to);
        _logger.LogInformation("Subject: {Subject}", subject);
        _logger.LogInformation("Body: {Body}", body);
        if (!string.IsNullOrEmpty(replyTo)) _logger.LogInformation("Reply-To: {ReplyTo}", replyTo);
        if (!string.IsNullOrEmpty(fromDisplayName)) _logger.LogInformation("From Display Name: {FromDisplayName}", fromDisplayName);
        _logger.LogInformation("==========================================");

        // Always save a copy to the emails/ folder in the project root for local testing/verification
        try
        {
            var emailsFolder = Path.Combine(Directory.GetCurrentDirectory(), "emails");
            if (!Directory.Exists(emailsFolder))
            {
                Directory.CreateDirectory(emailsFolder);
            }
            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}.html";
            var filePath = Path.Combine(emailsFolder, fileName);
            var replyToHeader = !string.IsNullOrEmpty(replyTo) ? $"<!-- REPLY-TO: {replyTo} -->\n" : "";
            var fromDisplayNameHeader = !string.IsNullOrEmpty(fromDisplayName) ? $"<!-- FROM-NAME: {fromDisplayName} -->\n" : "";
            var fileContent = $"<!-- TO: {to} -->\n<!-- SUBJECT: {subject} -->\n{replyToHeader}{fromDisplayNameHeader}<html><body>{body}</body></html>";
            await File.WriteAllTextAsync(filePath, fileContent);
            _logger.LogInformation("Email simulation copy saved to: {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save simulated email copy to file.");
        }

        if (enableSmtp)
        {
            try
            {
                var host = _config["Smtp:Host"];
                var port = _config.GetValue<int>("Smtp:Port");
                var username = _config["Smtp:Username"];
                var password = _config["Smtp:Password"];
                var fromEmail = _config["Smtp:FromEmail"] ?? "noreply@kitchenportal.com";
                var fromName = fromDisplayName ?? _config["Smtp:FromName"] ?? "Kitchen Operations Portal";

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail, fromName);
                message.To.Add(new MailAddress(to));
                
                if (!string.IsNullOrEmpty(replyTo))
                {
                    message.ReplyToList.Add(new MailAddress(replyTo));
                }

                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(host, port);
                if (!string.IsNullOrEmpty(username))
                {
                    client.Credentials = new NetworkCredential(username, password);
                    client.EnableSsl = true;
                }
                
                await client.SendMailAsync(message);
                _logger.LogInformation("Real email sent successfully via SMTP to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send real email via SMTP to {To}", to);
            }
        }
    }
}
