using System.Security.Cryptography;

namespace Danawa.Server.Security;

/// <summary>
/// PBKDF2-SHA256 비밀번호 해시. docs/02_테이블명세.md users 테이블: pw_hash(32B), pw_salt(16B), pw_iter(기본 210000).
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    public const int Iterations = 210_000;

    public static (byte[] Hash, byte[] Salt, int Iterations) Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return (hash, salt, Iterations);
    }

    public static bool Verify(string password, byte[] hash, byte[] salt, int iterations)
    {
        byte[] computed = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, hash.Length);
        return CryptographicOperations.FixedTimeEquals(computed, hash);
    }
}
