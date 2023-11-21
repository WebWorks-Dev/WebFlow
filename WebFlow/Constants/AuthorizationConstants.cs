namespace WebFlow.Models;

internal static class AuthorizationConstants
{
    public static string CantFindType(Type type) => $"Can't find model {type.Name} in the database!";
    public static string FailedToReadType(Type type) => $"Failed to read object {type.Name}.";

    public const string ClassMustHaveEmailDefined = "Email verification must contain an [EmailAddress] attribute somewhere within the class.";
    public const string ClassMustHaveRegTokenDefined = "Email verification must contain an [RegistrationToken] attribute somewhere within the class.";

    public const string InvalidParameters = "Invalid parameters provided";
    public const string ValueCantBeNull = "The password field being set is null, unable to hash it";
    public const string OnePasswordAttribute = "Each class can only have 1 password attribute"; 
    
    public const string AccountNotVerified = "Account must be first verified";
    public const string AccountAlreadyExists = "Account with specified unique attributes already exists";

    public const string MissingWebFlowSessionId = "WebFlowSessionId was not found, account might not be logged in";
}