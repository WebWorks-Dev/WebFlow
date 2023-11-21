using System;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebFlow.Attributes;
using WebFlow.Attributes.Cache;

namespace WebFlowTest;

public sealed class EntityFrameworkContext : DbContext
{
    public EntityFrameworkContext(DbContextOptions options) : base(options)
    {
        Database.EnsureCreated();
    }
    
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}

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

[AdaptFrom(typeof(User))]
public class CachedUser
{
    [CacheKey]
    public Guid Id { get; set; }

    public required string EmailAddress { get; set; }

    public static explicit operator CachedUser(User user) =>
        user.Adapt<CachedUser>();
}