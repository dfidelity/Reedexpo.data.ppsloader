using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PPSLambda.HttpClient;
using PPSLambda.Logger;
using PPSLambda.Model;
using PPSLambda.Models;

namespace PPSLambda.Services
{
    public class EventEditionService
    {
        private readonly IHttpClient _httpClient;
        private readonly string _royalboxUrl;


        public EventEditionService(IHttpClient httpClient, string royalboxUrl)
        {
            _httpClient = httpClient;
            _royalboxUrl = royalboxUrl;
        }

        public virtual EventEditionCollection GetEventEditions()
        {
            var message = new HttpRequestMessage(HttpMethod.Get, $"https://{_royalboxUrl}/v1/event-editions");
            message.Headers.Add("cache-control", "no-cache");
            var response = _httpClient.SendAsync(message).Result;
            return ProcessResponse(response);
        }


        private static EventEditionCollection ProcessResponse(HttpResponseMessage data)
        {
            var eventEditionsResponse = data.Content.ReadAsStringAsync().Result;
            return string.IsNullOrWhiteSpace(eventEditionsResponse)
                ? new EventEditionCollection()
                : JsonConvert.DeserializeObject<EventEditionCollection>(eventEditionsResponse);
        }
        

        private static async Task<JObject> ConvertResponseToJObject(HttpResponseMessage response) =>
            JObject.Parse(await response.Content.ReadAsStringAsync());
    }
}