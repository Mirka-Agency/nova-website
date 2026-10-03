using CMS.Modules.Blog.Web.Areas.Admin.Controllers;
using CMS.Modules.Forms.Web.Areas.Admin.Controllers;
using CMS.Modules.Media.Web.Areas.Admin.Controllers;
using CMS.Modules.Shop.Web.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CMS.Web.Integration.Tests;

public sealed class CmsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string? _connectionString;

    public CmsWebApplicationFactory(string? connectionString = null)
    {
        _connectionString = connectionString ?? TestConnectionString.Resolve();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (!string.IsNullOrWhiteSpace(_connectionString))
            builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);

        builder.UseSetting("Seed:Admin:Email", "admin@local.test");
        builder.UseSetting("Seed:Admin:Password", "Admin123!");
        builder.UseSetting("Seed:Admin:FullName", "مدیر سیستم");
        builder.UseSetting("FeatureManagement:Blog", "true");
        builder.UseSetting("FeatureManagement:Shop", "true");
        builder.UseSetting("FeatureManagement:Forms", "true");

        builder.ConfigureServices(services =>
        {
            // Module RCL views compile into the module assembly, but RelatedAssembly "*.Views"
            // is missing at runtime under WebApplicationFactory — register compiled parts explicitly.
            services.AddControllersWithViews()
                .ConfigureApplicationPartManager(parts =>
                {
                    EnsureCompiledRazorPart(parts, typeof(ShopController).Assembly);
                    EnsureCompiledRazorPart(parts, typeof(PostsController).Assembly);
                    EnsureCompiledRazorPart(parts, typeof(FormsController).Assembly);
                    EnsureCompiledRazorPart(parts, typeof(MediaController).Assembly);
                });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            var values = new Dictionary<string, string?>
            {
                ["Seed:Admin:Email"] = "admin@local.test",
                ["Seed:Admin:Password"] = "Admin123!",
                ["Seed:Admin:FullName"] = "مدیر سیستم",
                ["FeatureManagement:Blog"] = "true",
                ["FeatureManagement:Shop"] = "true",
                ["FeatureManagement:Forms"] = "true"
            };

            if (!string.IsNullOrWhiteSpace(_connectionString))
                values["ConnectionStrings:DefaultConnection"] = _connectionString;

            config.AddInMemoryCollection(values);
        });

        return base.CreateHost(builder);
    }

    private static void EnsureCompiledRazorPart(ApplicationPartManager parts, System.Reflection.Assembly assembly)
    {
        if (parts.ApplicationParts.OfType<CompiledRazorAssemblyPart>().Any(p => p.Assembly == assembly))
            return;

        parts.ApplicationParts.Add(new CompiledRazorAssemblyPart(assembly));
    }
}
