using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Infrastructure.Persistence;
using ItTms.Infrastructure.Persistence.Interceptors;
using ItTms.Infrastructure.Security;
using ItTms.Infrastructure.Seed;
using Xunit.Abstractions;

namespace ItTms.Tests.AdversarialTests;

/// <summary>
/// Adversarial Challenge Test Suite for Milestone 1 Domain Invariants, Security, and Seeder Idempotency.
/// Designed and executed by challenger_m1_2.
/// </summary>
public class DomainBoundaryAndSecurityAdversarialTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly PasswordHasherService _hasher;
    private readonly string _dbFileName;
    private readonly ItTmsDbContext _context;

    public DomainBoundaryAndSecurityAdversarialTests(ITestOutputHelper output)
    {
        _output = output;
        _hasher = new PasswordHasherService();
        _dbFileName = $"adversarial_test_{Guid.NewGuid():N}.db";

        var optionsBuilder = new DbContextOptionsBuilder<ItTmsDbContext>();
        optionsBuilder.UseSqlite($"Data Source={_dbFileName};Cache=Shared");
        optionsBuilder.AddInterceptors(new ConcurrencyTokenInterceptor());

        _context = new ItTmsDbContext(optionsBuilder.Options);
    }

    #region 1. Domain Boundary Limits: Negative Priority & Undefined Enum

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(999)]
    [InlineData(int.MinValue)]
    public void TicketCreate_WithNegativeOrUndefinedPriority_FailsValidation(int rawPriority)
    {
        var priority = (TicketPriority)rawPriority;

        // Act
        var result = Ticket.Create(
            title: "Test Boundary Priority",
            description: "Adversarial testing negative or invalid enum priority",
            priority: priority,
            category: TicketCategory.Hardware,
            creatorId: 1
        );

        result.IsFailure.Should().BeTrue("Mức độ ưu tiên ngoài dải định nghĩa Enum phải bị từ chối");
        result.Error.Code.Should().Be("TICKET_PRIORITY_INVALID");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TicketCreate_WithEmptyTitle_FailsValidation(string invalidTitle)
    {
        var result = Ticket.Create(invalidTitle, "Valid description", TicketPriority.Medium, TicketCategory.Software, 1);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TICKET_TITLE_REQUIRED");
    }

    [Fact]
    public void TicketCreate_WithTitleLength150_Succeeds()
    {
        var title150 = new string('A', 150);
        var result = Ticket.Create(title150, "Valid description", TicketPriority.Medium, TicketCategory.Software, 1);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void TicketCreate_WithTitleLength151_FailsValidation()
    {
        var title151 = new string('A', 151);
        var result = Ticket.Create(title151, "Valid description", TicketPriority.Medium, TicketCategory.Software, 1);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TICKET_TITLE_TOO_LONG");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public void TicketCreate_WithNonPositiveCreatorId_FailsValidation(int invalidCreatorId)
    {
        var result = Ticket.Create("Valid title", "Valid description", TicketPriority.Medium, TicketCategory.Software, invalidCreatorId);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TICKET_CREATOR_INVALID");
    }

    #endregion

    #region 2. Domain Boundary Limits: Attachment Size & ContentType

    [Fact]
    public void AttachmentCreate_WithExactly16MB_Succeeds()
    {
        const long exact16Mb = 16 * 1024 * 1024; // 16,777,216 bytes
        var result = Attachment.Create(
            ticketId: 1,
            fileName: "guid-name.png",
            originalFileName: "screenshot.png",
            contentType: "image/png",
            fileSize: exact16Mb
        );

        result.IsSuccess.Should().BeTrue();
        result.Value.FileSize.Should().Be(exact16Mb);
    }

    [Fact]
    public void AttachmentCreate_With16MBPlusOneByte_FailsValidation()
    {
        const long oversized = (16 * 1024 * 1024) + 1; // 16,777,217 bytes
        var result = Attachment.Create(
            ticketId: 1,
            fileName: "guid-name.png",
            originalFileName: "screenshot.png",
            contentType: "image/png",
            fileSize: oversized
        );

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ATTACH_FILE_TOO_LARGE");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1024)]
    [InlineData(long.MinValue)]
    public void AttachmentCreate_WithZeroOrNegativeSize_FailsValidation(long invalidSize)
    {
        var result = Attachment.Create(
            ticketId: 1,
            fileName: "guid-name.png",
            originalFileName: "screenshot.png",
            contentType: "image/png",
            fileSize: invalidSize
        );

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ATTACH_FILE_EMPTY");
    }

    [Fact]
    public void AttachmentCreate_WithNullContentType_FailsValidation()
    {
        var result = Attachment.Create(
            ticketId: 1,
            fileName: "guid-name.png",
            originalFileName: "screenshot.png",
            contentType: null!,
            fileSize: 1024
        );

        result.IsFailure.Should().BeTrue("Attachment.Create phải từ chối ContentType null hoặc rỗng thay vì ném NullReferenceException");
        result.Error.Code.Should().Be("ATTACH_CONTENT_TYPE_REQUIRED");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    #endregion

    #region 3. Domain Boundary Limits: User Email Format

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("missing_at_symbol.com")]
    [InlineData("@missingusername.com")]
    [InlineData("username@")]
    [InlineData("username@.com")]
    [InlineData("user name@domain.com")]
    [InlineData("user@domain..com")]
    public void UserCreate_WithMalformedEmailFormat_FailsValidation(string malformedEmail)
    {
        var result = User.Create(
            username: "testuser",
            email: malformedEmail,
            passwordHash: "validhash",
            fullName: "Test User",
            role: UserRole.User
        );

        result.IsFailure.Should().BeTrue($"Định dạng email bất hợp lệ '{malformedEmail}' phải bị từ chối");
        result.Error.Code.Should().Be("USER_EMAIL_INVALID");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UserCreate_WithEmptyEmail_FailsValidation(string emptyEmail)
    {
        var result = User.Create("testuser", emptyEmail, "validhash", "Test User", UserRole.User);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("USER_EMAIL_REQUIRED");
    }

    [Fact]
    public void UserCreate_WithEmailOver150Chars_FailsValidation()
    {
        var longEmail = string.Concat(new string('a', 142), "@domain.com"); // 153 chars
        var result = User.Create("testuser", longEmail, "validhash", "Test User", UserRole.User);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("USER_EMAIL_TOO_LONG");
    }

    #endregion

    #region 4. PasswordHasher: Salt Uniqueness, Unicode & Timing Resistance

    [Fact]
    public void PasswordHasher_SaltUniqueness_100HashesProduce100DistinctValues()
    {
        const string rawPassword = "P@ssw0rd_Empirical_Test#2026!";
        var hashes = new HashSet<string>();

        for (int i = 0; i < 100; i++)
        {
            var hash = _hasher.HashPassword(rawPassword);
            hashes.Add(hash);
        }

        // Assert 100% collision-free random salts
        hashes.Should().HaveCount(100, "every PBKDF2 hash should use an independent cryptographically random 128-bit salt");
    }

    [Theory]
    [InlineData("MậtKhẩu@HệThống_2026_TiếngViệt")]
    [InlineData("🔐CườngĐộBảoMậtTốiĐa🚀#8888")]
    [InlineData("日本語パスワード＠２０２６")]
    [InlineData("Пароль_Русский_Текст_123")]
    [InlineData("Special~!@#$%^&*()_+`-={}|[]\\:\";'<>?,./ ")]
    public void PasswordHasher_HandlesUnicodeAndSpecialCharacters_Correctly(string complexPassword)
    {
        // Act
        var hash = _hasher.HashPassword(complexPassword);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        _hasher.VerifyPassword(hash, complexPassword).Should().BeTrue();

        // Mutated candidate should fail
        _hasher.VerifyPassword(hash, string.Concat(complexPassword, "_wrong")).Should().BeFalse();
    }

    [Fact]
    public void PasswordHasher_WithLongPassword_1000Chars_CompletesWithoutCrash()
    {
        var longPassword = string.Concat(new string('Z', 1000), "@123!");
        var sw = Stopwatch.StartNew();
        var hash = _hasher.HashPassword(longPassword);
        sw.Stop();

        _output.WriteLine($"[PERFORMANCE] 1000-char password hashed in {sw.ElapsedMilliseconds} ms");
        hash.Should().NotBeNullOrWhiteSpace();
        _hasher.VerifyPassword(hash, longPassword).Should().BeTrue();
        _hasher.VerifyPassword(hash, longPassword.Substring(0, 999)).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AQAAAAIAAAAPAAAA")] // Truncated Identity V3 header with valid Base64 chars
    public void PasswordHasher_VerifyPassword_WithEmptyOrTruncatedBase64_ReturnsFalseWithoutThrowing(string candidateHash)
    {
        var result = _hasher.VerifyPassword(candidateHash, "AnyPassword123");
        result.Should().BeFalse();
    }

    [Fact]
    public void PasswordHasher_VerifyPassword_WithNonBase64MalformedHash_ReturnsFalseFailClosed()
    {
        const string nonBase64Hash = "invalid_base64_hash_payload!@#$";

        var isValid = _hasher.VerifyPassword(nonBase64Hash, "AnyPassword123");

        isValid.Should().BeFalse("PasswordHasherService must catch FormatException and fail-closed with false for malformed Base-64 hash strings");
    }

    [Fact]
    public void PasswordHasher_TimingResistance_StatisticalCheck()
    {
        const string password = "CorrectSuperSecretPassword@2026";
        var hash = _hasher.HashPassword(password);

        // Pre-warm JIT
        _hasher.VerifyPassword(hash, password);
        _hasher.VerifyPassword(hash, "WrongPasswordFirstCharMismatch");

        const int iterations = 30;
        var correctTimes = new List<double>(iterations);
        var wrongFirstCharTimes = new List<double>(iterations);
        var wrongLastCharTimes = new List<double>(iterations);

        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            _hasher.VerifyPassword(hash, password);
            sw.Stop();
            correctTimes.Add(sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            var wrongFirst = string.Concat("X", password.AsSpan(1));
            _hasher.VerifyPassword(hash, wrongFirst); // First char wrong
            sw.Stop();
            wrongFirstCharTimes.Add(sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            var wrongLast = string.Concat(password.AsSpan(0, password.Length - 1), "X");
            _hasher.VerifyPassword(hash, wrongLast); // Last char wrong
            sw.Stop();
            wrongLastCharTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        double avgCorrect = correctTimes.Average();
        double avgWrongFirst = wrongFirstCharTimes.Average();
        double avgWrongLast = wrongLastCharTimes.Average();

        _output.WriteLine($"[TIMING ANALYSIS] Avg Correct: {avgCorrect:F3} ms | Avg WrongFirst: {avgWrongFirst:F3} ms | Avg WrongLast: {avgWrongLast:F3} ms");

        // PBKDF2 compute dominates verification time; all paths must execute the key derivation before comparison
        // Difference between wrong first char and wrong last char should be minimal (< 30% discrepancy)
        double diffPercentage = Math.Abs(avgWrongFirst - avgWrongLast) / Math.Max(avgWrongFirst, avgWrongLast) * 100;
        _output.WriteLine($"[TIMING DISCREPANCY] Difference between wrong-first and wrong-last char: {diffPercentage:F2}%");
        diffPercentage.Should().BeLessThan(30.0, "PasswordHasher must exhibit fixed-time comparison resistant to character-position timing side channels");
    }

    #endregion

    #region 5. DataSeeder Idempotency & Data Preservation

    [Fact]
    public async Task DataSeeder_RepeatedExecutions_5Times_PreservesExactRecordCounts()
    {
        // 1. Run migration
        await _context.Database.MigrateAsync();

        var seeder = new DataSeeder(_context, _hasher, NullLogger<DataSeeder>.Instance);

        // Execute Seeder 5 consecutive times
        for (int i = 1; i <= 5; i++)
        {
            await seeder.SeedAsync();
        }

        // Assert record counts are strictly preserved
        var statusCount = await _context.TicketStatuses.CountAsync();
        var userCount = await _context.Users.CountAsync();
        var ticketCount = await _context.Tickets.CountAsync();
        var commentCount = await _context.Comments.CountAsync();
        var feedbackCount = await _context.Feedbacks.CountAsync();

        statusCount.Should().Be(7, "TicketStatuses must not duplicate");
        userCount.Should().Be(8, "Users must not duplicate");
        ticketCount.Should().Be(3, "Tickets must not duplicate");
        commentCount.Should().Be(2, "Comments must not duplicate");
        feedbackCount.Should().Be(1, "Feedbacks must not duplicate");
    }

    [Fact]
    public async Task DataSeeder_WhenCustomUserDataExists_PreservesExistingDataAndDoesNotOverwrite()
    {
        // 1. Run migration
        await _context.Database.MigrateAsync();

        var seeder = new DataSeeder(_context, _hasher, NullLogger<DataSeeder>.Instance);
        await seeder.SeedAsync();

        // 2. Modify an existing ticket and add a custom user
        var ticket1 = await _context.Tickets.FirstAsync(t => t.Title.Contains("không bật được nguồn"));
        ticket1.Title = "MODIFIED BY USER DURING PRODUCTION RUN";
        ticket1.Priority = TicketPriority.Critical;

        var customUser = User.Create(
            "custom_employee",
            "employee@ittms.local",
            _hasher.HashPassword("Emp@123"),
            "Custom Employee",
            UserRole.User,
            "R&D"
        ).Value;
        await _context.Users.AddAsync(customUser);
        await _context.SaveChangesAsync();

        // 3. Re-execute Seeder
        await seeder.SeedAsync();

        // 4. Verify modifications were NOT overwritten
        var reloadedTicket1 = await _context.Tickets.FirstAsync(t => t.Id == ticket1.Id);
        reloadedTicket1.Title.Should().Be("MODIFIED BY USER DURING PRODUCTION RUN");
        reloadedTicket1.Priority.Should().Be(TicketPriority.Critical);

        var customUserPersisted = await _context.Users.FirstOrDefaultAsync(u => u.Username == "custom_employee");
        customUserPersisted.Should().NotBeNull();
        customUserPersisted!.Department.Should().Be("R&D");

        // Total users should now be 9 (8 seeded + 1 custom)
        var totalUsers = await _context.Users.CountAsync();
        totalUsers.Should().Be(9);
    }

    #endregion

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
