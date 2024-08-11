namespace IdentitySetupWithJwt.Configurations
{
    public class JwtConfig
    {
        public string SecretKey { get; init; }
        public string Issuer { get; init; }
        public string Audience { get; init; }
        public int AccessTokenValidityMin { get; init; }
        public int RefreshTokenValidityDays { get; init; }
    }
}
