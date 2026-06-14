using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ECommerceFinalProject.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task GuiMaXacThucAsync(string email, string maXacThuc, string hoTen)
    {
        var smtp = _configuration.GetSection("SmtpSettings");

        var server = smtp["Server"];
        var portRaw = smtp["Port"];
        var username = smtp["Username"];
        var password = smtp["Password"];
        var senderEmail = smtp["SenderEmail"];
        var senderName = smtp["SenderName"] ?? "ECommerce Shop";

        _logger.LogInformation("[EmailService] Sending OTP to {Email} via {Server}:{Port} as {Sender}",
            email, server, portRaw, senderEmail);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(senderEmail))
        {
            _logger.LogError("[EmailService] SmtpSettings is missing Username/Password/SenderEmail. Check 'dotnet user-secrets list'.");
            throw new InvalidOperationException("SMTP chua duoc cau hinh. Hay chay 'dotnet user-secrets set' cho Username/Password/SenderEmail.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(new MailboxAddress(hoTen, email));
        message.Subject = "[ECommerce] Ma xac thuc dang nhap";

        var builder = new BodyBuilder
        {
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                    <div style='background: #1a1a1a; color: white; padding: 16px; text-align: center; border-radius: 8px 8px 0 0;'>
                        <h2 style='margin:0;'>ECommerce Shop</h2>
                    </div>
                    <div style='padding: 24px; text-align: center;'>
                        <p>Xin chao <strong>{hoTen}</strong>,</p>
                        <p>Ma xac thuc cua ban la:</p>
                        <div style='background: #f5f5f5; padding: 16px; margin: 16px 0; border-radius: 8px;'>
                            <span style='font-size: 28px; font-weight: bold; letter-spacing: 8px; color: #1a1a1a;'>{maXacThuc}</span>
                        </div>
                        <p style='color: #666; font-size: 14px;'>Ma co hieu luc trong <strong>5 phut</strong>.</p>
                        <p style='color: #999; font-size: 12px;'>Neu ban khong yeu cau dang nhap, vui long bo qua email nay.</p>
                    </div>
                </div>"
        };

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(
                server,
                int.Parse(portRaw ?? "587"),
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(username, password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("[EmailService] OTP sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmailService] Failed to send OTP to {Email}. Type: {Type}, Message: {Message}",
                email, ex.GetType().FullName, ex.Message);
            throw;
        }
    }
}
