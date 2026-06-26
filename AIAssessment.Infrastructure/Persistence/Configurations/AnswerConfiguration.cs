using AIAssessment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIAssessment.Infrastructure.Persistence.Configurations;

public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.HasKey(a => a.Id);
         
        // Nullable — MCQ answers won't have text, Descriptive won't have OptionId
        builder.Property(a => a.SelectedOptionId);
        builder.Property(a => a.AnswerText).HasMaxLength(4000);

        builder.Property(a => a.Score)
            .IsRequired()
            .HasDefaultValue(0);

        // Answer → Question:
        // Restrict (not cascade) because deleting a Question while
        // Answers reference it would silently corrupt submission history.
        // You must manually delete answers before deleting a question.
        builder.HasOne(a => a.Question)
            .WithMany()
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Answers");
    }
}