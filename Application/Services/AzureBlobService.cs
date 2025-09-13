using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace datopus.Application.Services
{
    public class AzureBlobService
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public AzureBlobService(string connectionString, string containerName)
        {
            _connectionString = connectionString;
            _containerName = containerName;
        }

        public async Task<string> UploadImageAsync(byte[] data, string userId, string blobName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobPath = $"{userId}/images/{blobName}";
            var blobClient = containerClient.GetBlobClient(blobPath);
            using var stream = new MemoryStream(data);
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "image/png" });

            return blobClient.Uri.ToString();
        }
    }
}
