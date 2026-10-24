using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Practice6.Data;
using Practice6.Models;
using Practice6.Services;

namespace Practice6.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class FilesController(AppDbContext db, FileStorageService storage) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Автор читает свои объявления; модератор видит все через /ads/moderation.
    private bool CanRead(Ad ad) => ad.UserId == UserId || User.HasClaim("permission", "ads.moderate");

    [HttpPost("ads/{adId:int}/files")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(int adId, [FromForm] UploadFileForm form, CancellationToken ct)
    {
        var error = await storage.ValidateAsync(form.File, ct);
        if (error is not null)
            return BadRequest(new ProblemDetails { Status = 400, Title = error });
        if (Path.GetFileName(form.File!.FileName.Replace('\\', '/')).Length > 255)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Имя файла длиннее 255 символов." });
        var ad = await db.Ads.SingleOrDefaultAsync(a => a.Id == adId, ct);
        if (ad is null)
            return NotFound();
        if (ad.UserId != UserId || ad.IsHidden)
            return Forbid();
        var file = await storage.SaveAsync(form.File!, adId, UserId, ct);
        return CreatedAtAction(nameof(Download), new { id = file.Id }, ToDto(file));
    }

    [HttpGet("ads/{adId:int}/files")]
    public async Task<IActionResult> List(int adId, CancellationToken ct)
    {
        var ad = await db.Ads.AsNoTracking().SingleOrDefaultAsync(a => a.Id == adId, ct);
        if (ad is null)
            return NotFound();
        if (!CanRead(ad))
            return Forbid();
        var files = await db.StoredFiles.AsNoTracking().Where(f => f.AdId == adId)
            .OrderBy(f => f.Id).ToListAsync(ct);
        return Ok(files.Select(ToDto));
    }

    [HttpGet("files/{id:int}")]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var file = await db.StoredFiles.AsNoTracking().Include(f => f.Ad)
            .SingleOrDefaultAsync(f => f.Id == id, ct);
        if (file is null)
            return NotFound();
        if (!CanRead(file.Ad))
            return Forbid();
        var stream = storage.OpenRead(file);
        if (stream is null)
            return NotFound();
        return File(stream, file.ContentType, file.OriginalName);
    }

    private static object ToDto(StoredFile f) => new
    {
        f.Id, f.OriginalName, f.ContentType, f.Size,
        UploadedAt = DateTime.SpecifyKind(f.UploadedAt, DateTimeKind.Utc)
    };
}

public sealed class UploadFileForm
{
    [FromForm(Name = "file")]
    public IFormFile? File { get; set; }
}
