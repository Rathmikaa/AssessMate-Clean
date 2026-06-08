using AIAssessment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIAssessment.Infrastructure.Persistence.Configurations
{
    public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
    {
        public void Configure(EntityTypeBuilder<Submission> builder)
        {
            builder.ToTable("Submissions");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.TotalScore).IsRequired().HasDefaultValue(0);
            builder.Property(s => s.StartedAt).IsRequired();
            builder.Property(s => s.SubmittedAt);

            builder.Property(s => s.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Navigation(s => s.Answers)
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasField("_answer");

            builder.Property(s => s.UserId).IsRequired();

 
            builder.HasIndex(s => s.UserId);

            builder.HasMany(s => s.Answers)
                .WithOne(a => a.Submission)
                .HasForeignKey(a => a.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}