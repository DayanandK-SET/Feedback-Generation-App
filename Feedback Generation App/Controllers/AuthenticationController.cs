using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Feedback_Generation_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthenticationController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpPost("Login")]
        public async Task<ActionResult<CheckUserResponseDto>> Login(CheckUserRequestDto userRequestDto)
        {

            var result = await _userService.CheckUser(userRequestDto);
            return Ok(result);

        }

        [HttpPost("Register")]
        public async Task<ActionResult> Register(RegisterUserDto request)
        {
            await _userService.RegisterUser(request);
            return Ok(new { message = "User registered successfully" });
            //return Ok("User registered successfully");

        }
    }
}
