using Abp.AspNetCore.Mvc.Views;

namespace BoilerplatePostmarkTest.Web.Views
{
    public abstract class BoilerplatePostmarkTestRazorPage<TModel> : AbpRazorPage<TModel>
    {
        protected BoilerplatePostmarkTestRazorPage()
        {
            LocalizationSourceName = BoilerplatePostmarkTestConsts.LocalizationSourceName;
        }
    }
}
