using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Infrastructure.Identity
{
    public class AppIdentityDbContextSeed
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            var roles = Enum.GetNames(typeof(Role));

            foreach (var role in roles)
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }

            var defaultUser = new ApplicationUser
            { UserName = "contacto@monobits.mx", Email = "contacto@monobits.mx", EmailConfirmed = true, Name = "Monobits", IsActive = true };
            await userManager.CreateAsync(defaultUser, "1Z2x3,.");
            await userManager.AddToRoleAsync(defaultUser, Role.Administrator.ToString());
        }
    }
}