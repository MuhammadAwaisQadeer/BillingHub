using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Mail;

namespace Client_Invoice_System.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }


        public async Task<bool> SendInvoiceEmailAsync(string recipientEmail, byte[] invoicePdf, string fileName)
        {
            try
            {
                var emailSettings = _config.GetSection("EmailSettings");

                if (emailSettings == null)
                    throw new Exception("Email settings not found in configuration.");

                string senderEmail = emailSettings["SenderEmail"];
                string smtpServer = emailSettings["SmtpServer"];
                string smtpPort = emailSettings["SmtpPort"];
                string senderPassword = emailSettings["SenderPassword"];

                if (new[] { senderEmail, smtpServer, smtpPort, senderPassword }.Any(string.IsNullOrEmpty))
                    throw new Exception("Missing SMTP configuration details.");

                var message = new MimeMessage
                {
                    Subject = "Your Invoice",
                    Body = new BodyBuilder
                    {
                        TextBody = "Dear Client,\n\nPlease find your invoice attached.\n\nBest regards,\nAtrule Technologies",
                        Attachments = { { fileName, invoicePdf, new ContentType("application", "pdf") } }
                    }.ToMessageBody()
                };

                message.From.Add(new MailboxAddress("Atrule Technologies Invoicing Updates", senderEmail));
                message.To.Add(new MailboxAddress("", recipientEmail));

                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(smtpServer, int.Parse(smtpPort), MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, senderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"✅ Email sent successfully to {recipientEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Email Sending Error: {ex.Message}");
                return false;
            }
        }



    }
}
