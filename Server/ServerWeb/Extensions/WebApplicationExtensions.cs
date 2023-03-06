using DataAccess.Contexts;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using ServerWeb.Services;
using System.Data;
using System.Diagnostics;

namespace ServerWeb.Extensions
{
    public static class WebApplicationExtensions
    {
        public static async Task<WebApplication> Initialize(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var authDbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var roles = app.Configuration.GetSection("Roles").Get<List<string>>();

            if(roleManager != null && roles != null) 
            {
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }
            }

            List<string> suData = app.Configuration.GetSection("SU").Get<List<string>>()!;
           
            if(!authDbContext.Users.Any(user => user.Email == suData[0]))
            {
                
                var user = new IdentityUser()
                {
                    UserName = suData[0],
                    Email = suData[0]
                };

                await userManager.CreateAsync(user, suData[1]);
                await userManager.AddToRoleAsync(user, "SU");
                return app;
            }
            else
            {
                var userControlService = scope.ServiceProvider.GetRequiredService<IUserControlService>(); 
                await userControlService.ChangeUserRoleToAsync(suData[0], "SU");
                return app;
            }
        }
    }
}
