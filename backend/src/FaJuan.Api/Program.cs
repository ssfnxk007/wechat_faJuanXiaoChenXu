using System.Text;
using FaJuan.Api.Application.Orders;
using FaJuan.Api.Contracts;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using FaJuan.Api.Infrastructure.WeChat;
using FaJuan.Api.Infrastructure.WeChatPay;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<WeChatMiniProgramOptions>(builder.Configuration.GetSection("WeChatMiniProgram"));
builder.Services.Configure<WeChatPayOptions>(builder.Configuration.GetSection("WeChatPay"));

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
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHealthChecks();
builder.Services.AddScoped<OrderPaymentService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<PasswordHashService>();
builder.Services.AddHttpClient<WeChatMiniProgramService>();
builder.Services.AddHttpClient<WeChatPayService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AdminWeb");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");
app.Run();

public partial class Program;
