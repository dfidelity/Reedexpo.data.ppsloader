using System;
using System.Net;
using System.Runtime.Serialization;

namespace PPSLambda.Model
{
    [Serializable]
    public class InvalidArgumentException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }
        
        public InvalidArgumentException( string message) : base(message)
        {
            StatusCode = HttpStatusCode.BadRequest;
        }
        
        protected InvalidArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}