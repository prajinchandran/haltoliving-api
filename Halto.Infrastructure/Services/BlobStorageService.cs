using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Halto.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Halto.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _serviceClient;
    private readonly BlobContainerClient _containerClient;
    private readonly string _containerName;

    public BlobStorageService(IConfiguration config)
    {
        var connectionString = config["Azure:BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure:BlobStorage:ConnectionString is required.");

        _containerName = config["Azure:BlobStorage:ContainerName"] ?? "halto-media";

        _serviceClient = new BlobServiceClient(connectionString);
        _containerClient = _serviceClient.GetBlobContainerClient(_containerName);
    }

    /// <inheritdoc/>
    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folder = null)
    {
        // Ensure container exists (idempotent)
        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        // Build a unique blob name to avoid collisions
        var ext = Path.GetExtension(fileName);
        var uniqueName = $"{(folder != null ? folder.TrimEnd('/') + "/" : "")}{Guid.NewGuid():N}{ext}";

        var blobClient = _containerClient.GetBlobClient(uniqueName);

        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
        {
            ContentType = contentType
        });

        return blobClient.Uri.ToString();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string blobUrl)
    {
        // Accept either a full URL or just the blob name
        string blobName;
        if (Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
        {
            // Strip leading slash + container name from path
            var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);
            blobName = segments.Length == 2 ? segments[1] : segments[0];
        }
        else
        {
            blobName = blobUrl;
        }

        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }

    /// <inheritdoc/>
    public string GetSasUrl(string blobName, TimeSpan expiry)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }
}
