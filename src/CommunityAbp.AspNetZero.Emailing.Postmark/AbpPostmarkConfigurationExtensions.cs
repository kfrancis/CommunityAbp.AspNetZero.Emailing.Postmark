using Abp.Configuration.Startup;

namespace CommunityAbp.AspNetZero.Emailing.Postmark
{
    public static class AbpPostmarkConfigurationExtensions
    {
        public static IAbpPostmarkConfiguration AbpPostmark(this IModuleConfigurations configurations)
        {
            return configurations.AbpConfiguration.Get<IAbpPostmarkConfiguration>();
        }
    }
}
