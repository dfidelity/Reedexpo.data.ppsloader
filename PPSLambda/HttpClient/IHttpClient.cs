using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PPSLambda.HttpClient
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage);
        
        Task<JObject> PostGraphQLResponse(string baseUrl, string requestBody, string jwt);

    }
}