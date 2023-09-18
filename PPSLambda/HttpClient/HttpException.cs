using System;
using System.Net;
using System.Runtime.Serialization;

namespace PPSLambda.HttpClient
{
    [Serializable]
    public class HttpException:Exception
    {
        public HttpStatusCode StatusCode { get; }

        public HttpException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
        
        protected HttpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
