using System.Security.Cryptography;

namespace FaJuan.Api.Application.Common;

public static class OrderNoGenerator
{
    public static string Create(string prefix)
    {
        Span<byte> buffer = stackalloc byte[3];
        RandomNumberGenerator.Fill(buffer);
        var suffix = Convert.ToHexString(buffer);
        return $"{prefix}{DateTime.Now:yyyyMMddHHmmssfff}{suffix}";
    }
}
