using System.ComponentModel.DataAnnotations;

namespace WebFlow.Attributes;

/// <summary>
/// Specifies that this value is checked upon authentication
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AuthenticationFieldAttribute : Attribute
{
    public AuthenticationFieldAttribute()
    {
        
    }
}