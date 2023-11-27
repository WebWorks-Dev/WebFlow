using System.ComponentModel.DataAnnotations;
using Mapster;

namespace WebFlowTest.Models;

[AdaptTo(typeof(User))]
public record AuthorizationRequest(string EmailAddress, string Password)
{
    public static explicit operator User(AuthorizationRequest request) =>
        request.Adapt<User>();
}

[AdaptTo(typeof(User))]
public record RegisterValidationRequest(Guid RegistrationToken)
{
    public static explicit operator User(RegisterValidationRequest request) =>
        request.Adapt<User>();
}

[AdaptTo(typeof(User))]
public record ResetPasswordRequest([MaxLength(64)] string PasswordResetToken)
{
    public static explicit operator User(ResetPasswordRequest request) =>
        request.Adapt<User>();
}