using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using CSharpFunctionalExtensions;

namespace ElectionResults.Core.Repositories
{
    public class FileRepository : IFileRepository
    {
        private readonly IAmazonS3 _amazonS3;

        public FileRepository(IAmazonS3 amazonS3)
        {
            _amazonS3 = amazonS3;
        }

        public async Task<Result> UploadFiles(string bucketName, FileData fileData)
        {
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileData.Stream,
                Key = fileData.FileName,
                BucketName = bucketName,
                CannedACL = S3CannedACL.NoACL
            };

            using (var fileTransferUtility = new TransferUtility(_amazonS3))
            {
                await fileTransferUtility.UploadAsync(uploadRequest);
            }

            return Result.Success();
        }
    }

    public class FileData
    {
        public Stream Stream { get; set; }
        public string FileName { get; set; }
    }
}