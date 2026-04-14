using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Eskineria.Core.Storage.Abstractions;
using Eskineria.Core.Storage.Configuration;
using Eskineria.Core.Storage.Security;

namespace Eskineria.Core.Storage.Implementations;

public class S3StorageService : IStorageService
{
    private readonly StorageOptions _options;
    private readonly FileSecurityProvider _security;
    private readonly IAmazonS3 _s3Client;

    public S3StorageService(StorageOptions options, FileSecurityProvider security)
    {
        _options = options;
        _security = security;

        if (string.IsNullOrWhiteSpace(_options.S3.BucketName))
            throw new InvalidOperationException("S3 bucket name is not configured.");

        var config = new AmazonS3Config();
        if (!string.IsNullOrWhiteSpace(_options.S3.ServiceUrl))
        {
            config.ServiceURL = _options.S3.ServiceUrl;
            config.ForcePathStyle = true;
        }
        else if (!string.IsNullOrWhiteSpace(_options.S3.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(_options.S3.Region);
        }

        var hasCredentials = !string.IsNullOrWhiteSpace(_options.S3.AccessKey) &&
                             !string.IsNullOrWhiteSpace(_options.S3.SecretKey);

        _s3Client = hasCredentials
            ? new AmazonS3Client(new BasicAWSCredentials(_options.S3.AccessKey, _options.S3.SecretKey), config)
            : new AmazonS3Client(config);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string folder = "")
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        if (!fileStream.CanSeek)
            throw new ArgumentException("Input stream must be seekable for file validation.", nameof(fileStream));

        _security.ValidateFile(fileName, fileStream.Length);
        _security.ValidateFileContent(fileStream, fileName);
        fileStream.Position = 0;

        var (sanitizedFolder, sanitizedName) = ResolveStoragePath(fileName, folder, forUpload: true);
        var key = BuildObjectKey(sanitizedFolder, sanitizedName);
        var fileTransferUtility = new TransferUtility(_s3Client);

        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = fileStream,
            Key = key,
            BucketName = _options.S3.BucketName
        };

        await fileTransferUtility.UploadAsync(uploadRequest);

        return key;
    }

    public async Task DeleteAsync(string fileName, string folder = "")
    {
        var (sanitizedFolder, sanitizedName) = ResolveStoragePath(fileName, folder, forUpload: false);
        var key = BuildObjectKey(sanitizedFolder, sanitizedName);
        await _s3Client.DeleteObjectAsync(_options.S3.BucketName, key);
    }

    public async Task<Stream> DownloadAsync(string fileName, string folder = "")
    {
        var (sanitizedFolder, sanitizedName) = ResolveStoragePath(fileName, folder, forUpload: false);
        var key = BuildObjectKey(sanitizedFolder, sanitizedName);
        var response = await _s3Client.GetObjectAsync(_options.S3.BucketName, key);
        return response.ResponseStream;
    }

    public string GetFileUrl(string fileName, string folder = "")
    {
        var (sanitizedFolder, sanitizedName) = ResolveStoragePath(fileName, folder, forUpload: false);
        var key = BuildObjectKey(sanitizedFolder, sanitizedName);
        var encodedKey = EncodePath(key);

        if (!string.IsNullOrWhiteSpace(_options.S3.ServiceUrl))
        {
            return $"{_options.S3.ServiceUrl.TrimEnd('/')}/{_options.S3.BucketName}/{encodedKey}";
        }

        if (string.IsNullOrWhiteSpace(_options.S3.Region))
        {
            return $"https://{_options.S3.BucketName}.s3.amazonaws.com/{encodedKey}";
        }

        return $"https://{_options.S3.BucketName}.s3.{_options.S3.Region}.amazonaws.com/{encodedKey}";
    }

    public async Task CreateFolderAsync(string folderName)
    {
        var sanitizedFolder = _security.SanitizeFolderName(folderName);
        if (string.IsNullOrWhiteSpace(sanitizedFolder))
            return;

        var folderKey = sanitizedFolder.EndsWith('/') ? sanitizedFolder : $"{sanitizedFolder}/";

        var request = new PutObjectRequest
        {
            BucketName = _options.S3.BucketName,
            Key = folderKey,
            ContentBody = string.Empty
        };

        await _s3Client.PutObjectAsync(request);
    }

    public async Task DeleteFolderAsync(string folderName)
    {
        var sanitizedFolder = _security.SanitizeFolderName(folderName);
        if (string.IsNullOrWhiteSpace(sanitizedFolder))
            return;

        var folderKey = sanitizedFolder.EndsWith('/') ? sanitizedFolder : $"{sanitizedFolder}/";
        string? continuationToken = null;

        do
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _options.S3.BucketName,
                Prefix = folderKey,
                ContinuationToken = continuationToken
            };

            var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

            if (listResponse.S3Objects.Count > 0)
            {
                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = _options.S3.BucketName,
                    Objects = listResponse.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList()
                };

                await _s3Client.DeleteObjectsAsync(deleteRequest);
            }

            continuationToken = listResponse.IsTruncated == true ? listResponse.NextContinuationToken : null;
        } while (continuationToken != null);
    }

    private (string folder, string fileName) ResolveStoragePath(string fileName, string folder, bool forUpload)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        var effectiveFolder = folder;
        var fileSegment = fileName;

        if (!forUpload && string.IsNullOrWhiteSpace(effectiveFolder))
        {
            var normalizedPath = fileName.Replace("\\", "/", StringComparison.Ordinal);
            var slashIndex = normalizedPath.LastIndexOf('/');
            if (slashIndex > 0)
            {
                effectiveFolder = normalizedPath[..slashIndex];
                fileSegment = normalizedPath[(slashIndex + 1)..];
            }
        }

        var sanitizedFolder = _security.SanitizeFolderName(effectiveFolder ?? string.Empty);
        var sanitizedFileName = forUpload
            ? _security.SanitizeFileName(fileSegment)
            : _security.NormalizeStoredFileName(fileSegment);

        return (sanitizedFolder, sanitizedFileName);
    }

    private static string BuildObjectKey(string folder, string fileName)
    {
        return string.IsNullOrWhiteSpace(folder) ? fileName : $"{folder}/{fileName}";
    }

    private static string EncodePath(string path)
    {
        return string.Join("/", path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));
    }
}
