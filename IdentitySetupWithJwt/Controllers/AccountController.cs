using IdentitySetupWithJwt.Services.Interfaces;
using IdentitySetupWithJwt.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IdentitySetupWithJwt.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountController(IAccountService accountService) : ControllerBase
    {
        [HttpGet]
        [Authorize]
        public IActionResult GetDetails()
        {
            return Ok("You are Authorized");
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetRefreshToken(string accessToken, string refreshToken)
        {
            var result = await accountService.RefreshTokenAsync(accessToken, refreshToken);
            if (!result.IsSuccess)
            {
                return Problem(detail: result.ErrorMessage, statusCode: StatusCodes.Status401Unauthorized);
            }
            return Ok(result.Data);
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(errors);
            }
            var result = await accountService.LoginAsync(loginVM);
            if (!result.IsSuccess)
            {
                return Problem(detail: result.ErrorMessage, statusCode: StatusCodes.Status401Unauthorized);
            }
            return Ok(result.Data);
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(errors);
            }
            var result = await accountService.RegisterAsync(registerVM);
            if (!result.IsSuccess)
            {
                return Problem(detail: result.ErrorMessage, statusCode: StatusCodes.Status400BadRequest);
            }
            return Ok(result.Data);
        }
    }
}
