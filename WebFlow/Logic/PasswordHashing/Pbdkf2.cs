using System.Security.Cryptography;
using WebFlow.Passwords;

namespace WebFlow.Logic.PasswordHashing;

public static class Pbkdf2
{
    private const int SaltByteSize = 24;
    private const int HashByteSize = 24;
    private const int Pbkdf2Iterations = 1000;

    private const int IterationIndex = 0;
    private const int SaltIndex = 1;
    private const int Pbkdf2Index = 2;
    
    private static bool SlowEquals(byte[] a, byte[] b)
    {
        uint diff = (uint)a.Length ^ (uint)b.Length;
        for (int i = 0; i < a.Length && i < b.Length; i++)
        {
            diff |= (uint)(a[i] ^ b[i]);
        }

        return diff == 0;
    }
    
    private static byte[] CreateHash(string password, byte[] salt, int iterations, int outputBytes)
    {
        using DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA512);
        return pbkdf2.GetBytes(outputBytes);
    }
    
    public static string CreateHash(string password)
    {
        // Generate a random salt
        byte[] salt = new byte[SaltByteSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);

        byte[] hash = CreateHash(password, salt, Pbkdf2Iterations, HashByteSize);
        
        return Pbkdf2Iterations + ":" +
               Convert.ToBase64String(salt) + ":" +
               Convert.ToBase64String(hash);
    }
    
    public static bool ValidatePassword(string password, string correctHash)
    {
        string[] split = correctHash.Split(':');
        
        int iterations = Int32.Parse(split[IterationIndex]);
        byte[] salt = Convert.FromBase64String(split[SaltIndex]);
        byte[] hash = Convert.FromBase64String(split[Pbkdf2Index]);

        byte[] testHash = CreateHash(password, salt, iterations, hash.Length);
        return SlowEquals(hash, testHash);
    }
}