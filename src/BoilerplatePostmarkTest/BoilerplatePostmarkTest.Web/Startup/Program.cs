using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace BoilerplatePostmarkTest.Web.Startup
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            await host.RunAsync();
        }
    }
}
