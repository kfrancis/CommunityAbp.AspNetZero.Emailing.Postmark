using Abp.Application.Services;

namespace BoilerplatePostmarkTest
{
    /// <summary>
    /// Derive your application services from this class.
    /// </summary>
    public abstract class BoilerplatePostmarkTestAppServiceBase : ApplicationService
    {
        protected BoilerplatePostmarkTestAppServiceBase()
        {
            LocalizationSourceName = BoilerplatePostmarkTestConsts.LocalizationSourceName;
        }
    }
}