using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace StowClient
{
    internal class StowClient
    {
        private readonly HttpClient _httpClient;

        public StowClient()
        {
            _httpClient = new HttpClient();
        }

        internal async Task<bool> StoreDicomInDirectory(string sourceDataDirectory, string stowServiceUrl, string accessToken)
        {
            const string text = "application/dicom";
            var multipartContent = GetMultipartContent(text);
            var result = false;
            try
            {
                foreach (var item in Directory.EnumerateFiles(sourceDataDirectory, "*.*", SearchOption.AllDirectories))
                {
                    var streamContent = new StreamContent(File.OpenRead(item));
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(text);
                    multipartContent.Add(streamContent);

                    result = await StoreToServer(multipartContent, stowServiceUrl, accessToken);
                    multipartContent = GetMultipartContent(text);
                }
                if (multipartContent.Any())
                {
                    result = await StoreToServer(multipartContent, stowServiceUrl, accessToken);
                }
            }
            catch (Exception ex)
            {
                result = false;
                Console.WriteLine($"Exception {ex}");
            }
            return result;
        }

        private static MultipartContent GetMultipartContent(string mimeType)
        {
            var multipartContent = new MultipartContent("related", "DICOM DATA BOUNDARY");
            multipartContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", "\"" + mimeType + "\""));
            return multipartContent;
        }

        private async Task<bool> StoreToServer(MultipartContent multiContent, string url, string accessToken)
        {
            try
            {
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                if (accessToken != "")
                {
                    httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                httpRequestMessage.Content = multiContent;
                var result = await _httpClient.SendAsync(httpRequestMessage);

                if (result.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine(result.StatusCode.ToString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred while sending request,check if Url is correct or service is up and running" + ex.Message);
                return false;
            }
            return true;
        }
    }
}