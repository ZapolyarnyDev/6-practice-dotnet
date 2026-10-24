using Gavira;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practice6.Contracts;
using Practice6.Services;

namespace Practice6.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto request)
    {
        var tokens = await auth.RegisterAsync(request);
        if (tokens is null)
            return GaviraProblem.Create(HttpContext, 409, "Email уже занят", "Используйте другой email или войдите");
        return Ok(tokens);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        var tokens = await auth.LoginAsync(request);
        if (tokens is null)
            return GaviraProblem.Create(HttpContext, 401, "Неверный логин или пароль", "Неверный логин или пароль");
        return Ok(tokens);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshDto request)
    {
        var tokens = await auth.RefreshAsync(request.RefreshToken);
        if (tokens is null)
            return GaviraProblem.Create(HttpContext, 401, "Refresh-токен недействителен", "Войдите заново");
        return Ok(tokens);
    }
}
