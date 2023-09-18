using System;
using Amazon.Lambda.Core;

namespace PPSLambdaRunner
{
   
    public class FakeLambdaContext : ILambdaContext
    {
        ILambdaLogger _logger;
        public FakeLambdaContext() {
            _logger = new FakeLogger();
        }
        public string AwsRequestId { get; set; }
        public IClientContext ClientContext => throw new NotImplementedException();
        public string FunctionName => throw new NotImplementedException();
        public string FunctionVersion => throw new NotImplementedException();
        public ICognitoIdentity Identity => throw new NotImplementedException();
        public string InvokedFunctionArn { get; set; }
        public ILambdaLogger Logger => _logger;
        public string LogGroupName => throw new NotImplementedException();
        public string LogStreamName => throw new NotImplementedException();
        public int MemoryLimitInMB => throw new NotImplementedException();
        public TimeSpan RemainingTime => throw new NotImplementedException();
    }
}