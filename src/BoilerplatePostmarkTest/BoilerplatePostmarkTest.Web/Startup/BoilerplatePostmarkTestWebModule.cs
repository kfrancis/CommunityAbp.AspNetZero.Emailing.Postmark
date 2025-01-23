using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using BoilerplatePostmarkTest.Configuration;
using BoilerplatePostmarkTest.EntityFrameworkCore;
using CommunityAbp.AspNetZero.Emailing.Postmark;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;

namespace BoilerplatePostmarkTest.Web.Startup
{
    [DependsOn(
        typeof(BoilerplatePostmarkTestApplicationModule), 
        typeof(BoilerplatePostmarkTestEntityFrameworkCoreModule), 
        typeof(AbpAspNetCoreModule),
        typeof(AbpPostmarkModule))]
    public class BoilerplatePostmarkTestWebModule : AbpModule
    {
        private readonly IConfigurationRoot _appConfiguration;

        public BoilerplatePostmarkTestWebModule(IWebHostEnvironment env)
        {
            _appConfiguration = AppConfigurations.Get(env.ContentRootPath, env.EnvironmentName);
        }

        public override void PreInitialize()
        {
            Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(BoilerplatePostmarkTestConsts.ConnectionStringName);

            Configuration.Navigation.Providers.Add<BoilerplatePostmarkTestNavigationProvider>();

            Configuration.Modules.AbpPostmark().ApiKey = _appConfiguration["Postmark:ApiKey"];
            Configuration.Modules.AbpPostmark().DefaultFromAddress = _appConfiguration["Postmark:DefaultFromAddress"];
            Configuration.Modules.AbpPostmark().TrackOpens = _appConfiguration.GetValue<bool?>("Postmark:TrackOpens");

            Configuration.Settings.Providers.Add<MySettingProvider>();

            Configuration.Modules.AbpAspNetCore()
                .CreateControllersForAppServices(
                    typeof(BoilerplatePostmarkTestApplicationModule).GetAssembly()
                );
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(BoilerplatePostmarkTestWebModule).GetAssembly());
        }

        public override void PostInitialize()
        {
            IocManager.Resolve<ApplicationPartManager>()
                .AddApplicationPartsIfNotAddedBefore(typeof(BoilerplatePostmarkTestWebModule).Assembly);
        }
    }
}
