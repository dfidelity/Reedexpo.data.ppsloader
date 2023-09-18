namespace PPSLambda.Model
{
    public class SnsMessage
    {
        public string Message { get; }
        public string MessageAttributes { get; }
        
        public SnsMessage(string message, string messageAttributes)
        {
            Message = message;
            MessageAttributes = messageAttributes;
        }

        public override string ToString()
        {
            return $"Message: ${Message}, MessageAttributes: ${MessageAttributes}";
        }
    }
}