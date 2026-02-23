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
            try
            {
                var result = await _userService.CheckUser(userRequestDto);
                return Ok(result);
            }
            catch (UnAuthorizedException)
            {
                return Unauthorized("Invalid username or password");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

        }

        [HttpPost("Register")]
        public async Task<ActionResult> Register(RegisterUserDto request)
        {
            try
            {
                await _userService.RegisterUser(request);
                return Ok("User registered successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
