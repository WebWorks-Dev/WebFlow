using System.ComponentModel.DataAnnotations;

namespace WebFlow.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AuthenticationFieldAttribute : Attribute
{
    public AuthenticationFieldAttribute()
    {
        
    }
}