using FocusLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusLab.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
        

        builder.Property(a => a.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.HasIndex(a => a.Email).IsUnique().HasDatabaseName("ux_accounts_email");
        builder.Property(a => a.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();

        builder.Property(a => a.CreatedAt).HasColumnName("created_at")
            .HasConversion(new UtcDateTimeConverter()).IsRequired();

        // Локальний акаунт: застосунок офлайн, але tasks.account_id обов’язковий за моделлю даних.
        builder.HasData(new
        {
            Id = Account.LocalAccountId,
            Username = "local",
            Email = string.Empty,
            PasswordHash = string.Empty,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
