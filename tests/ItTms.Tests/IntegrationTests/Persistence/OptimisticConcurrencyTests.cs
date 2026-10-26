using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Infrastructure.Persistence;
using ItTms.Infrastructure.Persistence.Interceptors;

namespace ItTms.Tests.IntegrationTests.Persistence;

public class OptimisticConcurrencyTests : IDisposable
{
    private readonly string _dbFileName;
    private readonly DbContextOptions<ItTmsDbContext> _options;

    public OptimisticConcurrencyTests()
    {
        _dbFileName = $"test_concurrency_{Guid.NewGuid():N}.db";
        var connectionString = $"Data Source={_dbFileName};Cache=Shared";

        var optionsBuilder = new DbContextOptionsBuilder<ItTmsDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        optionsBuilder.AddInterceptors(new ConcurrencyTokenInterceptor());

        _options = optionsBuilder.Options;

        // Khởi tạo schema ban đầu và seed TicketStatus.New
        using var initContext = new ItTmsDbContext(_options);
        initContext.Database.Migrate();
        initContext.TicketStatuses.Add(TicketStatus.Create(TicketStatus.New, "New", "Mới tạo", "badge bg-primary", 1));
        initContext.SaveChanges();
    }

    [Fact]
    public async Task ConcurrentUpdates_WhenSecondUserModifiesStaleTicket_ThrowsDbUpdateConcurrencyException()
    {
        int ticketId;
        Guid initialToken;

        // 1. Tạo ticket mẫu
        using (var setupContext = new ItTmsDbContext(_options))
        {
            var user = User.Create("author", "author@local", "hash", "Author", UserRole.User).Value;
            setupContext.Users.Add(user);
            await setupContext.SaveChangesAsync();

            var ticket = Ticket.Create("Test Concurrency", "Description", TicketPriority.Medium, TicketCategory.Software, user.Id).Value;
            setupContext.Tickets.Add(ticket);
            await setupContext.SaveChangesAsync();

            ticketId = ticket.Id;
            initialToken = ticket.ConcurrencyToken;
        }

        // 2. Hai phiên làm việc song song cùng đọc cùng 1 ticket
        using var context1 = new ItTmsDbContext(_options);
        using var context2 = new ItTmsDbContext(_options);

        var ticketFromUser1 = await context1.Tickets.FindAsync(ticketId);
        var ticketFromUser2 = await context2.Tickets.FindAsync(ticketId);

        ticketFromUser1.Should().NotBeNull();
        ticketFromUser2.Should().NotBeNull();
        ticketFromUser1!.ConcurrencyToken.Should().Be(initialToken);
        ticketFromUser2!.ConcurrencyToken.Should().Be(initialToken);

        // 3. User 1 sửa tiêu đề và lưu thành công
        ticketFromUser1.Title = "Tiêu đề đã được User 1 sửa đổi";
        await context1.SaveChangesAsync();

        // ConcurrencyToken trong CSDL đã được cấp mới bởi Interceptor
        ticketFromUser1.ConcurrencyToken.Should().NotBe(initialToken);

        // 4. User 2 sửa tiêu đề dựa trên dữ liệu cũ và cố gắng lưu
        ticketFromUser2.Title = "Tiêu đề do User 2 cố gắng ghi đè";

        Func<Task> act = async () => await context2.SaveChangesAsync();

        // 5. EF Core phát hiện xung đột và ném DbUpdateConcurrencyException
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    public void Dispose()
    {
        using (var cleanupContext = new ItTmsDbContext(_options))
        {
            cleanupContext.Database.EnsureDeleted();
        }

        if (File.Exists(_dbFileName))
        {
            try { File.Delete(_dbFileName); } catch { /* ignore */ }
        }

        GC.SuppressFinalize(this);
    }
}
