namespace BaseOps.Application.Bulletins.DTOs;

public record UploadAttachmentDto
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public byte[] FileContent { get; init; } = Array.Empty<byte>();
}
