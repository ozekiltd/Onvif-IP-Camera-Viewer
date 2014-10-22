using System;
using System.IO;
using System.Net;
using Onvif_IP_Camera_Manager.LOG;

namespace Onvif_IP_Camera_Manager.Model.Data
{
    public class Ftp
    {
        public string Host { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
      
        /* Construct Object */

        public Ftp(string hostIp, string userName, string password)
        {
            Host = hostIp;
            User = userName;
            Pass = password;
        }

        public void Upload(string filePath, string remoteFile)
        {
            try
            {
                var ftpRequest = (FtpWebRequest)WebRequest.Create(Host + "/" + remoteFile);

                ftpRequest.KeepAlive = false;
                ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;

                ftpRequest.Credentials = new NetworkCredential(User, Pass);

                var fileContents = File.ReadAllBytes(filePath);

                ftpRequest.ContentLength = fileContents.Length;

                var ftpStream = ftpRequest.GetRequestStream();

                ftpStream.Write(fileContents, 0, fileContents.Length);
                ftpStream.Close();

                var response = (FtpWebResponse)ftpRequest.GetResponse();

                response.Close();

                Log.Write("File uploaded to FTP server");
            }
            catch (Exception e)
            {
                Log.Write("Error occured during file uploading to FTP server: " + e.Message);
            }
        }
    }
}
