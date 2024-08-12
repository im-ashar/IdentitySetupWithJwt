using IdentitySetupWithJwt.Services;
using IdentitySetupWithJwt.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetRefreshToken(string accessToken, string refreshToken) =>
            (await accountService.RefreshTokenAsync(accessToken, refreshToken))
            .Match(
                l => Problem(detail: l, statusCode: StatusCodes.Status401Unauthorized),
                Ok
            );

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
            return result.Match(
                l => Problem(detail: l, statusCode: StatusCodes.Status401Unauthorized),
                Ok
            );
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
            return result.Match(
                l => Problem(detail: l, statusCode: StatusCodes.Status400BadRequest),
                Ok
            );
        }
    }
}
