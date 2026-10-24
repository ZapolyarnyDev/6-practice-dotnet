using Microsoft.Extensions.Options;
using Practice6.Data;
using Practice6.Models;
using Practice6.Options;

namespace Practice6.Services;

public sealed class FileStorageService(AppDbContext db, IHostEnvironment environment,
    IOptions<FileUploadOptions> settings, ILogger<FileStorageService> logger)
{
    private string Root => Path.Combine(environment.ContentRootPath, "storage", "uploads");

    public async Task<string?> ValidateAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return "Файл пустой.";
        if (file.Length > settings.Value.MaxSizeBytes)
            return $"Размер файла превышает лимит {settings.Value.MaxSizeBytes} байт.";
        var extension = Extension(file);
        if (!settings.Value.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return "Расширение файла не разрешено. Разрешены: " + string.Join(", ", settings.Value.AllowedExtensions);
        byte[] signature = extension switch
        {
            ".jpg" or ".jpeg" => [0xFF, 0xD8, 0xFF],
            ".png" => [0x89, 0x50, 0x4E, 0x47],
            _ => []
        };
        await using var stream = file.OpenReadStream();
        var header = new byte[signature.Length];
        var count = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken: ct);
        if (signature.Length == 0 || count != signature.Length || !header.SequenceEqual(signature))
            return "Содержимое файла не совпадает с расширением.";
        return null;
    }

    public async Task<StoredFile> SaveAsync(IFormFile file, int adId, int userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var extension = Extension(file);
        var relativePath = $"{now:yyyy/MM/dd}/{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, useAsync: true))
                await file.CopyToAsync(output, ct);
            var stored = new StoredFile
            {
                AdId = adId,
                OriginalName = Path.GetFileName(file.FileName.Replace('\\', '/')),
                StoredRelativePath = relativePath,
                ContentType = extension == ".png" ? "image/png" : "image/jpeg",
                Size = file.Length,
                UploadedBy = userId,
                UploadedAt = now
            };
            db.StoredFiles.Add(stored);
            await db.SaveChangesAsync(ct);
            return stored;
        }
        catch
        {
            try { File.Delete(path); }
            catch (Exception cleanupError)
            {
                logger.LogError(cleanupError, "Не удалось удалить файл после ошибки сохранения: {Path}", relativePath);
            }
            throw;
        }
    }

    public FileStream? OpenRead(StoredFile file)
    {
        var root = Path.GetFullPath(Root) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, file.StoredRelativePath));
        if (!path.StartsWith(root, StringComparison.Ordinal))
            return null;
        try
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    private static string Extension(IFormFile file) =>
        Path.GetExtension(Path.GetFileName(file.FileName.Replace('\\', '/'))).ToLowerInvariant();
}
