using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace KeyPresser
{
    public class SendGmail
    {
        public static string SenderAddress = "lukasz.piela1989App@gmail.com";
        public static string SenderPassword = "Qwedsa_123";
        public static string SmtpAddress = "smtp.gmail.com";
        public static int SmtpPort = 587;
        public static bool EnableSsl = true;

        //GMail wymaga ustawienia https://myaccount.google.com/lesssecureapps dla konta, którego będą wysyłane wiadomości

        public static bool SendMessage(string receiverAddress, string subject, string body)
        {
            try
            {
                using (MailMessage message = new MailMessage(SenderAddress, receiverAddress))
                {
                    message.Subject = subject;
                    message.Body = body;

                    using (SmtpClient smtpClient = new SmtpClient())
                    {
                        smtpClient.Host = SmtpAddress;
                        smtpClient.Port = SmtpPort;
                        smtpClient.EnableSsl = EnableSsl;
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Credentials = new System.Net.NetworkCredential(SenderAddress, SenderPassword);

                        smtpClient.Send(message);
                    };
                };
                return true;
            }
            catch(Exception ex)
            {
                Logger.Log(EventID.SendEmailException, ex);
                return false;
            }
        }
    }
}
