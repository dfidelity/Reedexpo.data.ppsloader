using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PPSLambda.HttpClient
{
    public class PPSLambdaHttpClient : IHttpClient, IDisposable
    {
        private System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();
        public PPSLambdaHttpClient(System.Net.Http.HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public PPSLambdaHttpClient()
        {
        }

        private void SetBearerToken(string bearerToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage)
        {
            httpRequestMessage.Headers.ConnectionClose = true;
            var response = await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }
            throw new HttpException(response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PPSLambdaHttpClient()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
        }


        public async Task<JObject> PostGraphQLResponse(string baseUrl, string requestBody, string jwt)
        {
            using (var message = new HttpRequestMessage(HttpMethod.Post, baseUrl))
            {
                message.Headers.Add("cache-control", "no-cache");

                if (!string.IsNullOrWhiteSpace(jwt))
                {
                    SetBearerToken(jwt);
                }

                if (!string.IsNullOrWhiteSpace(requestBody))
                {
                    message.Content = new StringContent(requestBody, Encoding.UTF8, "application/graphql");
                }
                try {
                    using (var response = await _httpClient.SendAsync(message, CancellationToken.None))
                    {
                        using (var content = response.Content)
                        {
                            return JObject.Parse(content.ReadAsStringAsync().Result);
                        }
                    }                    
                } catch(Exception e){
                    Console.WriteLine("Error loading event edition data.\n" + e);
                    throw e;
                }
            }
        }
    }
}