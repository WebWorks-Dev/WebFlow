namespace WebFlow.Attributes;

/// <summary>
/// Issues a claim in the httpContext
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class AuthenticationClaimAttribute : Attribute
{
    public string ClaimName { get; set; }
    
    public AuthenticationClaimAttribute(string claimName)
    {
        ClaimName = claimName;
    }
}