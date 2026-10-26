using ItTms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItTms.Infrastructure.Persistence.Configurations;

public class TicketStatusConfiguration : IEntityTypeConfiguration<TicketStatus>
{
    public void Configure(EntityTypeBuilder<TicketStatus> builder)
    {
        builder.ToTable("TicketStatuses");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever(); // Dùng Id cố định (1..7)

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Label)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.ColorClass)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.Description)
            .HasMaxLength(255);

        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("IX_TicketStatuses_Name");
    }
}
