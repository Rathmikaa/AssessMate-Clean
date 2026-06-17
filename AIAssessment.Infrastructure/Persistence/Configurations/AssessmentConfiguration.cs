using AIAssessment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIAssessment.Infrastructure.Persistence.Configurations;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("Assessments");
        builder.HasKey(a => a.Id);
         
        builder.Property(a => a.Title).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Description).HasMaxLength(1000);
        builder.Property(a => a.DurationMinutes).IsRequired();
        builder.Property(a => a.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(a => a.CreatedAt).IsRequired();

        // Tell EF Core which backing field to use for each navigation collection.
        // Without this, EF Core uses the public property name to guess —
        // and it cannot populate a protected field it doesn't know about.
        builder.Navigation(a => a.Questions)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_questions");

        builder.Navigation(a => a.Submissions)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_submissions");

        builder.HasMany(a => a.Questions)
            .WithOne(q => q.Assessment)
            .HasForeignKey(q => q.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Submissions)
            .WithOne(s => s.Assessment)
            .HasForeignKey(s => s.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}