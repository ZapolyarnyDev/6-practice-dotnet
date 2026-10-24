namespace Practice6.Models;

public sealed class StoredFile
{
    public int Id { get; set; }
    public int AdId { get; set; }
    public Ad Ad { get; set; } = null!;
    public string OriginalName { get; set; } = "";
    public string StoredRelativePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
}
