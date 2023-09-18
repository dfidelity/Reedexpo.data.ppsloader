using System;
using Moq;
using Newtonsoft.Json.Linq;
using PPSLambda.HttpClient;
using PPSLambda.Services;
using Xunit;

namespace PPSLambda.Tests.Services
{
    public class ProfileQuestionServiceTest
    {
        [Fact]
        public void ShouldReturnProfileQuestions()
        {
          AssertProfileQuestionService("{data:{registrationProfileQuestions:{questions:[{id:1,text:\"test\"}]}}}", 
            (result)=>Assert.Equal(JObject.Parse("{questions:[{id:1,text:\"test\"}]}").ToString(), 
              result.ToString()));
        }

        [Fact]
        public void ShouldReturnNullForErrorInPPSData()
        {
          AssertProfileQuestionService("{data:{registrationProfileQuestions:null},error:{message:\"Invalid GBS Code\"}}", 
            Assert.Null);
        }

        [Fact]
        public void ShouldReturnNullForEmptyPPSQuestion()
        {
           AssertProfileQuestionService("{data:{registrationProfileQuestions:{questions:[]}}}", 
             Assert.Null);
        }

        [Fact]
        public async void ShouldThrowExceptionOnGraphQLResponseFailure()
        {
          var ratBoxHttpClient = new Moq.Mock<IHttpClient>();
          ratBoxHttpClient.Setup(mock => mock.PostGraphQLResponse(It.IsAny<string>(),
                    It.IsAny<string>(), null))
                .Throws<Exception>();
          var profileQuestionService = new ProfileQuestionService(It.IsAny<string>(), ratBoxHttpClient.Object);
          await Assert.ThrowsAsync<Exception>(()=> profileQuestionService.GetProfileQuestionsAsync(It.IsAny<string>(), It.IsAny<string>()));
        }
        private void AssertProfileQuestionService(string ppsResponseString, Action<JObject> assertFunc)
        {
          var eventEditionId = "eve-1";
            var locale = "en-GB";
            string profileQuestionsQuery = @"{
                          registrationProfileQuestions(
                                        eventEditionId: ""{0}""
                                        locale: ""{1}""
                                        tagsToInclude: ""{2}""  
                                            ) {
                                                questions {
                                                  id
                                                  text
                                                  instructionalText
                                                  shortDescription
                                                  minimumResponses
                                                  maximumResponses
                                                  sortOrder
                                                  tags
                                                  answers {
                                                    id
                                                    text
                                                    parentId
                                                    taxonomyId
                                                  }
                                                  hierarchicalAnswers
                                                }
                                                config {
                                                  needs {
                                                    groups {
                                                      description
                                                      name
                                                      questions {
                                                        id
                                                        answerIds
                                                        sortOrder
                                                        canRepeat
                                                      }
                                                    }
                                                    maxLimit
                                                  }
                                                  offerings {
                                                    groups {
                                                      description
                                                      name
                                                      questions {
                                                        id
                                                        answerIds
                                                        sortOrder
                                                        canRepeat
                                                      }
                                                    }
                                                    maxLimit
                                                  }
                                                }
                                              }
                                            }"
                .Replace("{0}", eventEditionId)
                .Replace("{1}", locale)
                .Replace("{2}", string.Empty);
            var ratBoxHttpClient = new Moq.Mock<IHttpClient>();
            var ringBoxUrl = "test.ringbox.com";
            ratBoxHttpClient.Setup(mock => mock.PostGraphQLResponse(It.IsAny<string>(),
                    It.IsAny<string>(), null))
                .ReturnsAsync(JObject.Parse(ppsResponseString));

            var profileQuestionService = new ProfileQuestionService(ringBoxUrl, ratBoxHttpClient.Object);

            var result = profileQuestionService.GetProfileQuestionsAsync(eventEditionId, locale).Result;

            assertFunc(result);
            ratBoxHttpClient.Verify(client => client.PostGraphQLResponse(
                It.Is<string>(t => t == $"https://{ringBoxUrl}/graphql"),
                It.Is<string>(t => t == profileQuestionsQuery),
                It.Is<string>(t => t == null)), Times.Once);
        }
    }
}
