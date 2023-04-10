using DataAccess.Contexts;
using ServerWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServerWeb.Extensions;
using Microsoft.AspNetCore.Http.Features;
using DataAccess.Services;
using AudioEngine.Services;

namespace ServerWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

            builder.Services.AddRazorPages();

            builder.Services.AddServerSideBlazor();

            builder.Services.AddDbContextPool<UserDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("UserConnectionString"));
            });

            builder.Services.AddDbContextPool<StreamingDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("StreamingConnectionString"));
            });

            builder.Services.AddDbContextFactory<StreamingDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("StreamingConnectionString"));
            });

            builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<UserDbContext>();

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

            builder.Services.AddScoped<IIndexRedirection, IndexRedirection>();

            builder.Services.AddScoped<IAudioStoringService, AudioStoringService>();

            builder.Services.AddScoped<PlaylistManagerService>();

            builder.Services.AddScoped<SongManagerService>();

            builder.Services.AddScoped<IssueManagerService>();

            builder.Services.AddSingleton<IAudioEngineService, AudioEngineService>();

            builder.Services.AddScoped<IRegistrationService, RegistrationService>();


            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "AspNetCore.Identity.Application";
                options.AccessDeniedPath = "/AccessDenied"; // Change this
            });

            var app = builder.Build();

            Task.Run(() => app.Initialize().Wait());

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();

            app.UseAuthentication();

            app.UseAuthorization();

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