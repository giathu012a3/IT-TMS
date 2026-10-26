
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItTms.Data.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("Feedbacks");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedOnAdd();

        builder.Property(f => f.Rating)
            .IsRequired();

        builder.Property(f => f.Comment)
            .HasMaxLength(1000);

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        // Quan hệ 1 - 0..1 với Ticket
        builder.HasOne(f => f.Ticket)
            .WithOne(t => t.Feedback)
            .HasForeignKey<Feedback>(f => f.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique Index trên TicketId
        builder.HasIndex(f => f.TicketId)
            .IsUnique()
            .HasDatabaseName("IX_Feedbacks_TicketId");
    }
}
