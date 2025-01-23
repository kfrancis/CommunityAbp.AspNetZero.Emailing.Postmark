using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Configuration.Startup;
using CommunityAbp.AspNetZero.Emailing.Postmark;

namespace BoilerplatePostmarkTest
{
    [DependsOn(
        typeof(BoilerplatePostmarkTestCoreModule), 
        typeof(AbpAutoMapperModule),
        typeof(AbpPostmarkModule))]
    public class BoilerplatePostmarkTestApplicationModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(BoilerplatePostmarkTestApplicationModule).GetAssembly());
        }
    }
}
