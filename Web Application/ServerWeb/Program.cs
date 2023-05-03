using AudioEngine.Services;
using DataAccess.Contexts;
using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServerWeb.Extensions;
using ServerWeb.Services;

namespace ServerWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            builder.Services.AddRazorPages();

            builder.Services.AddServerSideBlazor();

            builder.Services.AddDbContextPool<IdentityDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("UserConnectionString"), 
                    b => b.MigrationsAssembly("DataAccess"));
            });

            builder.Services.AddDbContextPool<StreamingDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("StreamingConnectionString"),
                    b => b.MigrationsAssembly("DataAccess"));
            });

            builder.Services.AddDbContextFactory<StreamingDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("StreamingConnectionString"));
            });

            builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<IdentityDbContext>();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdministratorRole", policy => policy.RequireRole("Administrator"));
                options.AddPolicy("RequireEngineAdministratorRole", policy => policy.RequireRole("EngineAdministrator"));
                options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));
                options.AddPolicy("RequireSURole", policy => policy.RequireRole("SU"));
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(); // Lock pages 
            });

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 3000000000;
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartHeadersLengthLimit = int.MaxValue;
            });

            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 3000000000;
            });

            builder.Services.AddScoped<IDataAccessConfigurationService, DataAccessConfigurationService>();

            builder.Services.AddSingleton<IAudioEngineConfigurationService, AudioEngineConfigurationService>();

            builder.Services.AddScoped<IUserControlService, UserControlService>();

            builder.Services.AddScoped<IIndexRedirectionService, IndexRedirectionService>();

            builder.Services.AddScoped<IAudioStoringService, AudioStoringService>();

            builder.Services.AddScoped<IPlaylistManagerService, PlaylistManagerService>();

            builder.Services.AddScoped<ISongManagerService, SongManagerService>();

            builder.Services.AddScoped<IIssueManagerService, IssueManagerService>();

            builder.Services.AddSingleton<IAudioEngineService, AudioEngineService>();

            builder.Services.AddScoped<IAccountService, AccountService>();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "AspNetCore.Identity.Application";
                options.AccessDeniedPath = "/AccessDenied"; 
            });

            var app = builder.Build();

            Task.Run(() => app.Initialize().Wait());

            // Middleware

            app.UseExceptionHandler("/Error");

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapBlazorHub();

            app.MapRazorPages();

            app.MapFallback(context =>
            {
                context.Response.Redirect("/PageNotFound");
                return Task.CompletedTask;
            });

            app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");

            app.Run();
        }
    }
}