using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

public static class PasswordHasher
{
    public static (byte[] PasswordHash, byte[] PasswordSalt) HashPassword(string password)
    {
        // Generate a random salt
        byte[] salt = new byte[128 / 8]; // 128 bits
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hash the password using PBKDF2
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000, // Recommended number of iterations
            numBytesRequested: 256 / 8)); // 256 bits

        return (Convert.FromBase64String(hashed), salt);
    }

    public static bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
    {
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: storedSalt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        return Convert.FromBase64String(hashed).SequenceEqual(storedHash);
    }
}