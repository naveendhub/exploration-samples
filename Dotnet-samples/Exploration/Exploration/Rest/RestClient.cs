using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

#pragma warning disable TI5113

namespace Exploration {
    public class RestClient {

        private static HttpClient httpClientWithProxy = new HttpClient(
            new InvalidCertificateExceptionHandler {
                InnerHandler = GetHttpClientHandler()
            }, false);

        private static HttpClientHandler GetHttpClientHandler() {
            var clientHandler = new HttpClientHandler();
            clientHandler.Credentials = CredentialCache.DefaultCredentials;
            return clientHandler;
        }

        public void Run()
        {
            
            try
            {
                Func<HttpRequestMessage, X509Certificate, X509Chain, SslPolicyErrors, bool> handler1 = DefaultValidationCallback;
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://jsonplaceholder.typicode.com/todos/1");
                httpRequestMessage.Properties.Add("p1", handler1);
                var responseMessage = httpClientWithProxy.SendAsync(httpRequestMessage).Result.Content.ReadAsStringAsync().Result;

                Console.WriteLine(responseMessage);

                Func<HttpRequestMessage, X509Certificate, X509Chain, SslPolicyErrors, bool> handler2 = DefaultValidationCallback;
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://jsonplaceholder.typicode.com/todos/2");
                requestMessage.Properties.Add("p1", handler2);
                var r1 = httpClientWithProxy.SendAsync(requestMessage).Result.Content.ReadAsStringAsync().Result;
                
                Console.WriteLine(r1);
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            
        }
        internal bool DefaultValidationCallback(HttpRequestMessage message, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            Console.WriteLine("DefaultCallBack invoked");
            return true;
        }
        internal bool CustomValidationCallback(HttpRequestMessage message, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            Console.WriteLine("CustomValidationCallback invoked");
            return true;
        }
    }

    public class InvalidCertificateExceptionHandler : DelegatingHandler {

        CancellationTokenSource cts;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) {
            var token = new CancellationToken();
            using (cts = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, token)) {
                try {
                    
                    var clientHandler = this.InnerHandler as HttpClientHandler;
                    clientHandler.ServerCertificateCustomValidationCallback = GetCallback(request);

                    return await base.SendAsync(
                        request,
                        cts?.Token ?? cancellationToken).ConfigureAwait(false);

                } catch (OperationCanceledException) when (!token.IsCancellationRequested) {

                    throw;
                }
            }
        }

        public Func<HttpRequestMessage, X509Certificate, X509Chain, SslPolicyErrors, bool> GetCallback(HttpRequestMessage request) {
            
            if (
                request.Properties.TryGetValue("p1", out var value) &&
                value is Func<HttpRequestMessage, X509Certificate, X509Chain, SslPolicyErrors, bool> callBack
            ) {
                return callBack;
            }
            return null;
        }

    }
}


