using System.Threading;
using Amazon.S3;
using Amazon.S3.Model;
using Moq;
using PPSLambda.Logger;
using PPSLambda.Model;
using PPSLambda.Services;
using Xunit;

namespace PPSLambda.Tests
{
    public class PPSLambdaS3ServiceTest
    {
        [Fact]
        public void ShouldStoreDataToS3()
        {
            var s3Service = new Mock<IAmazonS3>();
            var logger = new Mock<IPPSLambdaLogger>();
            const string bucket = "test-bucket";
            const string eventEditionId = "eve-1";
            const string record = "data";
            var s3Details = new S3Details(bucket, "test-path");
            var eventPath =   $"businessUnitId1/eventId1/{eventEditionId}";
            var key = $"{s3Details.S3Destination}" + "/" + eventPath;
            var putObjectResponse = new PutObjectResponse();
            s3Service.Setup(service => service.PutObjectAsync(It.IsAny<PutObjectRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(putObjectResponse);
            var ratBoxS3Service = new S3Service(s3Service.Object, s3Details, logger.Object);
            
            var result = ratBoxS3Service.LoadToS3(record, eventPath);
            
            Assert.Equal(putObjectResponse, result.Result);
            s3Service.Verify(service => service.PutObjectAsync(
                It.Is<PutObjectRequest>(t=>t.BucketName==bucket && t.ContentBody==record &&t.Key==key
                && t.Metadata["x-amz-meta-harmonized"] == "No"),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
