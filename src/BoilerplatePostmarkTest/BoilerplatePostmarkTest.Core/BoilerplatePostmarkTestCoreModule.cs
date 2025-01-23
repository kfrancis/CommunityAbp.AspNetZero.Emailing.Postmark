using Abp.Modules;
using Abp.Reflection.Extensions;
using BoilerplatePostmarkTest.Localization;

namespace BoilerplatePostmarkTest
{
    public class BoilerplatePostmarkTestCoreModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Auditing.IsEnabledForAnonymousUsers = true;

            BoilerplatePostmarkTestLocalizationConfigurer.Configure(Configuration.Localization);
            
            Configuration.Settings.SettingEncryptionConfiguration.DefaultPassPhrase = BoilerplatePostmarkTestConsts.DefaultPassPhrase;
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(BoilerplatePostmarkTestCoreModule).GetAssembly());
        }
    }
}