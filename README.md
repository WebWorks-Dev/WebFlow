# WebFlow
A SDK for ASP.Net making development easier

The project requires minimal to no database changes in-order to work

### Extra
If for any reason the documenation below isn't clear we have a fully working example within [WebFlowTest](https://github.com/WebWorks-Dev/WebFlow/tree/master/WebFlowTest)

[All examples](https://github.com/WebWorks-Dev/WebFlow/blob/master/WebFlowTest/Controllers/TestController.cs)

# Project Documentation

This document provides an overview of two essential interfaces and their implementations within the project.

---

# IWebFlowAuthorizationService Interface

The `IWebFlowAuthorizationService` interface defines methods for user authentication, registration, session management, and password-related operations within a web application.

## Registration

### `RegisterAuthorizationService(IServiceCollection serviceCollection, Assembly executing, JwtConfig jwtConfig)`

Registers the authorization service based on provided configurations.

```csharp
var jwtConfig = new JwtConfig
{
    // Initialize your JwtConfig properties here
    Issuer = "your_issuer",
    Audience = "your_audience",
    Key = "abcdefghijklmnoprsquvxyz123456789",
    Duration = DateTime.UtcNow.AddMinutes(120)
};

builder.Services.RegisterAuthorizationService(executingAssembly, jwtConfig);
```

### `RegisterAuthorizationMiddlewares(IApplicationBuilder applicationBuilder, AuthorizationType authorizationType)`

Registers middlewares for authorization handling.

```csharp
app.RegisterAuthorizationMiddlewares(AuthorizationType.Jwt);
```

### `void UseEmailVerification(Assembly assembly)`

Tells WebFlow that users require email validation (Requires [RegistrationToken] and [PasswordResetToken] attributes to be defined in database classes)

```cs
builder.Services.UseEmailVerification(executingAssembly);
```

## Models

```
public class JwtConfig
{
    public string Key { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public DateTime Duration { get; set; }
}

public enum HashType
{
    None,
    PBKDF2,
    BCRYPT
}

//Sessions are not yet fully supported
public enum AuthorizationType
{
    Jwt
}
```

## Attributes

### `AuthenticationClaimAttribute`

Issues a claim in the `httpContext`.

```csharp
[AuthenticationClaim("UserId")]
public Guid Id { get; set; }
```

### `AuthenticationFieldAttribute`

Specifies that this value is checked upon authentication.

```csharp
[AuthenticationField]
public required string EmailAddress { get; set; }
```

### `PasswordAttribute`

Specifies that the value is a password and needs to be hashed and checked upon authentication.

```csharp
[Password(HashType.PBKDF2)]
public required string Password { get; set; }
```

### `UniqueAttribute`

Specifies that the value must not be a duplicate within the database.

```csharp
[Unique]
public required string EmailAddress { get; set; }
```

### `RequiresEmailVerificationAttribute`

Specifies that a class requires email verification

#### Remarks
Puttting this attribute on a class requires [RegistrationToken] and [PasswordResetToken] attributes to be defined in database classes

```cs
[RequiresEmailVerification]
public class User : IEntityTypeConfiguration<User>
```

### `RegistrationTokenAttribute`

This attribute is used to mark a property as a registration token.

```cs
[RegistrationToken]
public Guid RegistrationToken { get; set; }
```

### `PasswordResetTokenAttribute`

Specifies that a property represents a password reset token.
```cs
[PasswordResetToken]
[MaxLength(64)]
public string? PasswordResetToken { get; set; }
```
 
## Methods

### `RegisterUser<T>`
Registers a user into the authentication database with hashed password storage.

- **Parameters**:
  - `dbContext`: Database context used for user registration operation.
  - `authenticationObject`: Object containing user information to be registered.
- **Return Type**: `Result<T?>`
  - Result of the registration operation indicating success or failure along with the registered user object.
- **Constraints**: `T` must be a class.

### `AuthenticateUserAsync<T>`
Authenticates a user based on provided credentials.

- **Parameters**:
  - `dbContext`: Database context used for authentication process.
  - `httpContext`: HttpContext of the caller endpoint used for issuing the resulting authorization cookie.
  - `authenticationObject`: Object containing user credentials for authentication.
- **Return Type**: `Task<Result<T?>>`
  - Task leading to a Result of the authentication operation indicating success or failure along with the authenticated user object.
- **Constraints**: `T` must be a class.

### `LogoutUser`
Terminates the authenticated user's session, revoking their current authentication cookie.

- **Parameters**:
  - `httpContext`: HttpContext of the caller endpoint from which the user's cookie is extracted.
- **Return Type**: `Result`
  - Result indicating the success or failure of the logout operation.

### `ValidateRegistrationToken<T>`
Validates a user's registration token. Usable only when `UseEmailVerification` is enabled.

- **Parameters**:
  - `dbContext`: Database context used for the validation process.
  - `authenticationObject`: Object containing the user's registration token to validate.
- **Return Type**: `Result`
  - Result indicating the success or failure of the validation operation.
- **Constraints**: `T` must be a class.

#### Remarks
Class must contain a defined `RegistrationToken`
```cs
    [RegistrationToken]
    public Guid RegistrationToken { get; set; }
```
	
### `UpdatePassword<T>`
If `newPassword` is not provided, it will generate a `PasswordResetToken` with an expiration of 15m.
Else it will update the password based on the `[Password]` attribute parameters of the object.

- **Parameters**:
  - `dbContext`: Database context used for password update operation.
  - `authenticationObject` (optional): Object containing the authenticated user's information.
  - `newPassword` (optional): New password for the authenticated user. If null, a new password is automatically generated.
- **Return Type**: `Result<T?>`
  - Result indicating success or failure along with the updated user object.
- **Constraints**: `T` must be a class.

#### Remarks
Class must contain a defined `PasswordResetToken`
```cs
    [PasswordResetToken]
    [MaxLength(64)]
    public string? PasswordResetToken { get; set; }
```

---

---

# IGenericCacheService Interface

The `IGenericCacheService` interface defines methods to interact with a generic cache, providing functionalities for storing, retrieving, updating, and clearing cached objects.

## Registration

### `RegisterCachingService(this IServiceCollection serviceCollection, Assembly assembly, string connectionString)`

Registers the generic caching service and connects to the redis cache

```csharp
builder.Services.RegisterCachingService(executingAssembly, "127.0.0.1:6379");
```

## Attributes

### `CacheKeyAttribute`

Specifies the cache key, 
The key will be the "ClassName:Key-Value"

```csharp
[CacheKey]
public Guid Id { get; set; }
```

## Methods

### `CacheObject<T>`
Stores an object into a Redis database cache with a designated key construction.

- **Parameters**:
  - `genericObject`: Object instance to be stored into the cache.
  - `expiry` (optional): Specifies the duration for which the object should remain in the cache. Default value is null indicating no expiry time.
  - `when` (optional): Indicates the scenarios where this operation should be performed. Default is set to always.
- **Type Parameter**: `T`
  - Type of the object to be cached. It should be serializable.

### `FetchAll`
Retrieves all cached instances of the specified type from the cache.

- **Parameters**:
  - `genericObject`: Type of the objects to be fetched from the cache.
- **Return Type**: `List<string>`
  - List of string representations of all cached instances of the specified type.

### `FetchObject`
Retrieves a specific object from the cache using its associated key.

- **Parameters**:
  - `genericObject`: Type of the object to be fetched.
  - `key`: Unique key associated with the object in the cache.
- **Return Type**: `string?`
  - The cached object serialized as a string if found, otherwise null.

### `FetchNearest`
Fetches objects from the cache that have key values closely resembling the provided guess.

- **Parameters**:
  - `genericObject`: Type of the objects to be fetched.
  - `guess`: Estimate of the cache key associated with the desired objects.
- **Return Type**: `List<string>`
  - List of objects serialized as strings that have keys resembling the provided guess.

### `UpdateObject<T>`
Updates a specific object within the cache.

- **Parameters**:
  - `genericObject`: Updated version of the object to be stored into the cache.
- **Type Parameter**: `T`
  - Type of the object to be updated in the cache. It should be serializable.

### `DeleteObject`
Deletes a specific object from the cache using its associated key.

- **Parameters**:
  - `key`: Unique key associated with the object in the cache.
- **Return Type**: `bool`
  - True if deletion was successful, false otherwise.

### `RefreshCacheAsync`
Clears and repopulates the cache with a new set of objects asynchronously.

- **Parameters**:
  - `genericObjects`: List of new objects to be stored into cache after clearance.
- **Return Type**: `Task`
  - Task representing the asynchronous operation of cache refreshing.

---


---

# IEmailService Interface

The `IEmailService` interface encapsulates methods for sending emails synchronously and asynchronously within your application.

## Registration

### `RegisterEmailService(this IServiceCollection serviceCollection, string connectionString, string email, string password)`

Registers the email service and connects to the email server

```csharp
builder.Services.RegisterEmailService("dns:port", "email@email.com", "password");
```

## Methods

### `SendOutEmail`
Sends an email to a recipient.

- **Parameters**:
  - `sender`: The mailbox address from which the email will be sent.
  - `recipientEmail`: Email address of the recipient.
  - `subject`: The subject line of the email.
  - `body`: The main content of the email.
  - `isHtml` (optional): Flag indicating whether the body content is HTML or not (defaults to false).
- **Return Type**: `Result`
  - Result object indicating the status of the email sending operation.

### `SendOutEmail<T>`
Sends a templated email to a recipient.

- **Parameters**:
  - `sender`: The mailbox address from which the email will be sent.
  - `recipientEmail`: Email address of the recipient.
  - `subject`: The subject line of the email.
  - `templateObject`: An object that contains variables to be replaced in the HTML template.
  - `htmlContent`: The HTML template for the email body.
- **Type Parameter**:
  - `T`: The type of the template object.
- **Return Type**: `Result`
  - Result object indicating the status of the email sending operation.

### `SendOutEmailAsync`
Asynchronously sends an email to a recipient.

- **Parameters**:
  - `sender`: The mailbox address from which the email will be sent.
  - `recipientEmail`: Email address of the recipient.
  - `subject`: The subject line of the email.
  - `body`: The main content of the email.
  - `isHtml` (optional): Flag indicating whether the body content is HTML or not (defaults to false).
- **Return Type**: `Task<Result>`
  - Task representing the asynchronous email operation, containing a result object indicating the status of the email sending operation.

### `SendOutEmailAsync<T>`
Asynchronously sends a templated email to a recipient.

- **Parameters**:
  - `sender`: The mailbox address from which the email will be sent.
  - `recipientEmail`: Email address of the recipient.
  - `subject`: The subject line of the email.
  - `templateObject`: An object that contains variables to be replaced in the HTML template.
  - `htmlContent`: The HTML template for the email body.
- **Type Parameter**:
  - `T`: The type of the template object.
- **Return Type**: `Task<Result>`
  - Task representing the asynchronous email operation, containing a result object indicating the status of the email sending operation.

---


---

# IPasswordHashService Interface

The `IPasswordHashService` interface provides methods to hash passwords securely and validate password correctness.

## Registration

### `RegisterPasswordHashing(this IServiceCollection serviceCollection)`

Registers the password hashing service

```csharp
builder.Services.RegisterPasswordHashing();
```

## Methods

### `CreateHash`
Hashes a string into a password-safe hash.

- **Parameters**:
  - `password`: The password to be hashed.
  - `hashType`: The requested hashType.
- **Return Type**: `string`
  - The hashed password.

### `ValidatePassword`
Validates a password to determine correctness.

- **Parameters**:
  - `type`: The hashType used.
  - `password`: The raw password to validate.
  - `correctHash`: The correct hash retrieved from the database.
- **Return Type**: `bool`
  - Whether the password is valid or not.

---
