using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using ServerWeb.Pages.SU.Components;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Timers;

namespace ServerWeb.Services
{
    public class RegistrationCountService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        public event EventHandler<int> UserCountChanged;
        public RegistrationCountService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using(var scope = _serviceProvider.CreateScope())
                {
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                    var superUsers = await userManager.GetUsersInRoleAsync("SU");
                    var administrators = await userManager.GetUsersInRoleAsync("Administrator");
                    var engineAdministrators = await userManager.GetUsersInRoleAsync("EngineAdministrator");
                    var users = await userManager.GetUsersInRoleAsync("User");
                    NumberOfSuperUsers = superUsers.Count;
                    NumberOfAdministrators = administrators.Count;
                    NumberOfEngineAdministrators = engineAdministrators.Count;
                    NumberOfUsers = users.Count;

                    UserCountChanged?.Invoke(this, NumberOfUsers);

                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }
        }

      
        public int NumberOfSuperUsers { get; set; }
        public int NumberOfAdministrators { get; set; }
        public int NumberOfEngineAdministrators { get; set; }
        public int NumberOfUsers { get; set; }
    }
}
