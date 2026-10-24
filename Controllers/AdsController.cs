using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Practice6.Contracts;
using Practice6.Data;
using Practice6.Models;

namespace Practice6.Controllers;

[ApiController]
[Authorize]
[Route("api/ads")]
public sealed class AdsController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var userId = UserId;
        var ads = await db.Ads.AsNoTracking().Where(ad => ad.UserId == userId)
            .OrderBy(ad => ad.Id)
            .ToListAsync();
        return Ok(ads.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ad = await db.Ads.AsNoTracking().SingleOrDefaultAsync(ad => ad.Id == id);
        if (ad is null)
            return NotFound();
        if (ad.UserId != UserId)
            return Forbid();
        return Ok(ToDto(ad));
    }

    [HttpPost]
    public async Task<IActionResult> Create(AdWriteDto request)
    {
        var ad = new Ad { UserId = UserId, Title = request.Title.Trim(), Text = request.Text.Trim() };
        db.Ads.Add(ad);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = ad.Id }, ToDto(ad));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AdWriteDto request)
    {
        var ad = await db.Ads.SingleOrDefaultAsync(ad => ad.Id == id);
        if (ad is null)
            return NotFound();
        if (ad.UserId != UserId)
            return Forbid();
        ad.Title = request.Title.Trim();
        ad.Text = request.Text.Trim();
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ad = await db.Ads.SingleOrDefaultAsync(ad => ad.Id == id);
        if (ad is null)
            return NotFound();
        if (ad.UserId != UserId)
            return Forbid();
        db.Ads.Remove(ad);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("moderation")]
    [Authorize(Policy = "AdsModerate")]
    public async Task<IActionResult> GetForModeration()
    {
        var ads = await db.Ads.AsNoTracking().OrderBy(ad => ad.Id)
            .ToListAsync();
        return Ok(ads.Select(ToDto));
    }

    [HttpPatch("{id:int}/hide")]
    [Authorize(Policy = "AdsModerate")]
    public async Task<IActionResult> Hide(int id)
    {
        var ad = await db.Ads.SingleOrDefaultAsync(ad => ad.Id == id);
        if (ad is null)
            return NotFound();
        ad.IsHidden = true;
        await db.SaveChangesAsync();
        return Ok(ToDto(ad));
    }

    [HttpDelete("admin/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAny(int id)
    {
        var ad = await db.Ads.SingleOrDefaultAsync(ad => ad.Id == id);
        if (ad is null)
            return NotFound();
        db.Ads.Remove(ad);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static AdReadDto ToDto(Ad ad) => new(ad.Id, ad.UserId, ad.Title, ad.Text, ad.IsHidden);
}
