using IdentitySetupWithJwt.Data;
using IdentitySetupWithJwt.Models;
using IdentitySetupWithJwt.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentitySetupWithJwt.Services;

public interface ISeedDataService
{
    Task SeedDataAsync();
}

public class SeedDataService : ISeedDataService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public SeedDataService(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IDbContextFactory<AppDbContext> contextFactory)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _contextFactory = contextFactory;
    }
    //This method is used to seed the data in the database
    public async Task SeedDataAsync()
    {
        await MigrateDatabase();
        await SeedRolesIfNotExists();
        await SeedAdminUserIfNotExists();
    }

    private async Task MigrateDatabase()
    {
        using var context = _contextFactory.CreateDbContext();
        await context.Database.MigrateAsync();
    }
    private async Task SeedAdminUserIfNotExists()
    {
        if (await _userManager.FindByEmailAsync(ApplicationConstants.AdminAccount.Email) == null)
        {
            // if it doesn't exist, create it
            var user = new AppUser
            {
                FullName = ApplicationConstants.AdminAccount.FullName,
                UserName = ApplicationConstants.AdminAccount.UserName,
                Email = ApplicationConstants.AdminAccount.Email,
                EmailConfirmed = true,
                LockoutEnabled = false,
            };
            var result = await _userManager.CreateAsync(user, ApplicationConstants.AdminAccount.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                throw new Exception(string.Join(Environment.NewLine, errors));
            }
            //Add Admin user to AdminRole
            result = await _userManager.AddToRoleAsync(user, ApplicationConstants.RolesTypes.Admin);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                throw new Exception(string.Join(Environment.NewLine, errors));
            }
        }
    }
    private async Task SeedRolesIfNotExists()
    {
        if (!await _roleManager.RoleExistsAsync(ApplicationConstants.RolesTypes.Admin))
        {
            var roles = typeof(ApplicationConstants.RolesTypes).GetFields();
            foreach (var role in roles)
            {
                // if it doesn't exist, create it
                var result = await _roleManager.CreateAsync(new IdentityRole(role.Name));
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description);
                    throw new Exception(string.Join(Environment.NewLine, errors));
                }
            }
        }
    }
}