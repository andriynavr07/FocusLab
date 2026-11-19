using FocusLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusLab.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        builder.HasKey(project => project.Id);
        builder.Property(project => project.Id).HasColumnName("id");
        builder.Property(project => project.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(project => project.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(project => project.Color).HasColumnName("color").HasMaxLength(20).IsRequired();
        builder.HasIndex(project => new { project.AccountId, project.Name })
            .IsUnique()
            .HasDatabaseName("ux_projects_account_name");
        builder.HasOne(project => project.Account)
            .WithMany(account => account.Projects)
            .HasForeignKey(project => project.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
