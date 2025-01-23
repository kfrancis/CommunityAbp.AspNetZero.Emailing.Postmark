using System.Threading.Tasks;
using Abp;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Modules;
using Abp.Net.Mail;
using Abp.Reflection.Extensions;

namespace CommunityAbp.AspNetZero.Emailing.Postmark;

/// <summary>
/// Module for Postmark email sending integration.
/// </summary>
[DependsOn(typeof(AbpKernelModule))]
public class AbpPostmarkModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(AbpPostmarkModule).GetAssembly());
    }

    public override void PreInitialize()
    {
        IocManager.Register<IAbpPostmarkConfiguration, AbpPostmarkConfiguration>();
        Configuration.ReplaceService<IEmailSender, PostmarkEmailSender>(DependencyLifeStyle.Transient);
    }
}
