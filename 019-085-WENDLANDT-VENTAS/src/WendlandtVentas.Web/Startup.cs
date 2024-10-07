using Autofac;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Syncfusion.Licensing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using WendlandtVentas.Core;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models;
using WendlandtVentas.Core.Services;
using WendlandtVentas.Infrastructure.Data;
using WendlandtVentas.Infrastructure.Identity;
using WendlandtVentas.Infrastructure.Services;
using WendlandtVentas.Infrastructure.Repositories;
using WendlandtVentas.Web.Libs; 

namespace WendlandtVentas.Web
{
    public class Startup
    {
        public Startup(IConfiguration config)
        {
            Configuration = config;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureDevelopmentServices(IServiceCollection services)
        {
            // use in-memory database
            //ConfigureInMemoryDatabases(services);

            // use real database
            ConfigureProductionServices(services);
        }

        private void ConfigureInMemoryDatabases(IServiceCollection services)
        {
            // use in-memory database
            services.AddDbContext<AppDbContext>(c =>
                c.UseInMemoryDatabase("Catalog"));

            // Add Identity DbContext
            services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseInMemoryDatabase("Identity"));

            ConfigureServices(services);
        }

        public void ConfigureProductionServices(IServiceCollection services)
        {
            // use real database
            // Requires LocalDB which can be installed with SQL Server Express 2016
            // https://www.microsoft.com/en-us/download/details.aspx?id=54284
            services.AddDbContext<AppDbContext>(c =>
                c.UseSqlServer(Configuration.GetConnectionString("CatalogConnection")));

            // Add Identity DbContext
            services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("IdentityConnection")));

                

            ConfigureServices(services);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages().AddRazorRuntimeCompilation();

            ConfigureCookieSettings(services);

            services.AddAuthentication()
                .AddCookie(cfg => { cfg.SlidingExpiration = true; })
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = true;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = Configuration["Tokens:Issuer"],
                        ValidAudience = Configuration["Tokens:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:Key"]))
                    };
                });

            CreateIdentityIfNotCreated(services); 
            services.Configure<BrandSettings>(Configuration.GetSection("Brand"));
            services.Configure<EmailSettings>(Configuration.GetSection("Email"));
            services.Configure<IdentityServerSettings>(Configuration.GetSection("IdentityServer"));
            services.Configure<LogBookServerSettings>(Configuration.GetSection("LogBookServer"));
            services.Configure<OneSignalConfiguration>(Configuration.GetSection("OneSignalConfiguration"));
            services.Configure<TreasuryServerSettings>(Configuration.GetSection("TreasuryServer"));
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IBitacoraRepository, BitacoraRepository>();
            services.AddScoped<IBitacoraService, BitacoraService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddTransient<ITreasuryApi, TreasuryApi>();

            SyncfusionLicenseProvider.RegisterLicense(
                "MzM1MTk0QDMxMzgyZTMzMmUzMGo4MzR5cUI3Mit5cnlzamROck1RL0dEQXFobGNNY1BMNndPTC9hbHoxL3c9;MzM1MTk1QDMxMzgyZTMzMmUzMEcrS2Z4T2FtcGtzY25FQngvd3ZEMFdUT3o1QmxlbTZqOEdZMXZ2OG9sa0U9;MzM1MTk2QDMxMzgyZTMzMmUzMExsellHQ3VEMTFNTmVtdE5xTkk1c1M3ZFgvLyt3eFY4anpkMEVXTVdLbFk9;MzM1MTk3QDMxMzgyZTMzMmUzMFkrSDZDSDlrSjhlajZ4YXc4anNVVWIvNGt0bkdVS2w4U1hoWm4yTkJkczA9;MzM1MTk4QDMxMzgyZTMzMmUzMEZMd3U5cWZjSTh5emNiRDhBc3BqVmZiQlNUSjRZYk9nUXd1OUk3c001dFE9");

            services.Configure<RequestLocalizationOptions>(
                options =>
                {
                    var supportedCultures = new List<CultureInfo>
                    {
                        new CultureInfo("en-us"),
                        new CultureInfo("es-mx")
                    };

                    options.DefaultRequestCulture = new RequestCulture("es-mx", "es-mx");
                    options.SupportedCultures = supportedCultures;
                    options.SupportedUICultures = supportedCultures;

                    //options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
                });

            services.AddTransient<ILogBookService,LogBookService>();
            services.AddTransient<IUserResolverService, UserResolverService>();
            services.AddScoped<SfGridOperations>();
           

            services.AddMemoryCache();

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddHttpContextAccessor();
            //services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new Info {Title = "My API", Version = "v1"}); });

            //return ContainerSetup.InitializeWeb(Assembly.GetExecutingAssembly(), services);
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // Add any Autofac modules or registrations.
            // This is called AFTER ConfigureServices so things you
            // register here OVERRIDE things registered in ConfigureServices.
            //
            // You must have the call to AddAutofac in the Program.Main
            // method or this won't be called.
            builder.RegisterModule(new AutofacModule());
        }

        private static void ConfigureCookieSettings(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.Cookie = new CookieBuilder
                {
                    IsEssential =
                        true // required for auth to work without explicit user consent; adjust to suit your privacy policy
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, LinkGenerator linkGenerator)
        {
            if (env.EnvironmentName == "Development")
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                //app.UseBrowserLink();
            }
            else
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                //app.UseDeveloperExceptionPage();
                //app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseRouting();

            app.UseHttpsRedirection();
            app.UseCookiePolicy();
            app.UseStaticFiles();

            app.UseRequestLocalization();

            app.UseAuthentication();
            app.UseAuthorization();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            //app.UseSwagger();

            //// Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            //app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"); });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute(
                    name: "Order",
                    pattern: "{controller=Order}/{action=Index}" );
                endpoints.MapDefaultControllerRoute();
            });
        }

        private static void CreateIdentityIfNotCreated(IServiceCollection services)
        {
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var existingUserManager = scope.ServiceProvider
                    .GetService<UserManager<ApplicationUser>>();
                if (existingUserManager == null)
                    services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
                        {
                            options.Stores.MaxLengthForKeys = 128;
                            options.Password.RequireDigit = false;
                            options.Password.RequireLowercase = false;
                            options.Password.RequireUppercase = false;
                            options.Password.RequireNonAlphanumeric = false;
                            options.Password.RequiredLength = 6;
                        })
                        .AddEntityFrameworkStores<AppIdentityDbContext>()
                        .AddDefaultTokenProviders();
            }
        }
    }
}