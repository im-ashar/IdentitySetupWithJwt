using IdentitySetupWithJwt.Services;

namespace IdentitySetupWithJwt.Extensions
{
    public static class AddServicesExtension
    {
        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<ISeedDataService, SeedDataService>();
            services.AddScoped<IAccountService, AccountService>();
        }
    }
}
