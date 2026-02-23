using Feedback_Generation_App.Models.DTOs;

namespace Feedback_Generation_App.Interfaces
{
    public interface ITokenService
    {
        public string CreateToken(TokenPayloadDto payloadDto);
    }
}
