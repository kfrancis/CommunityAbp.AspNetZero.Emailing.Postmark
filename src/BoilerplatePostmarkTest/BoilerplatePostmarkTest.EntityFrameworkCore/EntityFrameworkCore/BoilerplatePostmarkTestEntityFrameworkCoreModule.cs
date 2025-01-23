using Abp.EntityFrameworkCore;
using Abp.Modules;
using Abp.Reflection.Extensions;

namespace BoilerplatePostmarkTest.EntityFrameworkCore
{
    [DependsOn(
        typeof(BoilerplatePostmarkTestCoreModule), 
        typeof(AbpEntityFrameworkCoreModule))]
    public class BoilerplatePostmarkTestEntityFrameworkCoreModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(BoilerplatePostmarkTestEntityFrameworkCoreModule).GetAssembly());
        }
    }
}