namespace WebFlow.Attributes;

/// <summary>
/// Represents an attribute that indicates that a object requires email verification.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class RequiresEmailVerificationAttribute : Attribute
{
    public RequiresEmailVerificationAttribute()
    {
    }
}

/// <summary>
/// This attribute is used to mark a property as a registration token.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class RegistrationTokenAttribute : Attribute
{
    public RegistrationTokenAttribute()
    {
    }
}
