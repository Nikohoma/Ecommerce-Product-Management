using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace Auth.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config) => _config = config;

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            try
            {
                var from = _config["Email:From"];
                var host = _config["Email:Host"];
                var portStr = _config["Email:Port"];
                var username = _config["Email:Username"];
                var password = _config["Email:Password"];

                // Validate config
                if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(host) ||
                    string.IsNullOrEmpty(portStr) || string.IsNullOrEmpty(username) ||
                    string.IsNullOrEmpty(password))
                {
                    throw new InvalidOperationException("Email configuration is missing.");
                }

                if (!int.TryParse(portStr, out var port))
                {
                    throw new FormatException("Invalid email port configuration.");
                }

                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(from));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(username, password);
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);
            }
            catch (FormatException ex)
            {
                throw new Exception("Invalid email address or configuration format.", ex);
            }
            catch (MailKit.Net.Smtp.SmtpCommandException ex)
            {
                throw new Exception($"SMTP command failed: {ex.Message}", ex);
            }
            catch (MailKit.Net.Smtp.SmtpProtocolException ex)
            {
                throw new Exception($"SMTP protocol error: {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception($"Email service configuration error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to send email.", ex);
            }
        }
    }
}
