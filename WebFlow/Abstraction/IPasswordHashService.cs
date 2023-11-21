using Microsoft.Extensions.DependencyInjection;
using WebFlow.Attributes;
using WebFlow.Logic;
using WebFlow.Logic.PasswordHashing;
using WebFlow.Models;

namespace WebFlow.Passwords;

public interface IPasswordHashService
{
    /// <summary>
    /// Hashes a string into a password-safe hash
    /// </summary>
    /// <param name="password">The password</param>
    /// <param name="hashType">The requested hashType</param>
    /// <returns>The hashed password</returns>
    string CreateHash(string password, HashType hashType);
    
    /// <summary>
    /// Does validation on the password to see whether its correct or not
    /// </summary>
    /// <param name="type">The hashType</param>
    /// <param name="password">The raw password</param>
    /// <param name="correctHash">The correct hash from the database</param>
    /// <returns>Whether the password is valid or not</returns>
    bool ValidatePassword(HashType type, string password, string correctHash);
}

public static partial class RegisterWebFlowServices
{
    public static void RegisterPasswordHashing(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient(typeof(IPasswordHashService), typeof(PasswordHash));
    }
}