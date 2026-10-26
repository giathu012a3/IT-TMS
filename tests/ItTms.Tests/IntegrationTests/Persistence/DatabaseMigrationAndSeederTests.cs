using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Infrastructure.Persistence;
using ItTms.Infrastructure.Persistence.Interceptors;
using ItTms.Infrastructure.Security;
using ItTms.Infrastructure.Seed;

namespace ItTms.Tests.IntegrationTests.Persistence;

public class DatabaseMigrationAndSeederTests : IDisposable
{
    private readonly string _dbFileName;
    private readonly ItTmsDbContext _context;
    private readonly PasswordHasherService _hasher;

    public DatabaseMigrationAndSeederTests()
    {
        _dbFileName = $"test_migration_{Guid.NewGuid():N}.db";
        var connectionString = $"Data Source={_dbFileName};Cache=Shared";

        var optionsBuilder = new DbContextOptionsBuilder<ItTmsDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        optionsBuilder.AddInterceptors(new ConcurrencyTokenInterceptor());

        _context = new ItTmsDbContext(optionsBuilder.Options);
        _hasher = new PasswordHasherService();
    }

    [Fact]
    public async Task MigrateAndSeed_ExecutesSuccessfullyAndPopulatesAllRequiredData()
    {
        // 1. Thực thi Migration
        await _context.Database.MigrateAsync();

        // 2. Thực thi Seeder
        var seeder = new DataSeeder(_context, _hasher, NullLogger<DataSeeder>.Instance);
        await seeder.SeedAsync();

        // 3. Kiểm tra 7 TicketStatuses
        var statuses = await _context.TicketStatuses.OrderBy(s => s.Id).ToListAsync();
        statuses.Should().HaveCount(7);
        statuses.Select(s => s.Name).Should().ContainInOrder(
            "New", "Assigned", "InProgress", "Waiting", "Resolved", "Closed", "Rejected"
        );

        // 4. Kiểm tra 8 Users
        var users = await _context.Users.ToListAsync();
        users.Should().HaveCount(8);

        // Kiểm tra tài khoản Admin
        var admin = users.FirstOrDefault(u => u.Username == "admin");
        admin.Should().NotBeNull();
        admin!.Role.Should().Be(UserRole.Admin);
        admin.IsActive.Should().BeTrue();
        _hasher.VerifyPassword(admin.PasswordHash, "Admin@123").Should().BeTrue();

        // Kiểm tra tài khoản Deactivated (Bảng 7.2 TC-01)
        var deactivated = users.FirstOrDefault(u => u.Username == "deactivated");
        deactivated.Should().NotBeNull();
        deactivated!.Role.Should().Be(UserRole.User);
        deactivated.Status.Should().Be(UserStatus.Inactive);
        deactivated.IsActive.Should().BeFalse();
        _hasher.VerifyPassword(deactivated.PasswordHash, "User@123").Should().BeTrue();

        // Kiểm tra tài khoản alias inactive_user
        var inactiveAlias = users.FirstOrDefault(u => u.Username == "inactive_user");
        inactiveAlias.Should().NotBeNull();
        inactiveAlias!.IsActive.Should().BeFalse();

        // 5. Kiểm tra 3 Demo Tickets
        var tickets = await _context.Tickets.Include(t => t.Comments).Include(t => t.Feedback).ToListAsync();
        tickets.Should().HaveCount(3);

        // Ticket 1: New
        var ticket1 = tickets.FirstOrDefault(t => t.StatusId == TicketStatus.New);
        ticket1.Should().NotBeNull();
        ticket1!.Priority.Should().Be(TicketPriority.High);
        ticket1.Category.Should().Be(TicketCategory.Hardware);

        // Ticket 2: InProgress kèm Comments
        var ticket2 = tickets.FirstOrDefault(t => t.StatusId == TicketStatus.InProgress);
        ticket2.Should().NotBeNull();
        ticket2!.AssignedToId.Should().NotBeNull();
        ticket2.Comments.Should().HaveCount(2);

        // Ticket 3: Closed kèm Feedback
        var ticket3 = tickets.FirstOrDefault(t => t.StatusId == TicketStatus.Closed);
        ticket3.Should().NotBeNull();
        ticket3!.Feedback.Should().NotBeNull();
        ticket3.Feedback!.Rating.Should().Be(5);
        ticket3.ClosedAt.Should().NotBeNull();

        // 6. Kiểm tra tính Idempotent của Seeder: Chạy lại lần 2 không gây lỗi và không nhân đôi dữ liệu
        await seeder.SeedAsync();

        var statusesAfterSecondRun = await _context.TicketStatuses.CountAsync();
        var usersAfterSecondRun = await _context.Users.CountAsync();
        var ticketsAfterSecondRun = await _context.Tickets.CountAsync();

        statusesAfterSecondRun.Should().Be(7);
        usersAfterSecondRun.Should().Be(8);
        ticketsAfterSecondRun.Should().Be(3);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();

        if (File.Exists(_dbFileName))
        {
            try { File.Delete(_dbFileName); } catch { /* ignore */ }
        }

        GC.SuppressFinalize(this);
    }
}
