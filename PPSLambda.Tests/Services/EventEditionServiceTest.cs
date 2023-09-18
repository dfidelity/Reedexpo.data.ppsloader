using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PPSLambda.HttpClient;
using PPSLambda.Logger;
using PPSLambda.Models;
using PPSLambda.Services;
using Xunit;

namespace PPSLambda.Tests.Services
{
    public class EventEditionServiceTest
    {
        [Fact]
        public void ShouldReturnEventEditions()
        {
            var eventEditions = new EventEditionCollection
            {
                EventEditions = new List<EventEdition>
                {
                    new EventEdition()
                    {
                        EndDate = DateTime.Now.AddDays(-1),
                        Id = "eve-1",
                        PrimaryLocale = "en-GB",
                        StartDate = DateTime.Now.AddDays(-1)
                    }
                }
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(eventEditions));
            
            AssertEventEditionResponse(eventEditions, httpResponseMessage);
        }

        [Fact]
        public void ShouldReturnEmptyEventEditionCollectionWhenResponseIsEmpty()
        {
            var eventEditions = new EventEditionCollection();
            eventEditions.EventEditions = new List<EventEdition>();
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(eventEditions));
            
            AssertEventEditionResponse(eventEditions, httpResponseMessage);
        }

        private void AssertEventEditionResponse(EventEditionCollection eventEditions,HttpResponseMessage httpResponseMessage)
        {
            var logger = new Mock<IPPSLambdaLogger>().Object;
            var httpClient = new Mock<IHttpClient>();
            var royalboxURL = "royalbox-prd.com";

            httpClient.Setup(client => client.SendAsync(It.IsAny<HttpRequestMessage>()))
                .ReturnsAsync(httpResponseMessage);

            var eventEditionService = new EventEditionService(httpClient.Object, royalboxURL);

            var result = eventEditionService.GetEventEditions();
            Assert.Equal(JsonConvert.SerializeObject(eventEditions), JsonConvert.SerializeObject(result));
            httpClient.Verify(client => 
                client.SendAsync(It.Is<HttpRequestMessage>(
                    message => message.Method==HttpMethod.Get && 
                               message.RequestUri.OriginalString==$"https://{royalboxURL}/v1/event-editions" &&
                               message.Headers.CacheControl.ToString() =="no-cache")), Times.Once);
        }
    }
}
