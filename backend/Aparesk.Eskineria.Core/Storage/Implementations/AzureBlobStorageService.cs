using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Aparesk.Eskineria.Core.Storage.Abstractions;
using Aparesk.Eskineria.Core.Storage.Configuration;
using Aparesk.Eskineria.Core.Storage.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aparesk.Eskineria.Core.Storage.Implementations;

public class AzureBlobStorageService : IStorageService
{
    private readonly BlobContainerClient _blobContainerClient;
    private readonly FileSecurityProvider _security;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly string _baseUrl;

    public AzureBlobStorageService(
        StorageOptions options,
        FileSecurityProvider security,
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);

        _security = security ?? throw new ArgumentNullException(nameof(security));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var connectionString = !string.IsNullOrWhiteSpace(options.AzureBlob.ConnectionString)
            ? options.AzureBlob.ConnectionString
            : configuration.GetConnectionString("AzureBlobStorage");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");

        var containerName = !string.IsNullOrWhiteSpace(options.AzureBlob.ContainerName)
            ? options.AzureBlob.ContainerName
            : configuration["Storage:ContainerName"];

        if (string.IsNullOrWhiteSpace(containerName))
            containerName = "uploads";

        _baseUrl = !string.IsNullOrWhiteSpace(options.AzureBlob.BaseUrl)
            ? options.AzureBlob.BaseUrl.TrimEnd('/')
            : (configuration["Storage:BaseUrl"]?.TrimEnd('/') ?? string.Empty);

        ValidateContainerName(containerName);

        var blobServiceClient = new BlobServiceClient(connectionString);
        _blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _blobContainerClient.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string folder = "")
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanSeek)
            throw new ArgumentException("The input stream must be seekable.", nameof(stream));

        _security.ValidateFile(fileName, stream.Length);
        _security.ValidateFileContent(stream, fileName);

        var (sanitizedFolder, sanitizedFileName) = ResolveStoragePath(fileName, folder, forUpload: true);
        var blobPath = BuildBlobPath(sanitizedFolder, sanitizedFileName);

        try
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobPath);
            
            stream.Position = 0;

            await blobClient.UploadAsync(stream, overwrite: true);

            _logger.LogInformation("File uploaded successfully: {BlobPath}", blobPath);

            return blobPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {BlobPath}", blobPath);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(string fileName, string folder = "")
    {
        var (sanitizedFolder, sanitizedFileName) = ResolveStoragePath(fileName, folder, forUpload: false);
        var blobPath = BuildBlobPath(sanitizedFolder, sanitizedFileName);

        try
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobPath);
            
            if (!await blobClient.ExistsAsync())
                throw new FileNotFoundException($"File not found: {blobPath}");

            var response = await blobClient.DownloadAsync();

            _logger.LogInformation("File downloaded successfully: {BlobPath}", blobPath);

            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file: {BlobPath}", blobPath);
            throw;
        }
    }

    public async Task DeleteAsync(string fileName, string folder = "")
    {
        var (sanitizedFolder, sanitizedFileName) = ResolveStoragePath(fileName, folder, forUpload: false);
        var blobPath = BuildBlobPath(sanitizedFolder, sanitizedFileName);

        try
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobPath);
            await blobClient.DeleteIfExistsAsync();
            
            _logger.LogInformation("File deleted successfully: {BlobPath}", blobPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {BlobPath}", blobPath);
            throw;
        }
    }

    public string GetFileUrl(string fileName, string folder = "")
    {
        var (sanitizedFolder, sanitizedFileName) = ResolveStoragePath(fileName, folder, forUpload: false);
        var blobPath = BuildBlobPath(sanitizedFolder, sanitizedFileName);

        var blobClient = _blobContainerClient.GetBlobClient(blobPath);

        return string.IsNullOrWhiteSpace(_baseUrl)
            ? blobClient.Uri.ToString()
            : $"{_baseUrl}/{EncodePath(blobPath)}";
    }

    public Task CreateFolderAsync(string folderName)
    {
        // Azure Blob Storage folders are virtual.
        return Task.CompletedTask;
    }

    public async Task DeleteFolderAsync(string folderName)
    {
        var sanitizedFolder = _security.SanitizeFolderName(folderName);
        if (string.IsNullOrWhiteSpace(sanitizedFolder))
            return;

        var prefix = sanitizedFolder.EndsWith('/') ? sanitizedFolder : $"{sanitizedFolder}/";
        var blobs = _blobContainerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, default);
        await foreach (var blob in blobs)
        {
            await _blobContainerClient.DeleteBlobIfExistsAsync(blob.Name);
        }
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

    private static string BuildBlobPath(string folder, string fileName)
    {
        return string.IsNullOrWhiteSpace(folder) ? fileName : $"{folder}/{fileName}";
    }

    private static string EncodePath(string path)
    {
        return string.Join("/", path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));
    }

    private static void ValidateContainerName(string containerName)
    {
        if (containerName.Length < 3 || containerName.Length > 63)
            throw new ArgumentException("Container name must be between 3 and 63 characters");

        if (!System.Text.RegularExpressions.Regex.IsMatch(containerName, "^[a-z0-9]([a-z0-9-]*[a-z0-9])?$"))
            throw new ArgumentException("Invalid container name format");
    }
}
