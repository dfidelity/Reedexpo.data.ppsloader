using System;
using System.Diagnostics;
using Amazon.Lambda.Core;
using NLog;
using ReedExpo.Digital.Logging;
using LambdaLogger = Amazon.Lambda.Core.LambdaLogger;

namespace PPSLambda.Logger
{
    public class PPSLambdaLogger:IPPSLambdaLogger
    {
        private readonly NLog.Logger _logger;

        private PPSLambdaLogger(NLog.Logger logger)
        {
            _logger = logger;
        }

        public void LogException(Exception exception, string message)
        {
            var logEventInfo = new LogEventInfo
            {
                Level = LogLevel.Error,
                Exception = exception,
                Message = message,
                LoggerName = GetType().Name
            };
            logEventInfo.Properties.Add(Attributes.EventType, EventType.Exception);
            
            _logger.Log(logEventInfo);
            LambdaLogger.Log(message);
        }
        
        public void LogTrace(string message)
        {
            var logEventInfo = new LogEventInfo
            {
                Level = LogLevel.Debug,
                Message = message,
                LoggerName = GetType().Name
            };
            logEventInfo.Properties.Add(Attributes.EventType, EventType.Trace);
            
            _logger.Log(logEventInfo);
            LambdaLogger.Log(message);
        }
        
        public void LogRequest(string message)
        {
            var logEventInfo = new LogEventInfo
            {
                Level = LogLevel.Info,
                Message = message,
                LoggerName = GetType().Name
            };
            logEventInfo.Properties.Add(Attributes.EventType, EventType.Request);
            
            _logger.Log(logEventInfo);
            LambdaLogger.Log(message);
        }
        
        public void LogRequestCompletion(Stopwatch stopwatch)
        {
            _logger.Log(GetRequestCompletionEvent(stopwatch, "Request Completed"));
        }
        
        public void LogRequestCompletionWithException(Stopwatch stopwatch)
        {
            _logger.Log(GetRequestCompletionEvent(stopwatch, "Request complete with Exception"));
        }

        private LogEventInfo GetRequestCompletionEvent(Stopwatch stopwatch, string message)
        {
            var logEventInfo = new LogEventInfo
            {
                Level = LogLevel.Info,
                Message = message,
                LoggerName = GetType().Name
            };
            logEventInfo.Properties.Add(Attributes.EventType, EventType.Request);
            logEventInfo.Properties.Add(Attributes.Duration, stopwatch.ElapsedMilliseconds.ToString());
            logEventInfo.Properties.Add(Attributes.DurationTicks, stopwatch.ElapsedTicks.ToString());
            return logEventInfo;
        }
        
        public static IPPSLambdaLogger GetLogger(LoggerConfig loggerConfig)
        {
            var simpleContext = new SimpleContext
            {
                CorrelationId = loggerConfig.CorrelationId,
                RequestId = loggerConfig.RequestId,
                ResourceId = loggerConfig.ResourceId,
                Service = loggerConfig.Service,
                ServiceType = loggerConfig.ServiceType,
                Version = loggerConfig.Version
            };
            var nLog = LoggingSetup.GetLogger(loggerConfig.SplunkUrl, loggerConfig.SplunkKey, "RatBox.Lambda", simpleContext);
            return new PPSLambdaLogger(nLog);
        }
    }
}
