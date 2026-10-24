namespace Practice6.Options;

public sealed class FileUploadOptions
{
    public long MaxSizeBytes { get; set; }
    public string[] AllowedExtensions { get; set; } = [];
}
