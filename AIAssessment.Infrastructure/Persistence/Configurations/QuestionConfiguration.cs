using AIAssessment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIAssessment.Infrastructure.Persistence.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.QuestionText).IsRequired().HasMaxLength(2000);
        builder.Property(q => q.MaxMarks).IsRequired();
        builder.Property(q => q.ModelAnswer).HasMaxLength(4000);

        builder.Property(q => q.QuestionType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Navigation(q => q.Options)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_options");

        builder.HasMany(q => q.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}