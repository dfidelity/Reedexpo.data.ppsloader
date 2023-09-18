using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using Moq;
using Newtonsoft.Json.Linq;
using PPSLambda.HttpClient;
using Xunit;

namespace PPSLambda.Tests.HttpClient
{
    public class HttpClientTest
    {
        [Fact]
        public void ShouldReturnTheResponseJsonOnSuccess()
        {
            var httpRequestMessage = new HttpRequestMessage();
            var httpResponseMessage = GetHttpResponseMessage(HttpStatusCode.OK, "{\"id\":\"123\"}");

            var ratBoxHttpClient =
                new PPSLambdaHttpClient(GetMockedHttpClient(httpRequestMessage, httpResponseMessage));
            var response = ratBoxHttpClient.SendAsync(httpRequestMessage).Result;

            Assert.Equal("123", JObject.Parse(response.Content.ReadAsStringAsync().Result)["id"]);
        }

        [Fact]
        public void ShouldPostGraphqlResponse()
        {
            const string url = "ringboxurl";
            const string token = "eyb";
            const string requestBody = "{event(id='sd'){name}}";
            var httpResponseMessage = GetHttpResponseMessage(HttpStatusCode.OK, "{\"event\":\"123abc\"}");
            var httpClient = new Mock<System.Net.Http.HttpClient>();
            httpClient.Setup(mock => mock.SendAsync(It.IsAny<HttpRequestMessage>(), CancellationToken.None)).ReturnsAsync(httpResponseMessage);
            var ratBoxHttpClient = new PPSLambdaHttpClient(httpClient.Object);

            var response = ratBoxHttpClient.PostGraphQLResponse(url, requestBody, token);

            Assert.Equal("123abc", response.Result["event"]);
        }
        
        [Fact]
        public void ShouldThrowExceptionForFailures()
        {
            var httpRequestMessage = new HttpRequestMessage();
            var httpResponseMessage = GetHttpResponseMessage(HttpStatusCode.InternalServerError, "{\"id\":\"123\"}");
        
            var ratBoxHttpClient =
                new PPSLambdaHttpClient(GetMockedHttpClient(httpRequestMessage, httpResponseMessage));
            var exception =
                Assert.Throws<AggregateException>(() => ratBoxHttpClient.SendAsync(httpRequestMessage).Result);
        
            Assert.Contains("123", exception.ToString());
        }

        private static HttpResponseMessage GetHttpResponseMessage(HttpStatusCode statusCode, string content) 
            => new HttpResponseMessage(statusCode) {Content = new StringContent(content)};

        private static System.Net.Http.HttpClient GetMockedHttpClient(HttpRequestMessage httpRequestMessage,
            HttpResponseMessage httpResponseMessage)
        {
            var httpClient = new Mock<System.Net.Http.HttpClient>();
            httpClient.Setup(mock => mock.SendAsync(httpRequestMessage, CancellationToken.None)).ReturnsAsync(httpResponseMessage);
            return httpClient.Object;
        }
    }
}
