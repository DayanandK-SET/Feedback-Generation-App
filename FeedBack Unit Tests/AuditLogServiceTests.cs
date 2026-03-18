using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Feedback_Generation_App.Repositories;
using Feedback_Generation_App.Services;
using Microsoft.EntityFrameworkCore;

namespace FeedbackBack_Unit_Tests
{
    // ================================================================
    // AuditLogServiceTests
    // Tests: LogAsync, GetLogsAsync
    // Pattern: Real Repository + InMemory DB
    // ================================================================

    public class AuditLogServiceTests
    {
        private readonly FeedbackContext _context;
        private readonly IRepository<int, AuditLog> _auditLogRepository;
        private readonly AuditLogService _auditLogService;

        public AuditLogServiceTests()
        {
            var options = new DbContextOptionsBuilder<FeedbackContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FeedbackContext(options);
            _auditLogRepository = new Repository<int, AuditLog>(_context);
            _auditLogService = new AuditLogService(_auditLogRepository);
        }

        // ── Helper ───────────────────────────────────────────────────
        private async Task AddLog(
            string username = "testuser",
            string role = "Creator",
            string action = "Login",
            string? details = "Logged in",
            int? userId = 1,
            DateTime? createdAt = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Username = username,
                Role = role,
                Action = action,
                Details = details,
                IpAddress = "127.0.0.1",
                CreatedAt = createdAt ?? DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(log);
        }

        // ================================================================
        // LogAsync Tests
        // ================================================================

        [Fact]
        public async Task LogAsync_ValidData_SavesLogToDatabase()
        {
            // Act
            await _auditLogService.LogAsync(
                userId: 1,
                username: "john",
                role: "Creator",
                action: "Survey Created",
                details: "Created survey 'Feedback'",
                ipAddress: "192.168.1.1"
            );

            // Assert
            var saved = await _auditLogRepository.GetQueryable()
                .FirstOrDefaultAsync(l => l.Username == "john");

            Assert.NotNull(saved);
            Assert.Equal("john", saved!.Username);
            Assert.Equal("Creator", saved.Role);
            Assert.Equal("Survey Created", saved.Action);
            Assert.Equal("Created survey 'Feedback'", saved.Details);
            Assert.Equal("192.168.1.1", saved.IpAddress);
        }

        [Fact]
        public async Task LogAsync_NullUserId_SavesLogWithNullUserId()
        {
            // Act — anonymous login attempt has no userId
            await _auditLogService.LogAsync(
                userId: null,
                username: "unknownuser",
                role: "Unknown",
                action: "Login Failed",
                details: "Bad credentials",
                ipAddress: "10.0.0.1"
            );

            // Assert
            var saved = await _auditLogRepository.GetQueryable()
                .FirstOrDefaultAsync(l => l.Username == "unknownuser");

            Assert.NotNull(saved);
            Assert.Null(saved!.UserId);
        }

        [Fact]
        public async Task LogAsync_MultipleActions_AllSavedSeparately()
        {
            // Act
            await _auditLogService.LogAsync(1, "alice", "Creator", "Login", null, null);
            await _auditLogService.LogAsync(1, "alice", "Creator", "Survey Created", null, null);
            await _auditLogService.LogAsync(1, "alice", "Creator", "Survey Deleted", null, null);

            // Assert
            var count = await _auditLogRepository.GetQueryable()
                .CountAsync(l => l.Username == "alice");

            Assert.Equal(3, count);
        }

        [Fact]
        public async Task LogAsync_CreatedAtIsSetToCurrentUtcTime()
        {
            // Arrange
            var before = DateTime.UtcNow.AddSeconds(-1);

            // Act
            await _auditLogService.LogAsync(1, "timetest", "Creator", "Login", null, null);

            // Assert
            var saved = await _auditLogRepository.GetQueryable()
                .FirstAsync(l => l.Username == "timetest");

            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.True(saved.CreatedAt >= before && saved.CreatedAt <= after,
                "CreatedAt should be close to current UTC time");
        }

        // ================================================================
        // GetLogsAsync Tests
        // ================================================================

        [Fact]
        public async Task GetLogsAsync_NoFilters_ReturnsAllLogs()
        {
            // Arrange
            await AddLog("user1", action: "Login");
            await AddLog("user2", action: "Survey Created");
            await AddLog("user3", action: "Survey Deleted");

            var request = new GetAuditLogsRequestDto { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _auditLogService.GetLogsAsync(request);

            // Assert
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Logs.Count);
        }

