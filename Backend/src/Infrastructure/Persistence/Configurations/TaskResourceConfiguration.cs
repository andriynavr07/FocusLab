using FocusLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusLab.Infrastructure.Persistence.Configurations;

public class TaskResourceConfiguration : IEntityTypeConfiguration<TaskResource>
{
    public void Configure(EntityTypeBuilder<TaskResource> builder)
    {
        builder.ToTable("task_resources");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.TaskId).HasColumnName("task_id").IsRequired();
        builder.Property(r => r.Type).HasColumnName("type")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.Path).HasColumnName("path").IsRequired();

        builder.HasOne(r => r.Task)
            .WithMany(t => t.Resources)
            .HasForeignKey(r => r.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.TaskId).HasDatabaseName("ix_task_resources_task_id");
    }
}
