using System;
using System.IO;
using System.Net;


namespace TraspasoDeCartera.Services
{
    public class FtpManager
    {
        private readonly string _ftpServer;
        private readonly string _ftpUsername;
        private readonly string _ftpPassword;

        public FtpManager(string ftpServer, string ftpUsername, string ftpPassword)
        {
            _ftpServer = ftpServer;
            _ftpUsername = ftpUsername;
            _ftpPassword = ftpPassword;
        }

        public async Task UploadDataAsync(byte[] data, string remoteFileName, string ftpDirectory = "")
        {
            try
            {
                string ftpFilePath = $"ftp://{_ftpServer}/{ftpDirectory}/{remoteFileName}";
                FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(new Uri(ftpFilePath));
                ftpRequest.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);
                ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;

                using (Stream ftpStream = await ftpRequest.GetRequestStreamAsync())
                {
                    await ftpStream.WriteAsync(data, 0, data.Length);
                }

                Console.WriteLine("Data uploaded successfully to FTP.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading data to FTP: {ex.Message}");
                throw;
            }
        }
    }
}
