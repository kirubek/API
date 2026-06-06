namespace BaseOps.Application.Bulletins;

public interface IAttachmentSecurityService
{
    Task<(string GeneratedFileName, string StoragePath)> StoreAttachmentAsync(
        string originalFileName,
        string contentType,
        byte[] fileContent,
        Guid uploadedBy,
        CancellationToken cancellationToken = default);
    
    Task<byte[]> GetAttachmentAsync(string storagePath, CancellationToken cancellationToken = default);
    
    Task<byte[]?> GetAttachmentContentAsync(string storagePath, CancellationToken cancellationToken = default);
    
    void ValidateFile(string fileName, string contentType, long fileSize);
    
    string SanitizeFileName(string fileName);
    
    string GenerateUniqueFileName(string originalFileName);
}
