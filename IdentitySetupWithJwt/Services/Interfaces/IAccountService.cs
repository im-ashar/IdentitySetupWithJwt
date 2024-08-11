using IdentitySetupWithJwt.Utilities;
using IdentitySetupWithJwt.ViewModels;

namespace IdentitySetupWithJwt.Services.Interfaces
{
    public interface IAccountService
    {
        Task<MethodResult<JwtTokenResponseVM>> LoginAsync(LoginVM loginVM);
        Task<MethodResult<JwtTokenResponseVM>> RefreshTokenAsync(string accessToken, string refreshToken);
        Task<MethodResult<RegisterVM>> RegisterAsync(RegisterVM registerVM);
    }
}