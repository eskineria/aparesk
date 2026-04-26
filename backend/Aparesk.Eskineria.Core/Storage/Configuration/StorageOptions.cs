namespace Aparesk.Eskineria.Core.Storage.Configuration;

public class StorageOptions
{
    public StorageProviderType ProviderType { get; set; } = StorageProviderType.Local;
    public LocalStorageOptions Local { get; set; } = new();
    public S3StorageOptions S3 { get; set; } = new();
    public AzureBlobStorageOptions AzureBlob { get; set; } = new();
    public SecurityOptions Security { get; set; } = new();
}

public enum StorageProviderType
{
    Local,
    S3,
    AzureBlob
}

public class LocalStorageOptions
{
    /// <summary>
    /// Physical path on disk. Example: "wwwroot/uploads"
    /// </summary>
    public string RootPath { get; set; } = "wwwroot/uploads";
    
    /// <summary>
    /// Base URL for public access. Example: "http://localhost:5000/uploads"
    /// </summary>
    public string BaseUrl { get; set; } = "/uploads";
}

public class S3StorageOptions
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string? ServiceUrl { get; set; } // For S3-compatible services like Minio
}

public class AzureBlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "uploads";
    public string BaseUrl { get; set; } = string.Empty;
}

public class SecurityOptions
{
    /// <summary>
    /// Limit allowed extensions. If empty, all are allowed (not recommended).
    /// </summary>
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".ico", ".pdf", ".txt", ".docx" };

    /// <summary>
    /// Maximum file size allowed. Default: 5MB.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
}