        [Fact]
        public async Task GetLogsAsync_UsernameFilter_ReturnsMatchingLogs()
        {
            // Arrange
            await AddLog("alice", action: "Login");
            await AddLog("alice", action: "Survey Created");
            await AddLog("bob", action: "Login");

            var request = new GetAuditLogsRequestDto
            {
                PageNumber = 1,
                PageSize = 10,
                Username = "alice"
            };

            // Act
            var result = await _auditLogService.GetLogsAsync(request);

            // Assert — only alice's logs
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Logs, l => Assert.Equal("alice", l.Username));
        }

        [Fact]
        public async Task GetLogsAsync_UsernameFilter_IsCaseInsensitive()
        {
            // Arrange
            await AddLog("Charlie", action: "Login");

            var request = new GetAuditLogsRequestDto
            {
                PageNumber = 1,
                PageSize = 10,
                Username = "charlie"   // lowercase search
            };

            // Act
            var result = await _auditLogService.GetLogsAsync(request);

            // Assert — finds "Charlie" even with lowercase search
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task GetLogsAsync_ActionFilter_ReturnsMatchingLogs()
        {
            // Arrange
            await AddLog("user1", action: "Login");
            await AddLog("user2", action: "Survey Created");
            await AddLog("user3", action: "Survey Created");

            var request = new GetAuditLogsRequestDto
            {
                PageNumber = 1,
                PageSize = 10,
                Action = "Survey Created"
            };

            // Act
            var result = await _auditLogService.GetLogsAsync(request);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Logs, l => Assert.Equal("Survey Created", l.Action));
        }

        [Fact]
        public async Task GetLogsAsync_ActionFilter_PartialMatchWorks()
        {
            // Arrange
            await AddLog(action: "Survey Created");
            await AddLog(action: "Survey Deleted");
            await AddLog(action: "Login");

            var request = new GetAuditLogsRequestDto
            {
                PageNumber = 1,
                PageSize = 10,
                Action = "Survey"   // partial match
            };

            // Act
            var result = await _auditLogService.GetLogsAsync(request);

            // Assert — both Survey actions found
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetLogsAsync_FromDateFilter_ReturnsLogsAfterDate()
        {
            // Arrange — old log from 10 days ago
            await AddLog("olduser", createdAt: DateTime.UtcNow.AddDays(-10));

            // Recent log from today
            await AddLog("newuser", createdAt: DateTime.UtcNow);

            var request = new GetAuditLogsRequestDto
            {
                PageNumber = 1,
                PageSize = 10,
                FromDate = DateTime.UtcNow.AddDays(-1)  // only last 1 day
            };

            // Act
            var result = await _auditLogService.GetLogsAsync(request);

            // Assert — only the recent log
            Assert.Equal(1, result.TotalCount);
            Assert.Equal("newuser", result.Logs[0].Username);
        }

        [Fact]
        public async Task GetLogsAsync_ToDateFilter_ReturnsLogsBeforeDate()
        {
            // Arrange
            await AddLog("olduser", createdAt: DateTime.UtcNow.AddDays(-5));
            await AddLog("newuser", createdAt: DateTime.UtcNow);

            var request = new GetAuditLogsRequestDto
            {
                PageNumber = 1,
                PageSize = 10,
                ToDate = DateTime.UtcNow.AddDays(-2)  // only logs older than 2 days
            };

            // Act
            var result = await _auditLogService.GetLogsAsync(request);

            // Assert — only old log
            Assert.Equal(1, result.TotalCount);
            Assert.Equal("olduser", result.Logs[0].Username);
        }

        [Fact]
        public async Task GetLogsAsync_Pagination_ReturnsCorrectPage()
        {
            // Arrange — add 6 logs
            for (int i = 1; i <= 6; i++)
                await AddLog($"user{i}", action: $"Action {i}");

            var request = new GetAuditLogsRequestDto
            {
                PageNumber = 2,
                PageSize = 3
            };

            // Act
            var result = await _auditLogService.GetLogsAsync(request);

            // Assert
            Assert.Equal(6, result.TotalCount);   // total across all pages
            Assert.Equal(3, result.Logs.Count);    // only 3 on this page
            Assert.Equal(2, result.PageNumber);
        }

        [Fact]
        public async Task GetLogsAsync_OrderedByNewestFirst()
        {
            // Arrange
            await AddLog("first",  createdAt: DateTime.UtcNow.AddHours(-2));
            await AddLog("second", createdAt: DateTime.UtcNow.AddHours(-1));
            await AddLog("third",  createdAt: DateTime.UtcNow);

            var request = new GetAuditLogsRequestDto { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _auditLogService.GetLogsAsync(request);

            // Assert — newest log is first
            Assert.Equal("third", result.Logs[0].Username);
            Assert.Equal("first", result.Logs[2].Username);
        }
    }
}
