namespace WebFlow.Attributes;

public enum HashType
{
    None,
    PBKDF2,
    BCRYPT
}

/// <summary>
/// Specifies that the value is a password and needs to be hashed and checked upon authentication
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PasswordAttribute : Attribute
{
    public readonly HashType HashType;

    public PasswordAttribute(HashType hashType)
    {
        HashType = hashType;
    }
}

/// <summary>
/// Specifies that a property represents a password reset token.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class PasswordResetTokenAttribute : Attribute
{
    public PasswordResetTokenAttribute()
    {
    }
}

