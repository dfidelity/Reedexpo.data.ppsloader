using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using PPSLambda.Logger;
using PPSLambda.Model;

namespace PPSLambda.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Service;
        private readonly IPPSLambdaLogger _logger;
        private readonly S3Details _s3Details;
        
        public S3Service(IAmazonS3 s3Service, S3Details s3Details, IPPSLambdaLogger logger)
        {
            _s3Service = s3Service;
            _s3Details = s3Details;
            _logger = logger;
        }

        public virtual async Task<PutObjectResponse> LoadToS3(string record, string key)
        {
            var filePath = $"{_s3Details.S3Destination}" + "/" + key;
            _logger.LogTrace($"Creating file: {filePath}");
            var request = new PutObjectRequest
            {
                BucketName = _s3Details.S3Bucket,
                Key = filePath,
                ContentBody = record
            };
            request.Metadata.Add("x-amz-meta-harmonized", "No");
            return await _s3Service.PutObjectAsync(request);
        }
    }
}
