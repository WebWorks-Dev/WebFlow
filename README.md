# WebFlow
A SDK for ASP.Net making development easier


# Project Documentation

This document provides an overview of two essential interfaces and their implementations within the project.

# WebFlowAuthorizationService Documentation

This document provides an overview of the `IWebFlowAuthorizationService` interface and related components within the project.

## `IWebFlowAuthorizationService`

The `IWebFlowAuthorizationService` interface manages user authorization within the web flow.

### Methods

#### `RegisterUser<T>(DbContext dbContext, T authenticationObject)`

Registers a user in the database. Passwords are automatically hashed when provided.

```csharp
Result<T?> RegisterUser<T>(DbContext dbContext, T authenticationObject) where T : class;
```

#### `AuthenticateUserAsync<T>(DbContext dbContext, HttpContext httpContext, T authenticationObject)`

Authenticates the user based on provided attributes.

```csharp
Task<Result<T?>> AuthenticateUserAsync<T>(DbContext dbContext, HttpContext httpContext, T authenticationObject) where T : class;
```

#### `LogoutUser(HttpContext httpContext)`

Logs the user out and invalidates their session.

```csharp
Result LogoutUser(HttpContext httpContext);
```

### Configuration Utilities

#### `RegisterAuthorizationService(IServiceCollection serviceCollection, Assembly executing, JwtConfig jwtConfig)`

Registers the authorization service based on provided configurations.

```csharp
public static void RegisterAuthorizationService(IServiceCollection serviceCollection, Assembly executing, JwtConfig jwtConfig);
```

#### `RegisterAuthorizationMiddlewares(IApplicationBuilder applicationBuilder, AuthorizationType authorizationType)`

Registers middlewares for authorization handling.

```csharp
public static void RegisterAuthorizationMiddlewares(IApplicationBuilder applicationBuilder, AuthorizationType authorizationType);
```

### Attributes

#### `AuthenticationClaimAttribute`

Issues a claim in the `httpContext`.

```csharp
[AuthenticationClaim("UserId")]
public Guid Id { get; set; }
```

#### `AuthenticationFieldAttribute`

Specifies that this value is checked upon authentication.

```csharp
[AuthenticationField]
public required string EmailAddress { get; set; }
```

#### `PasswordAttribute`

Specifies that the value is a password and needs to be hashed and checked upon authentication.

```csharp
[Password(HashType.PBKDF2)]
public required string Password { get; set; }
```

#### `UniqueAttribute`

Specifies that the value must not be a duplicate within the database.

```csharp
[Unique]
public required string EmailAddress { get; set; }
```

## Usage Examples

Here are snippets showcasing the usage of the `IWebFlowAuthorizationService` interface and related attributes:

### Interface Usage
```cs
public class User : IEntityTypeConfiguration<User>
{
    [AuthenticationClaim("UserId")] public Guid Id { get; set; }

    [Unique, AuthenticationClaim("EmailAddress"), AuthenticationField]
    public required string EmailAddress { get; set; }

    [Password(HashType.PBKDF2)] 
    public required string Password { get; set; }

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.Id)
            .HasDefaultValue(Guid.NewGuid());
    }
}

[AdaptTo(typeof(User))]
public record AuthorizationRequest(string EmailAddress, string Password)
{
    public static explicit operator User(AuthorizationRequest user) =>
        user.Adapt<User>();
}

[HttpPost("create")]
public async Task<IActionResult> CreateUser(AuthorizationRequest authorizationRequest)
{
    await using var context = await _dbContext.CreateDbContextAsync();

    Result<User?> result = _authorizationService.RegisterUser(context, (User)authorizationRequest);
    if (!result.IsSuccess)
        return BadRequest();
        
    await context.SaveChangesAsync();
        
    var cachedUser = (CachedUser)result.Unwrap()!;
    _genericCacheService.CacheObject(cachedUser);
        
    return Ok(result.Unwrap());
}

[HttpPost("login")]
public async Task<IActionResult> LoginUser(AuthorizationRequest authorizationRequest)
{
    await using var context = await _dbContext.CreateDbContextAsync();

    Result<User?> result = await _authorizationService.AuthenticateUserAsync(context, HttpContext, (User)authorizationRequest);
    if (!result.IsSuccess)
        return BadRequest();
        
    await context.SaveChangesAsync();

    return Ok();
}
```

## `IGenericCacheService`

This interface manages caching operations for various objects.

### Methods

#### `CacheObject<T>(T genericObject, TimeSpan? expiry = null, When when = When.Always)`

Caches an object into a Redis database.

```csharp
void CacheObject<T>(T genericObject, TimeSpan? expiry = null, When when = When.Always);
```

#### `FetchAll(Type genericObject)`

Fetches all cached variations of a given type.

```csharp
List<string> FetchAll(Type genericObject);
```

#### `FetchObject(Type genericObject, string key)`

Fetches a cached object based on a provided key.

```csharp
string? FetchObject(Type genericObject, string key);
```

#### `FetchNearest(Type genericObject, string guess)`

Fetches an item based on the nearest guess to the cache key.

```csharp
List<string> FetchNearest(Type genericObject, string guess);
```

#### `UpdateObject<T>(T genericObject)`

Updates the item within the cache.

```csharp
void UpdateObject<T>(T genericObject);
```

#### `DeleteObject(string key)`

Deletes an item from the cache.

```csharp
bool DeleteObject(string key);
```

#### `RefreshCacheAsync(List<Type> genericObjects)`

Refreshes the cache with the provided set of objects.

```csharp
Task RefreshCacheAsync(List<Type> genericObjects);
```

## Usage Examples

Here are snippets showcasing the usage of these interfaces:

### User Authorization

```csharp
// Creating a user
Result<User?> result = _authorizationService.RegisterUser(context, (User)authorizationRequest);

// Logging in a user
Result<User?> result = await _authorizationService.AuthenticateUserAsync(context, HttpContext, (User)authorizationRequest);

// Logging out a user
_authorizationService.LogoutUser(HttpContext);
```

### Caching Operations

```csharp
// Caching an object
_genericCacheService.CacheObject(cachedUser);

// Fetching a cached object
_genericCacheService.FetchObject(typeof(CachedUser), userId.ToString());

// Fetching all cached variations
_genericCacheService.FetchAll(typeof(CachedUser));

// Deleting an object from the cache
_genericCacheService.DeleteObject(key);

// Refreshing the cache
await _genericCacheService.RefreshCacheAsync(new List<Type> { typeof(CachedUser) });
```

Feel free to add more details or specific examples as needed!
```

This README.md file includes the documentation for the interfaces `IWebFlowAuthorizationService` and `IGenericCacheService`, along with snippets showcasing their usage. You can add this content to your GitHub repository to document these interfaces effectively.
