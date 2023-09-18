using System;
using System.Collections.Generic;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PPSLambda.Logger;
using PPSLambda.Model;
using PPSLambda.Models;
using PPSLambda.Services;
using Xunit;

namespace PPSLambda.Tests.Services
{
    public class PpsServiceTest
    {
        [Fact]
        public void ShouldLoadPpsForEventEditions()
        {
            var profileQuestions = JObject.Parse("{questions:[{id:1,text:\"test\"}]}");
            const string eventEditionId = "eve-1";

            AssertLoadPpsForEventEdition(profileQuestions, ratBoxS3Service => ratBoxS3Service.Verify(service =>
                service.LoadToS3(
                    It.Is<string>(t => t == JsonConvert.SerializeObject(profileQuestions)),
                    It.Is<string>(t => t == $"{eventEditionId}/{eventEditionId}")), Times.Once));
        }

        [Fact]
        public void ShouldNotLoadPpsWhenQuestionIsEmpty()
        {
            AssertLoadPpsForEventEdition(null,
                (ratBoxS3Service) => ratBoxS3Service.Verify(service => service.LoadToS3(
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Never));
        }

        [Fact]
        public void ShouldNotLoadPpsForEventEditionsWhenExceptionOccurs()
        {
            var logger = new Mock<IPPSLambdaLogger>();
            Config.Config config = new Config.Config()
            {
                FeatureToggles = ""
            };
            var eventEditionService = new Mock<EventEditionService>(null,null);
            var profileQuestionService = new Mock<ProfileQuestionService>();
            var ratBoxS3Service = new Mock<S3Service>(null,null,null);
            var eventEditions = new EventEditionCollection();
            const string eventEditionId = "eve-1";
            const string primaryLocale = "en-GB";
            eventEditions.EventEditions = new List<EventEdition>
            {
                new EventEdition()
                {
                    EndDate = DateTime.Now.AddDays(-1),
                    Id = eventEditionId,
                    PrimaryLocale = primaryLocale,
                    StartDate = DateTime.Now.AddDays(-1)
                }
            };
            var eventDetail = new EventDetail("ev","bu",eventEditionId);
            eventEditionService.Setup(service => service.GetEventEditions()).Returns(eventEditions);
            profileQuestionService.Setup(service =>
                    service.GetProfileQuestionsAsync(It.IsAny<string>(), It.IsAny<string>())).Throws<Exception>();

            var ppsService = new PpsService(logger.Object, eventEditionService.Object,
                profileQuestionService.Object, ratBoxS3Service.Object, new S3Details("bucket","dest"),
                config);
            ppsService.LoadPpsData();
            ratBoxS3Service.Verify(service => service.LoadToS3(
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);

        }
        private static void AssertLoadPpsForEventEdition(JObject profileQuestions,
            Action<Mock<S3Service>> assertFunc)
        {
            var logger = new Mock<IPPSLambdaLogger>();
            Config.Config config = new Config.Config()
            {
                FeatureToggles = ""
            };
            var eventEditionService = new Mock<EventEditionService>(null,null);
            var profileQuestionService = new Mock<ProfileQuestionService>();
            var ratBoxS3Service = new Mock<S3Service>(null,null,null);
            var eventEditions = new EventEditionCollection();
            const string eventEditionId = "eve-1";
            const string primaryLocale = "en-GB";
            eventEditions.EventEditions = new List<EventEdition>
            {
                new EventEdition()
                {
                    EndDate = DateTime.Now.AddDays(-1),
                    Id = eventEditionId,
                    PrimaryLocale = primaryLocale,
                    StartDate = DateTime.Now.AddDays(-1)
                }
            };
            var eventDetail = new EventDetail("ev","bu",eventEditionId);
            profileQuestionService.Setup(service =>
                    service.GetProfileQuestionsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(profileQuestions);
            eventEditionService.Setup(service => service.GetEventEditions()).Returns(eventEditions);
            var ppsService = new PpsService(logger.Object, eventEditionService.Object,
                profileQuestionService.Object, ratBoxS3Service.Object, new S3Details("bucket","dest"),
                config);
            ppsService.LoadPpsData();
            
            eventEditionService.Verify(service => service.GetEventEditions(), Times.Once);
            profileQuestionService.Verify(service => service.GetProfileQuestionsAsync(
                It.Is<string>(t => t == eventEditionId),
                It.Is<string>(t => t == primaryLocale)), Times.Once);
            assertFunc(ratBoxS3Service);
        }

        [Fact]
        public void ShouldNotProcessWhenNoEventEditions()
        {
            var logger = new Mock<IPPSLambdaLogger>();
            Config.Config config = new Config.Config()
            {
                FeatureToggles = ""
            };
            var eventEditionService = new Mock<EventEditionService>(null,null);
            var profileQuestionService = new Mock<ProfileQuestionService>();
            var ratBoxS3Service = new Mock<S3Service>(null, null, null);
            var eventEditions = new EventEditionCollection {EventEditions = new List<EventEdition>()};
            eventEditionService.Setup(service => service.GetEventEditions()).Returns(eventEditions);
            var ppsService = new PpsService(logger.Object, eventEditionService.Object,
                profileQuestionService.Object, ratBoxS3Service.Object,  new S3Details("bucket","dest"),
                config);
            ppsService.LoadPpsData();
            
            eventEditionService.Verify(service => service.GetEventEditions(), Times.Once);
            profileQuestionService.Verify(service => service.GetProfileQuestionsAsync(
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
            ratBoxS3Service.Verify(service => service.LoadToS3(
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }
    }
}
