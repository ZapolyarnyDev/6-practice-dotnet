using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Practice6.Options;

namespace Practice6.Controllers;

[ApiController]
[Route("api/config")]
public sealed class ConfigController(IHostEnvironment environment,
    IOptions<FileUploadOptions> files, IOptions<JwtOptions> jwt) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new
    {
        environment = environment.EnvironmentName,
        maxSizeBytes = files.Value.MaxSizeBytes,
        allowedExtensions = files.Value.AllowedExtensions,
        secretLength = jwt.Value.Secret.Length
    });
}
