using System;
using System.Diagnostics;
using System.Net.Mail;
using System.Text;


namespace IBIMTool.Authorization
{
    internal sealed class MailManager : IDisposable
    {
        public void SendDataToMail(string email, string data, out string msg)
        {
            msg = null;
            string mailAddress = "info@taimas-group.kz";
            try
            {
                
                SmtpClient mySmtpClient = new SmtpClient("smtp.mail.ru");
                mySmtpClient.UseDefaultCredentials = true;
                mySmtpClient.EnableSsl = true;

                System.Net.NetworkCredential basicAuthenticationInfo = new System.Net.NetworkCredential
                (mailAddress, "EvaV8dm0XYuGh7LN5J7u");
                mySmtpClient.Credentials = basicAuthenticationInfo;

                MailAddress fromAddress = new MailAddress(mailAddress, "TAIMAS COMPANY");
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
                Debug.Print("Error: " + ex.Message);
                msg = ex.Message;
            }

        }


        public void Dispose()
        {

        }
    }
}
