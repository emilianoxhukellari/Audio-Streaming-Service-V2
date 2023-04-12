using DataAccess.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerWeb.Services;

namespace ServerWeb.Extensions
{
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// 1) Creates all roles if they do not already exist. 2) Creates a specific super user that is set 
        /// in configuration if they do not already exist. 3) Set the role of the specific super user to "SuperUser" if it was 
        /// not already "SuperUser".
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static async Task<WebApplication> Initialize(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var authDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var roles = app.Configuration.GetSection("Roles").Get<List<string>>();

            if (roleManager != null && roles != null)
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

            if (!authDbContext.Users.Any(user => user.Email == suData[0]))
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

        /// <summary>
        /// Set the temporary data of the indicator that is displayed temporarily when an action occurred.
        /// </summary>
        /// <param name="pageModel"></param>
        /// <param name="status"></param>
        /// <param name="message"></param>
        public static void SetTempData(this PageModel pageModel, Status status, string message)
        {
            pageModel.TempData["Display"] = "block";
            pageModel.TempData["Message"] = message;
            if (status == Status.Success)
            {
                pageModel.TempData["Status"] = "success";
            }
            else if (status == Status.Fail)
            {
                pageModel.TempData["Status"] = "fail";
            }
        }

        public enum Status
        {
            Success,
            Fail
        }
    }
}
