using BaseOps.Application.Bulletins;
using System.Security.Cryptography;

namespace BaseOps.Infrastructure.Bulletins;

public sealed class AttachmentSecurityService : IAttachmentSecurityService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private static readonly string[] AllowedContentTypes = 
    {
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png"
    };
    private static readonly string StorageBasePath = Path.Combine(Path.GetTempPath(), "bulletin_attachments");

    public AttachmentSecurityService()
    {
        // Ensure storage directory exists
        if (!Directory.Exists(StorageBasePath))
        {
            Directory.CreateDirectory(StorageBasePath);
        }
    }

    public void ValidateFile(string fileName, string contentType, long fileSize)
    {
        // Check file size
        if (fileSize > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of 5MB. Provided size: {fileSize / 1024 / 1024:F2}MB");
        }

        // Check file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");
        }

        // Check content type
        if (!AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new InvalidOperationException($"Content type '{contentType}' is not allowed. Allowed types: {string.Join(", ", AllowedContentTypes)}");
        }

        // Validate extension matches content type
        ValidateExtensionMatchesContentType(extension, contentType);
    }

    public string SanitizeFileName(string fileName)
    {
        // Remove path traversal attempts
        var sanitized = fileName.Replace("..", "").Replace("/", "").Replace("\\", "");
        
        // Remove special characters except dots, hyphens, underscores, and spaces
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[^a-zA-Z0-9._\-\s]", "");
        
        // Trim whitespace
        sanitized = sanitized.Trim();
        
        if (string.IsNullOrEmpty(sanitized))
        {
            throw new InvalidOperationException("File name is invalid after sanitization");
        }

        return sanitized;
    }

    public string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var guid = Guid.NewGuid().ToString("N");
        return $"{guid}{extension}";
    }

    public async Task<(string GeneratedFileName, string StoragePath)> StoreAttachmentAsync(
        string originalFileName,
        string contentType,
        byte[] fileContent,
        Guid uploadedBy,
        CancellationToken cancellationToken = default)
    {
        // Validate file
        ValidateFile(originalFileName, contentType, fileContent.Length);

        // Sanitize original filename
        var sanitizedOriginalName = SanitizeFileName(originalFileName);

        // Generate unique filename
        var generatedFileName = GenerateUniqueFileName(originalFileName);

        // Create user-specific subdirectory
        var userStoragePath = Path.Combine(StorageBasePath, uploadedBy.ToString());
        if (!Directory.Exists(userStoragePath))
        {
            Directory.CreateDirectory(userStoragePath);
        }

        // Full storage path
        var storagePath = Path.Combine(userStoragePath, generatedFileName);

        // Write file asynchronously
        await File.WriteAllBytesAsync(storagePath, fileContent, cancellationToken);

        return (generatedFileName, storagePath);
    }

    public async Task<byte[]> GetAttachmentAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        // Validate path is within storage base path (prevent path traversal)
        var fullPath = Path.GetFullPath(storagePath);
        var basePath = Path.GetFullPath(StorageBasePath);

        if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access denied: Invalid file path");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Attachment not found");
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    public async Task<byte[]?> GetAttachmentContentAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetAttachmentAsync(storagePath, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private void ValidateExtensionMatchesContentType(string extension, string contentType)
    {
        var contentTypeLower = contentType.ToLowerInvariant();
        
        switch (extension)
        {
            case ".pdf":
                if (contentTypeLower != "application/pdf")
                    throw new InvalidOperationException($"File extension '{extension}' does not match content type '{contentType}'");
                break;
            case ".jpg":
            case ".jpeg":
                if (contentTypeLower != "image/jpeg" && contentTypeLower != "image/jpg")
                    throw new InvalidOperationException($"File extension '{extension}' does not match content type '{contentType}'");
                break;
            case ".png":
                if (contentTypeLower != "image/png")
                    throw new InvalidOperationException($"File extension '{extension}' does not match content type '{contentType}'");
                break;
        }
    }
}
