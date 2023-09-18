namespace PPSLambda.Model
{
    public class S3Details
    {
        public string S3Bucket { get; }
        public string S3Destination { get;}

        public S3Details(string s3Bucket, string s3Destination)
        {
            S3Bucket = s3Bucket;
            S3Destination = s3Destination;
        }
    }
}