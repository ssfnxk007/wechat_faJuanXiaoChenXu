using System.Text;
using FaJuan.Api.Application.Orders;
using FaJuan.Api.Application.UserCoupons;
using FaJuan.Api.Contracts;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Media;
using FaJuan.Api.Infrastructure.MiniApp;
using FaJuan.Api.Infrastructure.Persistence;
using FaJuan.Api.Infrastructure.Startup;
using FaJuan.Api.Infrastructure.WeChat;
using FaJuan.Api.Infrastructure.WeChatPay;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminWeb", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173",
                "http://10.168.1.106:5173",
                "https://10.168.1.106:5173",
                "http://localhost:5180",
                "https://localhost:5180",
                "http://127.0.0.1:5180",
                "https://127.0.0.1:5180")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<WeChatMiniProgramOptions>(builder.Configuration.GetSection("WeChatMiniProgram"));
builder.Services.Configure<MiniAppThemeSettingsOptions>(builder.Configuration.GetSection("MiniAppTheme"));
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection("Uploads"));

var uploadMaxBytes = builder.Configuration.GetSection("Uploads").Get<UploadOptions>()?.MaxFileSizeBytes
                     ?? new UploadOptions().MaxFileSizeBytes;
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = uploadMaxBytes;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = uploadMaxBytes;
});

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FaJuan.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FaJuan.Admin";
var jwtSecurityKey = builder.Configuration["Jwt:SecurityKey"] ?? throw new InvalidOperationException("Jwt:SecurityKey 未配置");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecurityKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sqlOptions =>
        {
            // The current deployment database behaves like SQL Server 2008 R2, so force legacy-compatible SQL generation.
            sqlOptions.UseCompatibilityLevel(100);
        }));

builder.Services.AddHealthChecks();
builder.Services.AddScoped<OrderPaymentService>();
builder.Services.AddScoped<UserCouponGrantService>();
builder.Services.AddSingleton<MiniAppThemeSettingsService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<PasswordHashService>();
builder.Services.AddSingleton<ImageCompressor>();
builder.Services.AddHttpClient<WeChatMiniProgramService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseProxy = false,
        AutomaticDecompression = System.Net.DecompressionMethods.GZip
                                 | System.Net.DecompressionMethods.Deflate
                                 | System.Net.DecompressionMethods.Brotli,
    });
builder.Services.AddScoped<WeChatPaySettingsProvider>();
builder.Services.AddHttpClient<WeChatPayService>();

var app = builder.Build();

TimeZoneAssertion.Assert();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath);
Directory.CreateDirectory(Path.Combine(webRootPath, "uploads"));

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath),
    RequestPath = ""
});

app.UseHttpsRedirection();
app.UseCors("AdminWeb");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");
app.Run();

public partial class Program;
