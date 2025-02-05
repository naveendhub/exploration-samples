using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataserverClient
{
    internal class WadoClient
    {
        private HttpClient _httpClient;

        internal async void Run()
        {
            int iterationCount = 500000;
            //http://localhost/DicomRS/wado/studies/1.2.840.113619.2.1.2.139348932.602501178/series/1.2.840.113619.2.1.1.318790346.551.841082886.260/instances/1.2.840.113619.2.1.1.318790346.551.841082886.261
            using (_httpClient = new HttpClient())
            {
                string baseUrl = "http://localhost/DicomRS/";
                string studyInstanceUID = "1.2.840.113619.2.1.2.139348932.602501178";
                string seriesInstanceUID = "1.2.840.113619.2.1.1.318790346.551.841082886.260";
                string sopInstanceUID = "1.2.840.113619.2.1.1.318790346.551.841082886.261";

                for (int i = 0; i < iterationCount; i++)
                {
                    byte[] dicomData = await RetrieveDicomObjectAsync(baseUrl, studyInstanceUID, seriesInstanceUID, sopInstanceUID);

                    // Process the DICOM data as needed
                    Console.WriteLine($"Retrieved DICOM object with iterationCount {i}: {dicomData.Length} bytes.");
                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                }
                
            }
        }


        public async Task<byte[]> RetrieveDicomObjectAsync(string baseUrl, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
        {
            // Construct the WADO URL
            string wadoUrl = $"{baseUrl}/wado/studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}";


            // Create the HTTP request
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, wadoUrl);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/dicom"));

            // Send the HTTP GET request
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            // Ensure the request was successful
            response.EnsureSuccessStatusCode();

            // Read the content as a byte array
            byte[] dicomData = await response.Content.ReadAsByteArrayAsync();

            return dicomData;
        }

    }
}
