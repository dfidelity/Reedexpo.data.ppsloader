using System;
using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PPSLambda.Logger;
using PPSLambda.Model;
using PPSLambda.Models;

namespace PPSLambda.Services
{
    public class PpsService
    {
        private readonly IPPSLambdaLogger _logger;
        private readonly EventEditionService _eventEditionService;
        private readonly ProfileQuestionService _profileQuestionService;
        private readonly S3Service _s3Service;
        private readonly S3Details _s3Details;
        private readonly Config.Config _config;

        public PpsService(IPPSLambdaLogger logger, EventEditionService eventEditionService,
            ProfileQuestionService profileQuestionService, S3Service s3Service, S3Details s3Details, Config.Config config)
        {
            _logger = logger;
            _eventEditionService = eventEditionService;
            _profileQuestionService = profileQuestionService;
            _s3Service = s3Service;
            _s3Details = s3Details;
            _config = config;
        }

        public void LoadPpsData()
        {
            var eventEditions = _eventEditionService.GetEventEditions();
            foreach (var eventEdition in eventEditions.EventEditions)
            {
                var questions = LoadPpsForEventEdition(eventEdition);
                if (questions != null)
                    LoadDataToS3(questions, eventEdition.Id);
            }
            
        }

        private JObject LoadPpsForEventEdition(EventEdition eventEdition)
        {
            try {
                return _profileQuestionService
                    .GetProfileQuestionsAsync(eventEdition.Id, eventEdition.PrimaryLocale).Result;
            } catch (Exception e){
                _logger.LogException(e, "Error loading PPS for even edition:"+ eventEdition.Id + "\n" + e.ToString());
                return null;
            }
            
        }

        private void LoadDataToS3(JObject questions, string eventEditionId)
        {
            try
            {
                var result = _s3Service.LoadToS3(JsonConvert.SerializeObject(questions), GetFileName(eventEditionId))
                    .Result;
                _logger.LogRequest($"Response status code: {result.HttpStatusCode}");
            }
            catch (Exception e)
            {
                _logger.LogException(e, e.ToString());
            }
        }

        private string GetFileName(string eventEditionId)
        {
            return $"{eventEditionId}/{eventEditionId}";
        }
    }
}