using FaJuan.Api.Infrastructure.Persistence;
using FaJuan.ScheduledTasks.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        configuration.GetConnectionString("Default"),
        sqlOptions => sqlOptions.UseCompatibilityLevel(100)));

services.AddScoped<CloseExpiredOrdersJob>();
services.AddScoped<ExpireUnusedCouponsJob>();

var serviceProvider = services.BuildServiceProvider();

var argsArray = args.ToArray();
var runAll = argsArray.Length == 0;

if (runAll || argsArray.Contains("close-expired-orders"))
{
    using var scope = serviceProvider.CreateScope();
    var job = scope.ServiceProvider.GetRequiredService<CloseExpiredOrdersJob>();
    await job.RunAsync();
    Console.WriteLine("CloseExpiredOrders completed.");
}

if (runAll || argsArray.Contains("expire-unused-coupons"))
{
    using var scope = serviceProvider.CreateScope();
    var job = scope.ServiceProvider.GetRequiredService<ExpireUnusedCouponsJob>();
    await job.RunAsync();
    Console.WriteLine("ExpireUnusedCoupons completed.");
}
