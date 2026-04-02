using Feedback_Generation_App.Services;
using System;
using Xunit;

namespace FeedbackBack_Unit_Tests
{
    public class PasswordServiceTests
    {
        private readonly PasswordService _passwordService;

        public PasswordServiceTests()
        {
            _passwordService = new PasswordService();
        }

        [Fact]
        public void HashPassword_NullPassword_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _passwordService.HashPassword(null!, null, out _));
        }

        [Fact]
        public void HashPassword_NewPassword_GeneratesHashAndHashKey()
        {
            byte[]? hashKey;

            var hash = _passwordService.HashPassword(
                "MyPassword123",
                dbHashKey: null,
                out hashKey);

            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.NotNull(hashKey);
            Assert.NotEmpty(hashKey!);
        }

        [Fact]
        public void HashPassword_ExistingHashKey_UsesSameKeyAndDoesNotGenerateNewKey()
        {
            byte[]? generatedKey;

            // First time (registration)
            var hash1 = _passwordService.HashPassword(
                "MyPassword123",
                dbHashKey: null,
                out generatedKey);

            Assert.NotNull(generatedKey);

            // Second time (login)
            byte[]? newKey;
            var hash2 = _passwordService.HashPassword(
                "MyPassword123",
                dbHashKey: generatedKey,
                out newKey);

            Assert.NotNull(hash2);
            Assert.Null(newKey); // ✅ IMPORTANT BRANCH
            Assert.Equal(hash1, hash2); // same password + same key → same hash
        }
    }
}
