using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Infrastructure.WeChatPay;

public class WeChatPaySettingsProvider(AppDbContext dbContext)
{
    private static WeChatPaySettingsSnapshot? _cached;
    private static DateTime _cachedAt;
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    public const string DisplayPlaceholder = "已配置";

    public async Task<WeChatPaySettingsSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var (cached, cachedAt) = (_cached, _cachedAt);
        if (cached is not null && DateTime.UtcNow - cachedAt < Ttl)
        {
            return cached;
        }

        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (_cached is not null && DateTime.UtcNow - _cachedAt < Ttl)
            {
                return _cached;
            }

            var row = await dbContext.WeChatPaySettings.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);

            _cached = row is null ? new WeChatPaySettingsSnapshot() : ToSnapshot(row);
            _cachedAt = DateTime.UtcNow;
            return _cached;
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<WeChatPaySettingsSnapshot> SaveAsync(WeChatPaySettingsUpdate update, CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var row = await dbContext.WeChatPaySettings.SingleOrDefaultAsync(x => x.Id == 1, cancellationToken)
                      ?? throw new InvalidOperationException("WeChatPaySetting seed row missing");

            if (update.AppId is not null) row.AppId = update.AppId.Trim();
            if (update.MerchantId is not null) row.MerchantId = update.MerchantId.Trim();
            if (update.MerchantSerialNo is not null) row.MerchantSerialNo = update.MerchantSerialNo.Trim();
            if (update.NotifyUrl is not null) row.NotifyUrl = update.NotifyUrl.Trim();
            if (update.EnableMockFallback is not null) row.EnableMockFallback = update.EnableMockFallback.Value;

            if (ShouldOverwriteSensitive(update.PrivateKeyPem)) row.PrivateKeyPem = update.PrivateKeyPem!.Trim();
            if (ShouldOverwriteSensitive(update.ApiV3Key)) row.ApiV3Key = update.ApiV3Key!.Trim();

            row.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            _cached = ToSnapshot(row);
            _cachedAt = DateTime.UtcNow;
            return _cached;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static void InvalidateCacheForTests()
    {
        _cached = null;
        _cachedAt = DateTime.MinValue;
    }

    private static bool ShouldOverwriteSensitive(string? value)
    {
        if (value is null) return false;
        var trimmed = value.Trim();
        if (trimmed.Length == 0) return false;
        if (trimmed == DisplayPlaceholder) return false;
        return true;
    }

    private static WeChatPaySettingsSnapshot ToSnapshot(WeChatPaySetting row) => new()
    {
        AppId = row.AppId,
        MerchantId = row.MerchantId,
        MerchantSerialNo = row.MerchantSerialNo,
        PrivateKeyPem = row.PrivateKeyPem,
        ApiV3Key = row.ApiV3Key,
        NotifyUrl = row.NotifyUrl,
        EnableMockFallback = row.EnableMockFallback,
        UpdatedAt = row.UpdatedAt,
    };
}
