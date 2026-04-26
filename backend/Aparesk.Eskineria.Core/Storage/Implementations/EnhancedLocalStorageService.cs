using Aparesk.Eskineria.Core.Storage.Abstractions;
using Aparesk.Eskineria.Core.Storage.Configuration;
using Aparesk.Eskineria.Core.Storage.Security;
using Microsoft.AspNetCore.Hosting;

namespace Aparesk.Eskineria.Core.Storage.Implementations;

public class EnhancedLocalStorageService : IStorageService
{
    private const int DefaultFileBufferSize = 81920;
    private readonly StorageOptions _options;
    private readonly FileSecurityProvider _security;
    private readonly IWebHostEnvironment _env;

    public EnhancedLocalStorageService(
        StorageOptions options,
        FileSecurityProvider security,
        IWebHostEnvironment env)
    {
        _options = options;
        _security = security;
        _env = env;
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
        var uploadPath = BuildPhysicalDirectoryPath(sanitizedFolder);
        Directory.CreateDirectory(uploadPath);

        var fullPath = Path.Combine(uploadPath, sanitizedName);

        await using (var stream = new FileStream(
                         fullPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         DefaultFileBufferSize,
                         FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await fileStream.CopyToAsync(stream);
        }

        return BuildRelativePath(sanitizedFolder, sanitizedName);
    }

    public Task DeleteAsync(string fileName, string folder = "")
    {
        var (sanitizedFolder, sanitizedName) = ResolveStoragePath(fileName, folder, forUpload: false);
        var filePath = BuildPhysicalFilePath(sanitizedFolder, sanitizedName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string fileName, string folder = "")
    {
        var (sanitizedFolder, sanitizedName) = ResolveStoragePath(fileName, folder, forUpload: false);
        var filePath = BuildPhysicalFilePath(sanitizedFolder, sanitizedName);
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found in local storage.", sanitizedName);

        return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public string GetFileUrl(string fileName, string folder = "")
    {
        var (sanitizedFolder, sanitizedName) = ResolveStoragePath(fileName, folder, forUpload: false);
        var relativePath = BuildRelativePath(sanitizedFolder, sanitizedName);
        var encodedPath = EncodePath(relativePath);
        var baseUrl = _options.Local.BaseUrl?.TrimEnd('/') ?? string.Empty;

        return string.IsNullOrEmpty(baseUrl) ? $"/{encodedPath}" : $"{baseUrl}/{encodedPath}";
    }

    public Task CreateFolderAsync(string folderName)
    {
        var sanitizedFolder = _security.SanitizeFolderName(folderName);
        var folderPath = BuildPhysicalDirectoryPath(sanitizedFolder);
        
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        return Task.CompletedTask;
    }

    public Task DeleteFolderAsync(string folderName)
    {
        var sanitizedFolder = _security.SanitizeFolderName(folderName);
        if (string.IsNullOrWhiteSpace(sanitizedFolder))
            throw new ArgumentException("Folder name cannot be empty.", nameof(folderName));

        var folderPath = BuildPhysicalDirectoryPath(sanitizedFolder);
        
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true);
        }
        return Task.CompletedTask;
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

    private string BuildPhysicalDirectoryPath(string folder)
    {
        var rootPath = GetStorageRootPath();
        var path = rootPath;
        if (string.IsNullOrWhiteSpace(folder))
            return rootPath;

        foreach (var segment in folder.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            path = Path.Combine(path, segment);
        }

        var fullPath = Path.GetFullPath(path);
        if (!IsSubPathOf(fullPath, rootPath))
            throw new InvalidOperationException("Resolved storage path is outside of configured root.");

        return fullPath;
    }

    private string BuildPhysicalFilePath(string folder, string fileName)
    {
        return Path.Combine(BuildPhysicalDirectoryPath(folder), fileName);
    }

    private static string BuildRelativePath(string folder, string fileName)
    {
        return string.IsNullOrWhiteSpace(folder) ? fileName : $"{folder}/{fileName}";
    }

    private static string EncodePath(string path)
    {
        return string.Join("/", path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));
    }

    private string GetStorageRootPath()
    {
        var configuredRoot = string.IsNullOrWhiteSpace(_options.Local.RootPath)
            ? Path.Combine("wwwroot", "uploads")
            : _options.Local.RootPath;
        var rootPath = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(_env.ContentRootPath, configuredRoot);

        return Path.GetFullPath(rootPath);
    }

    private static bool IsSubPathOf(string candidatePath, string rootPath)
    {
        var normalizedCandidate = candidatePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedRoot = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        return normalizedCandidate.Equals(normalizedRoot, comparison)
               || normalizedCandidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison)
               || normalizedCandidate.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, comparison);
    }
}
