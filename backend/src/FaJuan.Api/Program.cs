using System.Text;
using System.Net;
using System.Net.Sockets;
using FaJuan.Api.Application.Orders;
using FaJuan.Api.Application.Erp;
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
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var configuredAdminOrigins = builder.Configuration
    .GetSection("Cors:AdminOrigins")
    .Get<string[]>() ?? [];
var adminOrigins = new[]
{
    "http://localhost:5173",
    "https://localhost:5173",
    "http://127.0.0.1:5173",
    "https://127.0.0.1:5173",
    "http://10.168.1.106:5173",
    "https://10.168.1.106:5173",
    "http://localhost:5180",
    "https://localhost:5180",
    "http://127.0.0.1:5180",
    "https://127.0.0.1:5180",
    "https://xcx.bookso.cn",
}
    .Concat(configuredAdminOrigins)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminWeb", policy =>
    {
        policy.WithOrigins(adminOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<WeChatMiniProgramOptions>(builder.Configuration.GetSection("WeChatMiniProgram"));
builder.Services.Configure<MiniAppThemeSettingsOptions>(builder.Configuration.GetSection("MiniAppTheme"));
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection("Uploads"));
builder.Services.Configure<ErpApiOptions>(builder.Configuration.GetSection("ErpApi"));

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
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
                           ?? throw new InvalidOperationException("ConnectionStrings:Default 未配置");
    var mysqlVersion = builder.Configuration["Database:MySqlServerVersion"]
                       ?? throw new InvalidOperationException("Database:MySqlServerVersion 未配置");

    options.UseMySql(connectionString, new MySqlServerVersion(Version.Parse(mysqlVersion)));
});

builder.Services.AddHealthChecks();
builder.Services.AddScoped<ErpCouponService>();
builder.Services.AddScoped<OrderPaymentService>();
builder.Services.AddScoped<OrderExpirationService>();
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
builder.Services.AddHttpClient<WeChatPayService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .ConfigurePrimaryHttpMessageHandler(CreateIpv4PreferredHttpHandler);

var app = builder.Build();

TimeZoneAssertion.Assert();

var autoMigrate = app.Configuration.GetValue("Database:AutoMigrate", app.Environment.IsDevelopment());
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
});

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

static SocketsHttpHandler CreateIpv4PreferredHttpHandler()
{
    return new SocketsHttpHandler
    {
        UseProxy = false,
        AutomaticDecompression = DecompressionMethods.GZip
                                 | DecompressionMethods.Deflate
                                 | DecompressionMethods.Brotli,
        ConnectCallback = async (context, cancellationToken) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
            var orderedAddresses = addresses
                .OrderBy(address => address.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                .ToArray();

            Exception? lastException = null;
            foreach (var address in orderedAddresses)
            {
                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    socket.Dispose();
                }
            }

            throw lastException ?? new SocketException((int)SocketError.HostNotFound);
        }
    };
}

public partial class Program;
