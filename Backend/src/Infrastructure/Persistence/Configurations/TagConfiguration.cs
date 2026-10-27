using FocusLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusLab.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(t => t.Color).HasColumnName("color").HasMaxLength(20);

        builder.HasOne(t => t.Account)
            .WithMany(a => a.Tags)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
