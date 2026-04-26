using Aparesk.Eskineria.Core.Auth.Entities;
using Aparesk.Eskineria.Core.Auth.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aparesk.Eskineria.Core.Auth.Data;

public class EskineriaIdentityDbContext<TUser, TRole, TKey> : IdentityDbContext<TUser, TRole, TKey>
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
{
    public EskineriaIdentityDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        if (typeof(TUser) == typeof(EskineriaUser))
        {
            var encryptedRequiredStringConverter = new ValueConverter<string, string>(
                value => AuthSensitiveDataProtector.Encrypt(value) ?? string.Empty,
                value => AuthSensitiveDataProtector.Decrypt(value) ?? string.Empty);
            var encryptedOptionalStringConverter = new ValueConverter<string?, string?>(
                value => AuthSensitiveDataProtector.Encrypt(value),
                value => AuthSensitiveDataProtector.Decrypt(value));

            builder.Entity<EskineriaUser>(entity =>
            {
                entity.Property(x => x.FirstName).HasConversion(encryptedRequiredStringConverter);
                entity.Property(x => x.LastName).HasConversion(encryptedRequiredStringConverter);
                entity.Property(x => x.ProfilePicture).HasConversion(encryptedOptionalStringConverter);
            });
        }

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(200);
            entity.Property(e => e.JwtId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(1024);
            entity.Property(e => e.RevocationReason).HasMaxLength(200);
            entity.HasIndex(e => e.Token).IsUnique(); 
        });
    }
}

// Default convenience class using Guid
public class EskineriaIdentityDbContext : EskineriaIdentityDbContext<EskineriaUser, EskineriaRole, Guid>
{
    public EskineriaIdentityDbContext(DbContextOptions<EskineriaIdentityDbContext> options) : base(options)
    {
    }
    
    // Protected constructor for derived context
    protected EskineriaIdentityDbContext(DbContextOptions options) : base(options)
    {
    }
}
