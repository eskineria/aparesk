using System.Text.RegularExpressions;
using Aparesk.Eskineria.Core.Storage.Configuration;
using Microsoft.Extensions.Localization;

namespace Aparesk.Eskineria.Core.Storage.Security;

public class FileSecurityProvider
{
    private const int MaxFileNameLength = 180;
    private static readonly HashSet<char> UnsafeUnicodeCharacters = new(
        new[]
        {
            '\u202A', '\u202B', '\u202C', '\u202D', '\u202E',
            '\u2066', '\u2067', '\u2068', '\u2069',
            '\u200B', '\u200C', '\u200D', '\u200E', '\u200F'
        });
    private static readonly Regex InvalidFileNameCharsRegex = new(
        $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]",
        RegexOptions.Compiled);
    private static readonly Regex InvalidFolderPathCharsRegex = new(
        $"[{Regex.Escape(new string(Path.GetInvalidPathChars().Where(c => c != '/').ToArray()))}]",
        RegexOptions.Compiled);
    private static readonly string[] EmptyAllowedExtensions = [];

    private static readonly HashSet<string> ReservedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "con", "prn", "aux", "nul",
        "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9"
    };

    private readonly StorageOptions _options;
    private readonly IStringLocalizer<FileSecurityProvider> _localizer;

    public FileSecurityProvider(StorageOptions options, IStringLocalizer<FileSecurityProvider> localizer)
    {
        _options = options;
        _localizer = localizer;
    }

    public string SanitizeFileName(string fileName)
    {
        var cleanFileName = NormalizeStoredFileName(fileName);
        
        // Add unique suffix to prevent overwrites and make it harder to guess
        var extension = Path.GetExtension(cleanFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(cleanFileName);
        
        return $"{nameWithoutExtension}_{Guid.NewGuid():N}{extension}";
    }

    public string NormalizeStoredFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException(_localizer["FileNameCannotBeEmpty"].Value);

        fileName = RemoveUnsafeUnicodeCharacters(fileName);

        // Keep only file segment to prevent path traversal attempts.
        var cleanFileName = Path.GetFileName(fileName);

        // Remove illegal characters.
        cleanFileName = InvalidFileNameCharsRegex.Replace(cleanFileName, "_");

        // Prevent hidden files and path traversal fragments.
        cleanFileName = cleanFileName.TrimStart('.', '/', '\\');
        cleanFileName = cleanFileName.Trim();

        if (string.IsNullOrWhiteSpace(cleanFileName))
            throw new ArgumentException(_localizer["InvalidFileName"].Value);

        if (cleanFileName.Length > MaxFileNameLength)
        {
            var extension = Path.GetExtension(cleanFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(cleanFileName);
            var maxBaseLength = Math.Max(1, MaxFileNameLength - extension.Length);
            cleanFileName = $"{nameWithoutExtension[..Math.Min(nameWithoutExtension.Length, maxBaseLength)]}{extension}";
        }

        var normalizedNameWithoutExtension = Path.GetFileNameWithoutExtension(cleanFileName).TrimEnd('.', ' ');
        if (ReservedFileNames.Contains(normalizedNameWithoutExtension))
            throw new ArgumentException(_localizer["InvalidFileName"].Value);

        return cleanFileName;
    }

    public string SanitizeFolderName(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return string.Empty;

        folder = RemoveUnsafeUnicodeCharacters(folder);

        // Remove illegal path characters (but allow / for nesting).
        var cleanFolder = InvalidFolderPathCharsRegex.Replace(folder, "_");

        // Prevent path traversal.
        cleanFolder = cleanFolder.TrimStart('/', '\\', '.');
        cleanFolder = cleanFolder.Replace("..", "");
        
        // Normalize separators and duplicated slashes.
        cleanFolder = cleanFolder.Replace("\\", "/");
        while (cleanFolder.Contains("//", StringComparison.Ordinal))
        {
            cleanFolder = cleanFolder.Replace("//", "/", StringComparison.Ordinal);
        }
        cleanFolder = cleanFolder.Trim('/');
        
        return cleanFolder;
    }

    public void ValidateFile(string fileName, long length)
    {
        if (length <= 0)
            throw new ArgumentException(_localizer["FileCorrupted"].Value);

        // Check size.
        if (length > _options.Security.MaxFileSizeBytes)
            throw new ArgumentException(_localizer["FileSizeExceedsLimit", _options.Security.MaxFileSizeBytes].Value);

        // Check extension.
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExtensions = _options.Security.AllowedExtensions ?? EmptyAllowedExtensions;
        if (allowedExtensions.Length > 0 &&
            !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(_localizer["FileExtensionNotAllowed", extension].Value);
        }
    }

    /// <summary>
    /// SECURITY: Validates file content by checking magic bytes (file signature).
    /// This prevents malicious users from uploading executable files with image extensions.
    /// </summary>
    public void ValidateFileContent(Stream fileStream, string fileName)
    {
        if (fileStream == null || !fileStream.CanRead)
            throw new ArgumentException(_localizer["FileStreamNull"].Value);

        if (!fileStream.CanSeek)
            throw new ArgumentException(_localizer["FileContentValidationFailed"].Value);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        // Read first 12 bytes for magic number detection (WebP needs 12)
        var buffer = new byte[12];
        var originalPosition = fileStream.Position;
        
        try
        {
            var bytesRead = fileStream.Read(buffer, 0, buffer.Length);
            if (bytesRead < 2) // Need at least 2 bytes for most formats
                throw new ArgumentException(_localizer["FileCorrupted"].Value);
            
            // Reset stream position for subsequent operations
            fileStream.Position = originalPosition;
            
            // Validate based on extension and magic bytes
            // SECURITY: Support "Image-is-Image" logic. Browser editors often export as PNG even for JPG files.
            // As long as it is a valid image format, we allow it for any image extension.
            var isValid = extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => IsJpeg(buffer) || IsPng(buffer) || IsGif(buffer) || IsWebp(buffer),
                ".ico" => IsIco(buffer),
                ".pdf" => IsPdf(buffer),
                ".zip" => IsZip(buffer),
                ".7z" => Is7z(buffer),
                ".rar" => IsRar(buffer),
                ".docx" or ".xlsx" or ".pptx" => IsOfficeOpenXml(buffer), // Office files are ZIP-based
                ".txt" => true, // Text files don't have magic bytes
                _ => true // For other allowed extensions, skip magic byte check
            };
            
            if (!isValid)
                throw new ArgumentException(_localizer["FileContentMismatch", extension].Value);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException(_localizer["FileContentValidationFailed"].Value, ex);
        }
    }

    private static string RemoveUnsafeUnicodeCharacters(string value)
    {
        return string.Create(value.Length, value, static (span, source) =>
        {
            var writeIndex = 0;
            foreach (var character in source)
            {
                if (char.IsControl(character) || UnsafeUnicodeCharacters.Contains(character))
                {
                    continue;
                }

                span[writeIndex++] = character;
            }

            span[writeIndex..].Clear();
        }).TrimEnd('\0');
    }

    private static bool IsJpeg(byte[] buffer) =>
        buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xD8;

    private static bool IsPng(byte[] buffer) =>
        buffer.Length >= 4 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47;

    private static bool IsGif(byte[] buffer) =>
        buffer.Length >= 3 && buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46; // "GIF"

    private static bool IsIco(byte[] buffer) =>
        buffer.Length >= 4 &&
        buffer[0] == 0x00 &&
        buffer[1] == 0x00 &&
        buffer[2] == 0x01 &&
        buffer[3] == 0x00;

    private static bool IsPdf(byte[] buffer) =>
        buffer.Length >= 4 && buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46; // "%PDF"

    private static bool IsZip(byte[] buffer) =>
        buffer.Length >= 4 && buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04; // "PK"

    private static bool Is7z(byte[] buffer) =>
        buffer.Length >= 6 && buffer[0] == 0x37 && buffer[1] == 0x7A && buffer[2] == 0xBC && buffer[3] == 0xAF && buffer[4] == 0x27 && buffer[5] == 0x1C;

    private static bool IsRar(byte[] buffer) =>
        buffer.Length >= 6 && buffer[0] == 0x52 && buffer[1] == 0x61 && buffer[2] == 0x72 && buffer[3] == 0x21 && buffer[4] == 0x1A && buffer[5] == 0x07;

    private static bool IsOfficeOpenXml(byte[] buffer) =>
        IsZip(buffer); // Office Open XML files (.docx, .xlsx, .pptx) are ZIP archives

    private static bool IsWebp(byte[] buffer) =>
        buffer.Length >= 12 && 
        buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 && // "RIFF"
        buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50; // "WEBP"
}
