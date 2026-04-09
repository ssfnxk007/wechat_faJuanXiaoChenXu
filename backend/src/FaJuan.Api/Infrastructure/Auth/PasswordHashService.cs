using System.Security.Cryptography;
using System.Text;

namespace FaJuan.Api.Infrastructure.Auth;

public class PasswordHashService
{
    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string password, string hash)
    {
        return string.Equals(Hash(password), hash, StringComparison.OrdinalIgnoreCase);
    }
}
