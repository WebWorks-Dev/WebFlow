namespace WebFlow.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class RequiresEmailVerificationAttribute : Attribute
{
    public RequiresEmailVerificationAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class RegistrationTokenAttribute : Attribute
{
    public RegistrationTokenAttribute()
    {
    }
}
