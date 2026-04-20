namespace FaJuan.Api.Infrastructure.Startup;

public static class TimeZoneAssertion
{
    private static readonly string[] AllowedIds = { "China Standard Time", "Asia/Shanghai" };

    public static void Assert(TimeZoneInfo? tz = null)
    {
        tz ??= TimeZoneInfo.Local;
        if (Array.IndexOf(AllowedIds, tz.Id) < 0)
        {
            throw new InvalidOperationException(
                $"服务器时区必须为 Asia/Shanghai / China Standard Time，当前：{tz.Id}。" +
                "容器部署请设置环境变量 TZ=Asia/Shanghai。");
        }
    }
}
