using System;
using System.Security.Cryptography;
using System.Text;

namespace TicketingSystem.Application.Security;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        using (var sha256 = SHA256.Create())
        {
            var saltedPassword = string.Concat(Convert.ToBase64String(salt), password);
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));

            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }
    }

    public static bool Verify(string password, string hashedPassword)
    {
        var parts = hashedPassword.Split(':');
        if (parts.Length != 2)
            return false;

        var saltString = parts[0];
        var hashString = parts[1];

        using (var sha256 = SHA256.Create())
        {
            var saltedPassword = string.Concat(saltString, password);
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            var computedHashString = Convert.ToBase64String(hash);

            return hashString == computedHashString;
        }
    }
}