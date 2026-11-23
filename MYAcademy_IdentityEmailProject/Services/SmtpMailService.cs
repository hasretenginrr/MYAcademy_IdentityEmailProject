using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MYAcademy_IdentityEmailProject.Services;
using IMailService = MYAcademy_IdentityEmailProject.Services.IMailService;

public class SmtpMailService : IMailService
{
    private readonly SmtpSettings _s;

    public SmtpMailService(IOptions<SmtpSettings> options) => _s = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_s.FromName, _s.FromAddress));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_s.Host, _s.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_s.UserName, _s.Password);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
    }
}