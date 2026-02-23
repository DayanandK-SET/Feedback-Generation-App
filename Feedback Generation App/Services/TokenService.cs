using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Feedback_Generation_App.Services
{
    public class TokenService : ITokenService
    {
        SymmetricSecurityKey _key;
        public TokenService(IConfiguration configuration)
        {
            string secretKey = configuration["Keys:Jwt"] ?? throw new InvalidOperationException("Secret key not found in configuration.");
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        }
        public string CreateToken(TokenPayloadDto payloadDto)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, payloadDto.UserId.ToString()),
                new Claim(ClaimTypes.Name, payloadDto.Username),
                new Claim(ClaimTypes.Role, payloadDto.Role)
            };
            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = creds
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
