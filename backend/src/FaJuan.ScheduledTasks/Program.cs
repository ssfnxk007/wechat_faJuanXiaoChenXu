using System.Net;
using System.Net.Sockets;
using FaJuan.Api.Application.Orders;
using FaJuan.Api.Application.UserCoupons;
using FaJuan.Api.Infrastructure.Persistence;
using FaJuan.Api.Infrastructure.WeChatPay;
using FaJuan.ScheduledTasks.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("Default")
                           ?? throw new InvalidOperationException("ConnectionStrings:Default 未配置");
    var mysqlVersion = configuration["Database:MySqlServerVersion"]
                       ?? throw new InvalidOperationException("Database:MySqlServerVersion 未配置");

    options.UseMySql(connectionString, new MySqlServerVersion(Version.Parse(mysqlVersion)));
});

services.AddLogging();
services.AddHttpClient<WeChatPayService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .ConfigurePrimaryHttpMessageHandler(CreateIpv4PreferredHttpHandler);
services.AddScoped<WeChatPaySettingsProvider>();
services.AddScoped<UserCouponGrantService>();
services.AddScoped<OrderPaymentService>();
services.AddScoped<OrderExpirationService>();
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
