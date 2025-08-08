using JapaneseMealReservation.DataTransferObject;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace JapaneseMealReservation.Services
{
    public class MailService
    {
        private readonly IConfiguration _config;

        public MailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtp = _config.GetSection("EmailSettings");

            var host = smtp["Host"];
            var port = int.Parse(smtp["Port"]);
            var enableSsl = bool.Parse(smtp["EnableSSL"]);
            var username = smtp["Username"];
            var password = smtp["Password"];

            var smtpClient = new SmtpClient(host)
            {
                Port = port,
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            //Only set credentials if they are provided
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                smtpClient.Credentials = new NetworkCredential(username, password);
            }
            else
            {
                smtpClient.UseDefaultCredentials = true;
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtp["SenderEmail"], smtp["SenderName"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
