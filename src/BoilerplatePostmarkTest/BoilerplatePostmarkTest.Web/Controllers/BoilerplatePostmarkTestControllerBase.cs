using Abp.AspNetCore.Mvc.Controllers;

namespace BoilerplatePostmarkTest.Web.Controllers
{
    public abstract class BoilerplatePostmarkTestControllerBase: AbpController
    {
        protected BoilerplatePostmarkTestControllerBase()
        {
            LocalizationSourceName = BoilerplatePostmarkTestConsts.LocalizationSourceName;
        }
    }
}