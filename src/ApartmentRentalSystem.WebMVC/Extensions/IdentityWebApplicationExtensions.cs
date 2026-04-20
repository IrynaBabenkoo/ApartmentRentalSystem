using ApartmentRentalSystem.WebMVC.Constants;
using ApartmentRentalSystem.WebMVC.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace ApartmentRentalSystem.WebMVC.Extensions;

public static class IdentityWebApplicationExtensions
{
    private record UserInfo(string Username, string Password);

    public static async Task InitializeRolesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    public static async Task InitializeDefaultUsersAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var defaultUsers = new List<UserInfo>();
        app.Configuration.GetSection("IdentityDefaults:DefaultUsers").Bind(defaultUsers);

        foreach (var user in defaultUsers)
        {
            string role = user.Username.Contains("owner", StringComparison.OrdinalIgnoreCase)
                          ? RoleNames.Owner
                          : RoleNames.Tenant;

            await AddUserWithRole(userManager, user, role);
        }
    }

    private static async Task AddUserWithRole(UserManager<ApplicationUser> userManager, UserInfo info, string role)
    {
        if (await userManager.FindByEmailAsync(info.Username) == null)
        {
            var user = new ApplicationUser
            {
                UserName = info.Username,
                Email = info.Username,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, info.Password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
