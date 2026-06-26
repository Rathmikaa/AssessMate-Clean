using AIAssessment.Application.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace AIAssessment.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink) =>
            SendAsync(toEmail, toName, "Reset your password", $"""
                <p>Hi {toName},</p>
                <p>We received a request to reset your AI Assessment account password.</p>
                <p><a href="{resetLink}">Click here to reset your password</a></p>
                <p>If you didn't request this, you can safely ignore this email.</p>
                """);

        public Task SendCandidateSetupInviteAsync(string toEmail, string toName, string setupLink, string? assessmentTitle)
        {
            var assessmentLine = assessmentTitle != null
                ? $"<p>You've been invited to take: <strong>{assessmentTitle}</strong>.</p>"
                : string.Empty;

            return SendAsync(toEmail, toName, "You're invited — set up your account", $"""
                <p>Hi {toName},</p>
                <p>An account has been created for you on the AI Assessment Platform.</p>
                {assessmentLine}
                <p><a href="{setupLink}">Click here to set your password and get started</a></p>
                """);
        }

        public Task SendAssessmentInviteAsync(string toEmail, string toName, string assessmentTitle, string assessmentLink) =>
            SendAsync(toEmail, toName, $"Invitation: {assessmentTitle}", $"""
                <p>Hi {toName},</p>
                <p>You've been invited to take: <strong>{assessmentTitle}</strong>.</p>
                <p><a href="{assessmentLink}">Click here to start the assessment</a></p>
                """);

        private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var fromEmail = _config["Email:FromEmail"] ?? throw new InvalidOperationException("Email:FromEmail not configured.");
            var fromName = _config["Email:FromName"] ?? "AI Assessment Platform";
            var host = _config["Email:Host"] ?? throw new InvalidOperationException("Email:Host not configured.");
            var port = int.TryParse(_config["Email:Port"], out var p) ? p : 587;
            var username = _config["Email:Username"];
            var password = _config["Email:Password"];
            var enableSsl = !bool.TryParse(_config["Email:EnableSsl"], out var ssl) || ssl;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(host, port, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(username))
                    await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true);
            }
        }
    }
}