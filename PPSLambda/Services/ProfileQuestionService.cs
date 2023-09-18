using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PPSLambda.HttpClient;

namespace PPSLambda.Services
{
    public class ProfileQuestionService
    {
        public ProfileQuestionService(){}

        private readonly string _ringBoxUrl;
        private readonly IHttpClient _iHttpClient;

        public ProfileQuestionService(string ringBoxUrl, IHttpClient iHttpClient)
        {
            _ringBoxUrl = ringBoxUrl;
            _iHttpClient = iHttpClient;
        }

        public virtual async Task<JObject> GetProfileQuestionsAsync(string eventEditionId, string locale)
        {
            var fullRequestUri = $"https://{_ringBoxUrl}/graphql";


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

            try
            {
                var apiHttpResponse =
                    await _iHttpClient.PostGraphQLResponse(fullRequestUri, profileQuestionsQuery, null);
                var registrationProfileQuestions = apiHttpResponse["data"]["registrationProfileQuestions"];
                if (registrationProfileQuestions.ToString() == "" ||
                    registrationProfileQuestions["questions"].ToString() == "[]")
                    return null;

                return JObject.Parse(registrationProfileQuestions.ToString());

            }catch (Exception e)
            {
                Console.WriteLine(e);
                throw e;
            }
        }
    }
}