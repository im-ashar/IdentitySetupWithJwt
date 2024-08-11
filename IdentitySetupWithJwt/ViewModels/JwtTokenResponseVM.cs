namespace IdentitySetupWithJwt.ViewModels
{
    public class JwtTokenResponseVM
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int AccessTokenExpiresIn { get; set; }
        public int RefreshTokenExpiresIn { get; set; }
    }
}
