using ItTms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItTms.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.Description)
            .IsRequired();

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.RejectionReason)
            .HasMaxLength(500);

        builder.Property(t => t.DueAt).IsRequired();
        builder.Property(t => t.ResolvedAt);
        builder.Property(t => t.ClosedAt);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        // Optimistic Concurrency Token
        builder.Property(t => t.ConcurrencyToken)
            .IsConcurrencyToken()
            .IsRequired();

        // Foreign Key Relationships
        builder.HasOne(t => t.Creator)
            .WithMany(u => u.CreatedTickets)
            .HasForeignKey(t => t.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedTo)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Status)
            .WithMany(s => s.Tickets)
            .HasForeignKey(t => t.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        // Covering & High-Performance Indexes
        // 1. Phục vụ UC05: lọc theo CreatorId và sắp xếp CreatedAt
        builder.HasIndex(t => new { t.CreatorId, t.CreatedAt })
            .HasDatabaseName("IX_Tickets_CreatorId_CreatedAt");

        // 2. Phục vụ UC13: Hàng đợi xử lý Staff: lọc theo AssignedToId và StatusId
        builder.HasIndex(t => new { t.AssignedToId, t.StatusId })
            .HasDatabaseName("IX_Tickets_AssignedToId_StatusId");

        // 3. Phục vụ UC17: Dashboard & Cảnh báo vi phạm SLA DueAt
        builder.HasIndex(t => new { t.StatusId, t.DueAt })
            .HasDatabaseName("IX_Tickets_StatusId_DueAt");

        // 4. Phục vụ tra cứu đa tiêu chí của Leader/Admin
        builder.HasIndex(t => new { t.StatusId, t.Priority, t.CreatedAt })
            .HasDatabaseName("IX_Tickets_StatusId_Priority_CreatedAt");
    }
}
