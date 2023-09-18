using System;
using System.Diagnostics;

namespace PPSLambda.Logger
{
    public interface IPPSLambdaLogger
    {
        void LogException(Exception exception, string message);
        
        void LogTrace(string message);

        void LogRequest(string message);

        void LogRequestCompletion(Stopwatch stopwatch);

        void LogRequestCompletionWithException(Stopwatch stopwatch);
    }
}