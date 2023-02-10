using System;
using System.Net.Mail;
using System.Text;


namespace IBIMTool.Authorization
{
    internal sealed class MailManager : IDisposable
    {
        public void SendDataToMail(string email, string data)
        {
            string mailAddress = "b.koljabai@taimas-group.kz";
            try
            {
                SmtpClient mySmtpClient = new SmtpClient("smtp.mail.ru");
                mySmtpClient.UseDefaultCredentials = true;
                mySmtpClient.EnableSsl = true;

                System.Net.NetworkCredential basicAuthenticationInfo = new System.Net.NetworkCredential
                (mailAddress, "1Je5jDjhGCP4WukqrDVJ");
                mySmtpClient.Credentials = basicAuthenticationInfo;

                MailAddress fromAddress = new MailAddress(mailAddress, "Taimas");
                MailAddress toAddress = new MailAddress(email);
                MailMessage message = new MailMessage(fromAddress, toAddress);

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Password is {data}");
                stringBuilder.AppendLine($"Email is {email}");

                message.Body = stringBuilder.ToString();
                message.Subject = "Client password";
                mySmtpClient.Send(message);
                stringBuilder.Clear();

            }
            catch (SmtpException ex)
            {
                throw new ApplicationException
                ("Error SmtpException: " + ex.Message);
            }

        }


        public void Dispose()
        {

        }
    }
}
