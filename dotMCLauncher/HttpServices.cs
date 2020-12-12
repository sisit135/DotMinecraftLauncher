using System;
using System.IO;
using System.Net;

namespace dotMCLauncher
{
    public class HttpServices
    {
        public static string HttpWebGET(string address)
        {
            try
            {
                string Response = string.Empty;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.Method = "GET";
                //request.UserAgent = "FreeLauncher/V " + Application.ProductVersion.ToString();
                request.UserAgent = "FreeLauncher/V ";
                request.Timeout = 5000;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    Response = reader.ReadToEnd();
                }

                //Console.WriteLine(htm
                return Response;
            }
            catch (Exception e)
            {
                return e.StackTrace.ToString();
            }
            return string.Empty;
        }
    }
}