using FaJuan.Api.Infrastructure.Startup;
using Xunit;

namespace FaJuan.Api.Tests;

public class TimeZoneAssertionTests
{
    [Fact]
    public void Assert_PassesForShanghai()
    {
        var shanghai = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "China Standard Time" : "Asia/Shanghai");
        TimeZoneAssertion.Assert(shanghai);
    }

    [Fact]
    public void Assert_ThrowsForUtc()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TimeZoneAssertion.Assert(TimeZoneInfo.Utc));
    }
}
