using System;
using System.Diagnostics;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Amazon.S3;
using Newtonsoft.Json.Linq;
using PPSLambda.Config;
using PPSLambda.HttpClient;
using PPSLambda.Logger;
using PPSLambda.Model;
using PPSLambda.Services;

[assembly: LambdaSerializer(typeof(JsonSerializer))]

namespace PPSLambda
{
    public class Main
    {
        private const string ServiceName = "Data.PPSLambda";

        public string Execute(JObject input, ILambdaContext lambdaContext)
        {
            var logger = PPSLambdaLogger.GetLogger(getLoggerConfig(input, lambdaContext));
            logger.LogTrace("Entered Execute Method Of Main class");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                var config = ConfigFactory.GetConfig();
                var eventEditionService =
                    new EventEditionService(new PPSLambdaHttpClient(), config.RoyalBoxURL);
                var profileQuestionService = new ProfileQuestionService(config.RingBoxURL, new PPSLambdaHttpClient());
                var s3Details = new S3Details(config.S3BucketName, "PPSQuestionAndAnswers/RawData");
                var ratBoxS3Service = new S3Service(new AmazonS3Client(), s3Details, logger);
                var ppsService = new PpsService(logger, eventEditionService, profileQuestionService, ratBoxS3Service,
                    s3Details, config);
                ppsService.LoadPpsData();
                stopWatch.Stop();
                logger.LogRequestCompletion(stopWatch);
                return "Request Successfully Completed";
            }
            catch (Exception e)
            {
                stopWatch.Stop();
                logger.LogException(e, "Error has occured:" + e);
                logger.LogRequestCompletionWithException(stopWatch);
                return "Request Failed";
            }
        }

        private LoggerConfig getLoggerConfig(JObject input, ILambdaContext lambdaContext)
        {
            var config = ConfigFactory.GetConfig();
            return new LoggerConfig
            {
                SplunkUrl = config.SplunkUrl,
                SplunkKey = config.SplunkKey,
                RequestId = lambdaContext.AwsRequestId,
                ResourceId = lambdaContext.InvokedFunctionArn,
                Service = ServiceName,
                ServiceType = "Lambda",
                Version = ""
            };
        }
    }
}