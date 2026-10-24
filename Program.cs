using System.Text;
using Gavira;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Practice6.Data;
using Practice6.Services;
using Practice6.Options;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer обязателен.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience обязателен.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Secret) && o.Secret.Length >= 32,
        "Jwt:Secret должен содержать минимум 32 символа.")
    .Validate(o => o.AccessMinutes > 0, "Jwt:AccessMinutes должен быть больше нуля.")
    .ValidateOnStart();
builder.Services.AddOptions<FileUploadOptions>()
    .Bind(builder.Configuration.GetSection("FileUpload"))
    .Validate(o => o.MaxSizeBytes > 0 && o.MaxSizeBytes <= long.MaxValue - 262144,
        "Лимит файла должен быть больше нуля и допускать запас для формы.")
    .Validate(o => o.AllowedExtensions is { Length: > 0 } &&
        o.AllowedExtensions.All(e => e is ".jpg" or ".jpeg" or ".png"),
        "Разрешены только .jpg, .jpeg и .png.")
    .ValidateOnStart();
builder.Services.AddOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>()
    .Configure<IOptions<FileUploadOptions>>((o, files) =>
        o.Limits.MaxRequestBodySize = checked(files.Value.MaxSizeBytes + 262144));
builder.Services.AddOptions<Microsoft.AspNetCore.Http.Features.FormOptions>()
    .Configure<IOptions<FileUploadOptions>>((o, files) =>
        o.MultipartBodyLengthLimit = checked(files.Value.MaxSizeBytes + 262144));

builder.Services.AddGavira<Program>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, settings) =>
{
    var jwt = settings.Value;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdsModerate", policy => policy.RequireAuthenticatedUser().RequireClaim("permission", "ads.moderate")));
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Вставьте accessToken"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();

// Проверяем до миграций и любых изменений базы.
_ = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
_ = app.Services.GetRequiredService<IOptions<FileUploadOptions>>().Value;

app.UseGavira();
app.Use(async (context, next) =>
{
    var limit = context.RequestServices.GetRequiredService<IOptions<FileUploadOptions>>().Value.MaxSizeBytes;
    if (context.Request.ContentLength > limit + 262144)
    {
        await Results.Problem(statusCode: StatusCodes.Status413PayloadTooLarge,
            title: $"Запрос слишком большой. Лимит файла — {limit} байт.").ExecuteAsync(context);
        return;
    }
    await next(context);
});
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await UserSeeder.SeedAsync(db, builder.Configuration);
}

app.MapControllers();
await app.RunAsync();
