using IdentitySetupWithJwt.Services.Implementations;
using IdentitySetupWithJwt.Services.Interfaces;

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
