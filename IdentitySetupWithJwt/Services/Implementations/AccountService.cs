﻿using IdentitySetupWithJwt.Configurations;
using IdentitySetupWithJwt.Models;
using IdentitySetupWithJwt.Services.Interfaces;
using IdentitySetupWithJwt.Utilities;
using IdentitySetupWithJwt.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IdentitySetupWithJwt.Services.Implementations
{
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

        public async Task<MethodResult<JwtTokenResponseVM>> LoginAsync(LoginVM loginVM)
        {
            var user = await _userManager.FindByEmailAsync(loginVM.Email);
            if (user == null)
            {
                return "Invalid Email Or Password";
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginVM.Password, false);
            if (!result.Succeeded)
            {
                return "Invalid Email Or Password";
            }
            var roles = (await _userManager.GetRolesAsync(user)).ToList();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email??""),
                new Claim(ClaimTypes.Role, string.Join(',',roles)),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.Id ?? ""),
            };
            var jwtTokenResponse = await CreateTokenAsync(claims, user);
            if (!jwtTokenResponse.IsSuccess)
            {
                return jwtTokenResponse.ErrorMessage;
            }
            return jwtTokenResponse.Data;

        }

        public async Task<MethodResult<RegisterVM>> RegisterAsync(RegisterVM registerVM)
        {
            var user = new AppUser
            {
                Email = registerVM.Email,
                UserName = registerVM.Email,
                FullName = registerVM.FullName,
                EmailConfirmed = true,
                LockoutEnabled = false
            };
            var result = await _userManager.CreateAsync(user, registerVM.Password);
            if (!result.Succeeded)
            {
                return "Failed To Register";
            }
            result = await _userManager.AddToRoleAsync(user, ApplicationConstants.RolesTypes.User);
            if (!result.Succeeded)
            {
                return "Failed To Register";
            }
            return registerVM;
        }

        public async Task<MethodResult<JwtTokenResponseVM>> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            var principalResult = GetPrincipalFromExpiredToken(accessToken);
            if (!principalResult.IsSuccess)
            {
                return principalResult.ErrorMessage;
            }
            var userId = principalResult.Data.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return "Invalid Access Token";
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.RefreshToken != refreshToken)
            {
                return "Invalid Refresh Token";
            }
            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return "Refresh Token Expired";
            }
            var claims = principalResult.Data.Claims;
            var result = await CreateTokenAsync(claims, user);
            if (!result.IsSuccess)
            {
                return result.ErrorMessage;
            }
            return result.Data;
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
                return "Failed To Update Refresh Token";
            }

            var refreshTokenExpiryTimeStamp = DateTime.UtcNow.AddDays(_jwtConfig.RefreshTokenValidityDays);
            var accessTokenExpiryTimeStamp = DateTime.UtcNow.AddMinutes(_jwtConfig.AccessTokenValidityMin);

            return new JwtTokenResponseVM
            {
                AccessToken = newAccessToken,
                AccessTokenExpiresIn = (int)accessTokenExpiryTimeStamp.Subtract(DateTime.UtcNow).TotalSeconds,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiresIn = (int)refreshTokenExpiryTimeStamp.Subtract(DateTime.UtcNow).TotalSeconds
            };
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
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null)
            {
                return "Invalid Token";
            }

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase))
                return "Invalid Token";
            return principal;
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
}