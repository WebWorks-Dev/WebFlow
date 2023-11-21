namespace WebFlow.Attributes;

public enum HashType
{
    None,
    PBKDF2,
    BCRYPT
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PasswordAttribute : Attribute
{
    public readonly HashType HashType;

    public PasswordAttribute(HashType hashType)
    {
        HashType = hashType;
    }
}

