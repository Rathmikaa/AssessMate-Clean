using AIAssessment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIAssessment.Infrastructure.Persistence.Configurations
{
    public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
    {
        public void Configure(EntityTypeBuilder<UserToken> builder)
        {
            builder.ToTable("UserTokens");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Token).IsRequired().HasMaxLength(2000);
            builder.Property(t => t.JwtId).IsRequired().HasMaxLength(100);
            builder.Property(t => t.CreatedAt).IsRequired();
            builder.Property(t => t.ExpiresAt).IsRequired();

            // Fast lookup on every authenticated request
            builder.HasIndex(t => t.JwtId).IsUnique();

            // ONE TOKEN PER USER — enforced at DB level.
            // Application code calls RevokeAllForUserAsync before inserting,
            // but if there's ever a race condition the DB unique constraint catches it.
            builder.HasIndex(t => t.UserId).IsUnique();
        }
    }
}