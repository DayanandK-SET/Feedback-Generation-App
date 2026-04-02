using Feedback_Generation_App.Models.DTOs;
using Feedback_Generation_App.Services;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Xunit;

namespace FeedbackBack_Unit_Tests
{
    public class TokenServiceTests
    {
        private TokenService CreateServiceWithValidKey()
        {
            var settings = new Dictionary<string, string>
            {
                { "Keys:Jwt", "THIS_IS_A_TEST_SECRET_KEY_123456789" }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings!)
                .Build();

            return new TokenService(configuration);
        }

        [Fact]
        public void Constructor_MissingSecretKey_ThrowsInvalidOperationException()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>())
                .Build();

            Assert.Throws<InvalidOperationException>(() =>
                new TokenService(configuration));
        }

        [Fact]
        public void CreateToken_ValidPayload_ReturnsValidJwtToken()
        {
            var service = CreateServiceWithValidKey();

            var payload = new TokenPayloadDto
            {
                UserId = 1,
                Username = "testuser",
                Role = "Admin"
            };

            var token = service.CreateToken(payload);

            Assert.NotNull(token);
            Assert.NotEmpty(token);

            // Validate token contents
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Assert.Equal("testuser",
                jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);

            Assert.Equal("Admin",
                jwtToken.Claims.First(c => c.Type == "role").Value);

            Assert.Equal("1",
                jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.NameId).Value);
        }
    }
}