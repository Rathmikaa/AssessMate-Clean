using AIAssessment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIAssessment.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");
            builder.HasKey(t => t.Id);
             
            // Base64(64 bytes) = 88 chars — 200 gives plenty of room
            builder.Property(t => t.Token).IsRequired().HasMaxLength(200);
            builder.Property(t => t.CreatedAt).IsRequired();
            builder.Property(t => t.ExpiresAt).IsRequired();

            // Fast lookup when client sends refresh token
            builder.HasIndex(t => t.Token).IsUnique();

            // ONE REFRESH TOKEN PER USER — same rule as access tokens
            builder.HasIndex(t => t.UserId).IsUnique();

            // No navigation to User entity — UserId just references AspNetUsers.Id
            // We don't need EF to load the User when working with refresh tokens
        }
    }
}