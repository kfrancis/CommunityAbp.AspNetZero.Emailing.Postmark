using System.Collections.Generic;
using Abp.Configuration;
using BoilerplatePostmarkTest.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace BoilerplatePostmarkTest.Web.Startup
{
    public class MySettingProvider : SettingProvider
    {
        private readonly IConfigurationRoot _appConfiguration;

        public MySettingProvider(IWebHostEnvironment env)
        {
            _appConfiguration = AppConfigurations.Get(env.ContentRootPath, env.EnvironmentName);
        }
        public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
        {
            return new[]
            {
                new SettingDefinition(
                    "Abp.Net.Mail.Smtp.Host",
                    _appConfiguration.GetValue<string>("Settings:Abp.Net.Mail.Smtp.Host", string.Empty)
                ),
                new SettingDefinition(
                    "Abp.Net.Mail.Smtp.Port",
                    _appConfiguration.GetValue<string>("Settings:Abp.Net.Mail.Smtp.Port", "25")
                ),
                new SettingDefinition(
                    "Abp.Net.Mail.Smtp.UserName",
                    _appConfiguration.GetValue<string>("Settings:Abp.Net.Mail.Smtp.UserName", string.Empty)
                ),
                new SettingDefinition(
                    "Abp.Net.Mail.Smtp.Password",
                    _appConfiguration.GetValue<string>("Settings:Abp.Net.Mail.Smtp.Password", string.Empty)
                ),
                new SettingDefinition(
                    "Abp.Net.Mail.Smtp.Domain",
                    _appConfiguration.GetValue<string>("Settings:Abp.Net.Mail.Smtp.Domain", string.Empty)
                ),
                new SettingDefinition(
                    "Abp.Net.Mail.Smtp.EnableSsl",
                    _appConfiguration.GetValue<string>("Settings:Abp.Net.Mail.Smtp.EnableSsl", "false")
                ),
                new SettingDefinition(
                    "Abp.Net.Mail.Smtp.UseDefaultCredentials",
                    _appConfiguration.GetValue<string>("Settings:Abp.Net.Mail.Smtp.UseDefaultCredentials", "true")
                ),
                new SettingDefinition(
                    "Abp.Net.Mail.DefaultFromAddress",
                    _appConfiguration.GetValue<string>("Settings:Abp.Net.Mail.DefaultFromAddress", string.Empty)
                ),
                new SettingDefinition(
                    "Abp.Net.Mail.DefaultFromDisplayName",
                    _appConfiguration.GetValue<string>("Settings:Abp.Net.Mail.DefaultFromDisplayName", string.Empty)
                )
            };
        }
    }
}
