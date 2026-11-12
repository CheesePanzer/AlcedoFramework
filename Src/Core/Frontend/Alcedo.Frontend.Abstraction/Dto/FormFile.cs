namespace Alcedo.Frontend.Abstraction.Dto;

public sealed record FormFile(Stream Stream, string FileName, string ContentType = "application/octet-stream");