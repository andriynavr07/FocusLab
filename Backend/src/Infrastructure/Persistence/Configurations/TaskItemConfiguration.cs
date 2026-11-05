using FocusLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusLab.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(t => t.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
        builder.Property(t => t.Description).HasColumnName("description");

        builder.Property(t => t.Deadline).HasColumnName("deadline")
            .HasConversion(new NullableUtcDateTimeConverter());

        builder.Property(t => t.Priority).HasColumnName("priority")
            .HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(t => t.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(t => t.CreatedAt).HasColumnName("created_at")
            .HasConversion(new UtcDateTimeConverter()).IsRequired();

        builder.Property(t => t.CompletedAt).HasColumnName("completed_at")
            .HasConversion(new NullableUtcDateTimeConverter());

        builder.HasOne(t => t.Account)
            .WithMany(a => a.Tasks)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Status).HasDatabaseName("ix_tasks_status");
    }
}
