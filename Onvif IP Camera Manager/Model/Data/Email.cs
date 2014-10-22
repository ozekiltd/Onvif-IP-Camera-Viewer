using System;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Onvif_IP_Camera_Manager.LOG;
using System.IO;

namespace Onvif_IP_Camera_Manager.Model.Data
{
    public class Email
    {
        public MailAddress FromAddress { get; set; }
        public string FromPassword { get; set; }

        public MailAddress ToAddress { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public int PortNum { get; set; }

        public SmtpClient Smtpclient { get; set; }

        public Email(string fromAddress, string fromPassword, string toAddress, string subject, string body, int port)
        {
            FromAddress = new MailAddress(fromAddress, "Ozeki");
            FromPassword = fromPassword;
            ToAddress = new MailAddress(toAddress, "Ozeki");
            Subject = subject;
            Body = body;
            PortNum = port;
        }

        public void SendEmail(string file)
        {
            var attachmentFilename = file;
            var attachment = new Attachment(attachmentFilename, MediaTypeNames.Application.Octet);
            if (attachmentFilename != null)
            {
                var disposition = attachment.ContentDisposition;
                disposition.CreationDate = File.GetCreationTime(attachmentFilename);
                disposition.ModificationDate = File.GetLastWriteTime(attachmentFilename);
                disposition.ReadDate = File.GetLastAccessTime(attachmentFilename);
                disposition.FileName = Path.GetFileName(attachmentFilename);
                disposition.Size = new FileInfo(attachmentFilename).Length;
                disposition.DispositionType = DispositionTypeNames.Attachment;
            }

            using (var message = new MailMessage(FromAddress, ToAddress)
            {
                Subject = Subject,
                Body = Body
            })
            {
                if (Smtpclient == null) return;
                try
                {
                    Smtpclient.Timeout = 300000000;
                    message.Attachments.Add(attachment);
                    Smtpclient.Send(message);
                    Log.Write(String.Format("Email has been sent from {0} to {1}", FromAddress.Address, ToAddress.Address)); 
                }
                catch (Exception exception)
                {
                    Log.Write("Error occured during e-mail sending: " + exception.Message);
                }
            }
        }

        public void CreateSmtpClient(string address)
        {
            Smtpclient = new SmtpClient
            {
                Host = address,
                Port = PortNum,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(FromAddress.Address, FromPassword)
            };
        }
    }
}
