using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Models;

namespace ElectionResults.Core.Repositories
{
    public class BucketRepository : IBucketRepository
    {
        private readonly IAmazonS3 _amazonS3;

        public BucketRepository(IAmazonS3 amazonS3)
        {
            _amazonS3 = amazonS3;
        }

        public virtual async Task<bool> DoesS3BucketExist(string bucketName)
        {
            var bucketExistAsync = await _amazonS3.DoesS3BucketExistAsync(bucketName);
            return bucketExistAsync;
        }

        public async Task<Result<CreateBucketResponse>> CreateBucket(string bucketName)
        {
            try
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true,
                    CannedACL = S3CannedACL.NoACL
                };
                var response = await _amazonS3.PutBucketAsync(putBucketRequest);

                return Result.Success(new CreateBucketResponse
                {
                    BucketName = bucketName,
                    RequestId = response.ResponseMetadata.RequestId
                });
            }
            catch (Exception e)
            {
                Log.LogError(e, "Failed to upload");
                throw;
            }
        }
    }
}
