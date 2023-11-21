using WebFlow.Attributes;
using WebFlow.Logic.PasswordHashing;
using WebFlow.Passwords;

namespace WebFlow.Logic;

internal class PasswordHash : IPasswordHashService
{
    public string CreateHash(string password, HashType hashType)
    {
        return hashType switch
        {
            HashType.PBKDF2 => Pbkdf2.CreateHash(password),
            HashType.BCRYPT => BCrypt.Net.BCrypt.HashPassword(password),
            
            _ => throw new ArgumentOutOfRangeException(nameof(hashType), hashType, null)
        };
    }

    public bool ValidatePassword(HashType hashType, string password, string correctHash)
    {
        return hashType switch
        {
            HashType.PBKDF2 => Pbkdf2.ValidatePassword(password, correctHash),
            HashType.BCRYPT => BCrypt.Net.BCrypt.Verify(password, correctHash),
            
            _ => throw new ArgumentOutOfRangeException(nameof(hashType), hashType, null)
        };   
    }
}