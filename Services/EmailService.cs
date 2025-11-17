using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Threading.Tasks;

public class EmailService
{
    private readonly string _smtpServer = "smtp-relay.brevo.com";
    private readonly int _smtpPort = 587;
    private readonly string _username = "974b97001@smtp-brevo.com";
    private readonly string _password = "b4RUah96vXgx8JLk"; // from Brevo
    private readonly string email = "lmukoya96@gmail.com";

    public async Task<bool> SendReportAsync(string toEmail, string subject, string body, byte[] reportBytes, string fileName)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Report System", email));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body
            };

            builder.Attachments.Add(fileName, reportBytes);
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_username, _password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true; // success
        }
        catch (Exception ex)
        {
            //log to file, DB, or console
            Console.WriteLine($"Email send failed: {ex.Message}");

            return false; // failure
        }
    }
}
