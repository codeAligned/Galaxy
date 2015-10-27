using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Galaxy.PricingService
{
    public static class Email
    {
        private static readonly ILog _logger;

        static Email()
        {
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public static void Send(string body, string filePath, string subject, string[] mailingList, string from, string password, string smtp, int port)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtpServer = new SmtpClient(smtp);
                mail.From = new MailAddress(from);

                foreach (var email in mailingList)
                {
                    mail.To.Add(email);
                }

                mail.Subject = subject;
                mail.Body = body;
                smtpServer.Port = port;
                smtpServer.Credentials = new System.Net.NetworkCredential(from, password);
                smtpServer.EnableSsl = true;
                Attachment attachment = new Attachment(filePath);
                mail.Attachments.Add(attachment);
                smtpServer.Send(mail);
            }
            catch (Exception e)
            {
                _logger.Error("Issue while sending DailyReport");
            }
        }
    }
}
