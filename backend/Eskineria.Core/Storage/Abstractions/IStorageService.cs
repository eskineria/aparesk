namespace Eskineria.Core.Storage.Abstractions;

public interface IStorageService
{
    /// <summary>
    /// Uploads a file to the configured storage provider.
    /// </summary>
    /// <param name="fileStream">The stream of the file content.</param>
    /// <param name="fileName">The original filename (will be sanitized).</param>
    /// <param name="folder">Optional target folder name.</param>
    /// <returns>The unique path or URL of the uploaded file.</returns>
    Task<string> UploadAsync(Stream fileStream, string fileName, string folder = "");

    /// <summary>
    /// Deletes a file from the configured storage provider.
    /// </summary>
    Task DeleteAsync(string fileName, string folder = "");

    /// <summary>
    /// Downloads a file as a stream.
    /// </summary>
    Task<Stream> DownloadAsync(string fileName, string folder = "");

    /// <summary>
    /// Gets the public access URL for a file.
    /// </summary>
    string GetFileUrl(string fileName, string folder = "");

    /// <summary>
    /// Creates a folder in the storage provider.
    /// </summary>
    Task CreateFolderAsync(string folderName);

    /// <summary>
    /// Deletes a folder and its contents.
    /// </summary>
    Task DeleteFolderAsync(string folderName);
}
