using System.Security.Cryptography;

namespace Alcedo.Core.Application;

public record UploadedFileDto
{
    public Stream Content { get; init; }
    public string FileName { get; init; }
    public string ContentType { get; init; }
    public long Size { get; init; }
    public string Hash { get; init; }
    public string Extension => Path.GetExtension(FileName);
    public string HashedFileName => $"{Hash}{Extension.ToLowerInvariant()}";

    public UploadedFileDto(Stream content, string fileName, string contentType, long size)
    {
        Content = content;
        FileName = fileName;
        ContentType = contentType;
        Size = size;
        Hash = ComputeHash(content);
    }

    private static string ComputeHash(Stream stream)
    {
        var position = stream.Position;
        stream.Position = 0;
        
        var hash = SHA256.HashData(stream);
        
        stream.Position = position;
        
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}