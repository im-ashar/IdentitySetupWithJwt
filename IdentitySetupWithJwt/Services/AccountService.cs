using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IdentitySetupWithJwt.Configurations;
using IdentitySetupWithJwt.Models;
using IdentitySetupWithJwt.Utilities;
using IdentitySetupWithJwt.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IdentitySetupWithJwt.Services;

public interface IAccountService
{
    Task<MethodResult<JwtTokenResponseVM>> LoginAsync(LoginVM loginVm);
    Task<MethodResult<JwtTokenResponseVM>> RefreshTokenAsync(string accessToken, string refreshToken);
    Task<MethodResult<RegisterVM>> RegisterAsync(RegisterVM registerVm);
}

public class AccountService : IAccountService
{
    private readonly JwtConfig _jwtConfig;
    private readonly SymmetricSecurityKey _key;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public AccountService(IOptions<JwtConfig> jwtConfigOptions, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _jwtConfig = jwtConfigOptions.Value;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<MethodResult<JwtTokenResponseVM>> LoginAsync(LoginVM loginVm)
    {
        var user = await _userManager.FindByEmailAsync(loginVm.Email);
        if (user == null)
        {
            return new MethodResult<JwtTokenResponseVM>.Failure("Invalid Email Or Password");
        }
        var result = await _signInManager.CheckPasswordSignInAsync(user, loginVm.Password, false);
        if (!result.Succeeded)
        {
            return new MethodResult<JwtTokenResponseVM>.Failure("Invalid Email Or Password");
        }
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email??""),
            new(ClaimTypes.Role, string.Join(',',roles)),
            new(ClaimTypes.Name, user.UserName ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id),
        };
            
        return await CreateTokenAsync(claims, user);
    }

    public async Task<MethodResult<RegisterVM>> RegisterAsync(RegisterVM registerVm)
    {
        var user = new AppUser
        {
            Email = registerVm.Email,
            UserName = registerVm.Email,
            FullName = registerVm.FullName,
            EmailConfirmed = true,
            LockoutEnabled = false
        };
        var result = await _userManager.CreateAsync(user, registerVm.Password);
        if (!result.Succeeded)
        {
            return new MethodResult<RegisterVM>.Failure("Failed To Register");
        }
        var resultRoleCreation = await _userManager.AddToRoleAsync(user, ApplicationConstants.RolesTypes.User);
        if (!resultRoleCreation.Succeeded)
        {
            return new MethodResult<RegisterVM>.Failure("Failed To Register");
        }
        return new MethodResult<RegisterVM>.Success(registerVm);
    }

    public async Task<MethodResult<JwtTokenResponseVM>> RefreshTokenAsync(string accessToken, string refreshToken) =>
        await GetPrincipalFromExpiredToken(accessToken).Bind(
            r => GetToken(refreshToken, r)
        );

    private async Task<MethodResult<JwtTokenResponseVM>> GetToken(string refreshToken, ClaimsPrincipal result)
    {
        var userId = result.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return new MethodResult<JwtTokenResponseVM>.Failure("Invalid Access Token");
        }
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.RefreshToken != refreshToken)
        {
            return new MethodResult<JwtTokenResponseVM>.Failure("Invalid Refresh Token");
        }
        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return new MethodResult<JwtTokenResponseVM>.Failure("Refresh Token Expired");
        }
        var claims = result.Claims;
        return await CreateTokenAsync(claims, user);
    }

    private async Task<MethodResult<JwtTokenResponseVM>> CreateTokenAsync(IEnumerable<Claim> claims, AppUser user)
    {
        var newAccessToken = GenerateAccessToken(claims);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtConfig.RefreshTokenValidityDays);
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return new MethodResult<JwtTokenResponseVM>.Failure("Failed To Update Refresh Token");
        }

        var refreshTokenExpiryTimeStamp = DateTime.UtcNow.AddDays(_jwtConfig.RefreshTokenValidityDays);
        var accessTokenExpiryTimeStamp = DateTime.UtcNow.AddMinutes(_jwtConfig.AccessTokenValidityMin);

        return new MethodResult<JwtTokenResponseVM>.Success(new JwtTokenResponseVM
        {
            AccessToken = newAccessToken,
            AccessTokenExpiresIn = (int)accessTokenExpiryTimeStamp.Subtract(DateTime.UtcNow).TotalSeconds,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresIn = (int)refreshTokenExpiryTimeStamp.Subtract(DateTime.UtcNow).TotalSeconds
        });
    }
    private MethodResult<ClaimsPrincipal> GetPrincipalFromExpiredToken(string accessToken)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _jwtConfig.Audience,
            ValidateIssuer = true,
            ValidIssuer = _jwtConfig.Issuer,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKey = _key,
            ValidateLifetime = false //here we are saying that we don't care about the token's expiration date
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken)
        {
            return new MethodResult<ClaimsPrincipal>.Failure("Invalid Token");
        }

        if (!jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase))
            return new MethodResult<ClaimsPrincipal>.Failure("Invalid Token");
        return new MethodResult<ClaimsPrincipal>.Success(principal);
    }
    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
    private string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);
        var tokenExpiryTimeStamp = DateTime.UtcNow.AddMinutes(_jwtConfig.AccessTokenValidityMin);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = tokenExpiryTimeStamp,
            SigningCredentials = creds,
            Issuer = _jwtConfig.Issuer,
            Audience = _jwtConfig.Audience
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);
        return accessToken;
    }
}