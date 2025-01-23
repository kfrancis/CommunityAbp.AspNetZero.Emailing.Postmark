using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Modules;
using Abp.TestBase;
using Castle.MicroKernel.Registration;
using NSubstitute;

namespace CommunityAbp.AspNetZero.Emailing.Postmark.Tests
{
    public abstract class AbpPostmarkTestBase : AbpIntegratedTestBase<AbpPostmarkTestModule>
    {

        //protected override void PostInitialize()
        //{
        //    base.PostInitialize();

        //    // Register your mock in the container:
        //    LocalIocManager.IocContainer.Register(
        //        Component.For<IAbpPostmarkConfiguration>()
        //            .UsingFactoryMethod(() =>
        //            {
        //                // Create a mock
        //                var mockConfig = Substitute.For<IAbpPostmarkConfiguration>();
        //                // Setup defaults if needed
        //                mockConfig.ApiKey.Returns("test-api-key");
        //                mockConfig.DefaultFromAddress.Returns("somefrom@address.com");
        //                mockConfig.TrackOpens.Returns(true);
        //                return mockConfig;
        //            })
        //            .LifestyleSingleton()
        //    );
        //}
    }

    [DependsOn(typeof(AbpPostmarkModule), typeof(AbpTestBaseModule))]
    public class AbpPostmarkTestModule : AbpModule
    {
        public AbpPostmarkTestModule()
        {

        }
        public override void PostInitialize()
        {
            // Now it's safe to resolve and set up your configuration
            var abpConfiguration = IocManager.Resolve<IAbpStartupConfiguration>();
            var postmarkConfig = abpConfiguration.Get<IAbpPostmarkConfiguration>();
            postmarkConfig.ApiKey = "test-api-key";
        }

        public override void PreInitialize()
        {
            Configuration.Modules.AbpPostmark().ApiKey = "some-key";
            Configuration.Modules.AbpPostmark().DefaultFromAddress = "some.email@address.com";
            Configuration.Modules.AbpPostmark().TrackOpens = false;
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AbpPostmarkTestModule).Assembly);
        }
    }
}
