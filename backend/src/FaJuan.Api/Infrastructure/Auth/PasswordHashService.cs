using System.Security.Cryptography;
using System.Text;

namespace FaJuan.Api.Infrastructure.Auth;

public class PasswordHashService
{
    private const string Pbkdf2Prefix = "pbkdf2$";
    private const int Iterations = 120_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    // 新密码统一使用 PBKDF2-HMAC-SHA256；格式：pbkdf2$<iter>$<salt_base64>$<hash_base64>
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password ?? string.Empty,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"{Pbkdf2Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    // 兼容旧格式：无前缀的原始 SHA256 十六进制（由早期迁移种子写入）
    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        if (storedHash.StartsWith(Pbkdf2Prefix, StringComparison.Ordinal))
        {
            return VerifyPbkdf2(password, storedHash);
        }

        return VerifyLegacySha256(password, storedHash);
    }

    // 用于登录成功后判断是否需要升级到新格式
    public bool NeedsRehash(string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return true;
        }
        return !storedHash.StartsWith(Pbkdf2Prefix, StringComparison.Ordinal);
    }

    private static bool VerifyPbkdf2(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password ?? string.Empty,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool VerifyLegacySha256(string password, string storedHash)
    {
        var computed = SHA256.HashData(Encoding.UTF8.GetBytes(password ?? string.Empty));
        var computedHex = Convert.ToHexString(computed);
        return string.Equals(computedHex, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
