using FocusLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusLab.Infrastructure.Persistence.Configurations;

public class FocusSessionConfiguration : IEntityTypeConfiguration<FocusSession>
{
    public void Configure(EntityTypeBuilder<FocusSession> builder)
    {
        builder.ToTable("focus_sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.TaskId).HasColumnName("task_id").IsRequired();
        builder.Property(s => s.StartDateTime).HasColumnName("start_date_time")
            .HasConversion(new UtcDateTimeConverter()).IsRequired();
        builder.Property(s => s.EstimatedMinutes).HasColumnName("estimated_minutes");
        builder.Property(s => s.DurationMinutes).HasColumnName("duration_minutes").IsRequired();
        builder.Property(s => s.IsCompleted).HasColumnName("is_completed").IsRequired();

        builder.HasOne(s => s.Task)
            .WithMany(t => t.FocusSessions)
            .HasForeignKey(s => s.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.StartDateTime).HasDatabaseName("ix_focus_sessions_start_date_time");
        builder.HasIndex(s => s.TaskId).HasDatabaseName("ix_focus_sessions_task_id");
    }
}
