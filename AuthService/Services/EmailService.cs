using Auth.Exceptions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;

namespace Auth.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var (from, host, port, username, password) = ResolveConfig(); // REsolve and validate first

            MimeMessage message;

            try
            {
                message = BuildMessage(from, to, subject, htmlBody);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid email address — from: {From}, to: {To}", from, to);
                throw new EmailAddressFormatException(to, ex);
            }

            try
            {
                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(username, password);
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent to {To}, subject: {Subject}", to, subject);
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError(ex, "SMTP command rejected while sending to {To}", to);
                throw new SmtpCommandFailedException(ex.Message, ex);
            }
            catch (SmtpProtocolException ex)
            {
                _logger.LogError(ex, "SMTP protocol error while sending to {To}", to);
                throw new SmtpProtocolFailedException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error delivering email to {To}", to);
                throw new EmailDeliveryException(to, ex);
            }
        }

        // Throws EmailConfigurationException early — before any I/O is attempted
        private (string from, string host, int port, string username, string password) ResolveConfig()
        {
            var from = _config["Email:From"];
            var host = _config["Email:Host"];
            var portStr = _config["Email:Port"];
            var username = _config["Email:Username"];
            var password = _config["Email:Password"];

            if (string.IsNullOrWhiteSpace(from)) throw new EmailConfigurationException("'Email:From' is missing");
            if (string.IsNullOrWhiteSpace(host)) throw new EmailConfigurationException("'Email:Host' is missing");
            if (string.IsNullOrWhiteSpace(portStr)) throw new EmailConfigurationException("'Email:Port' is missing");
            if (string.IsNullOrWhiteSpace(username)) throw new EmailConfigurationException("'Email:Username' is missing");
            if (string.IsNullOrWhiteSpace(password)) throw new EmailConfigurationException("'Email:Password' is missing");

            if (!int.TryParse(portStr, out var port))
                throw new EmailConfigurationException($"'Email:Port' value '{portStr}' is not a valid integer");

            return (from, host, port, username, password);
        }

        private static MimeMessage BuildMessage(string from, string to, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));   // throws FormatException on bad address
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };
            return message;
        }
    }
}