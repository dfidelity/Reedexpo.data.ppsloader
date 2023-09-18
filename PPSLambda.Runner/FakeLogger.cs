using Amazon.Lambda.Core;

namespace PPSLambdaRunner
{
    public class FakeLogger : ILambdaLogger
    {
        public void Log(string message)
        {
            System.Console.WriteLine(message);
        }
        public void LogLine(string message)
        {
            System.Console.WriteLine(message);
        }
    }
}