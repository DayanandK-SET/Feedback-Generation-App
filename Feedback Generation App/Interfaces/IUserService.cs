using Feedback_Generation_App.Models.DTOs;

namespace Feedback_Generation_App.Interfaces
{
    public interface IUserService
    {
        public Task<CheckUserResponseDto> CheckUser(CheckUserRequestDto request);

        public Task RegisterUser(RegisterUserDto request);
    }
}
