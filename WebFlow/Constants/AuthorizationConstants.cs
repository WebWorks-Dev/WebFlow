namespace WebFlow.Models;

internal static class AuthorizationConstants
{
    public static string CantFindType(Type type) => $"Can't find model {type.Name} in the database!";
    public static string FailedToReadType(Type type) => $"Failed to read object {type.Name} check the object mapping.";

    public const string ClassMustHaveEmailDefined = "Email verification must contain an [EmailAddress] attribute somewhere within the class.";
    public const string ClassMustHaveRegTokenDefined = "Email verification must contain an [RegistrationToken] attribute somewhere within the class.";

    public const string InvalidParameters = "Invalid parameters provided";
    public const string PasswordFieldValueCantBeNull = "The password field being set is null, unable to hash it";
    public const string OnePasswordAttribute = "Each class can only have 1 password attribute";

    public const string EmailAuthenticationMustBeEnabled = "Email authentication must be enabled";
    
    public const string AccountAlreadyVerified = "Account is already verified";
    public const string AccountNotVerified = "Account must be first verified";
    public const string AccountAlreadyExists = "Account with specified unique attributes already exists";
    public const string AccountDoesntExist = "Account with specified validation attributes doesn't exists";

    public const string InvalidRegistrationToken = "Invalid registration token provided";

    public const string MissingWebFlowSessionId = "WebFlowSessionId was not found, account might not be logged in";
    
    public const string RegistrationTokenCantBeNull = "Registration token cant be null";
    
    public const string ClassMustHavePassTokenDefined = "Email verification must contain an [PasswordResetToken] attribute somewhere within the class.";
    public const string PasswordTokenExpired = "Password reset token is expired";
    public const string InvalidToken = "Invalid token";

    public const string RecaptchaMustBeEnabled = "Recaptcha must be enabled by calling UseRecaptcha(your_key);";
}