using BoilerplatePostmarkTest.Configuration;
using BoilerplatePostmarkTest.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BoilerplatePostmarkTest.EntityFrameworkCore
{
    /* This class is needed to run EF Core PMC commands. Not used anywhere else */
    public class BoilerplatePostmarkTestDbContextFactory : IDesignTimeDbContextFactory<BoilerplatePostmarkTestDbContext>
    {
        public BoilerplatePostmarkTestDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<BoilerplatePostmarkTestDbContext>();
            var configuration = AppConfigurations.Get(WebContentDirectoryFinder.CalculateContentRootFolder());

            DbContextOptionsConfigurer.Configure(
                builder,
                configuration.GetConnectionString(BoilerplatePostmarkTestConsts.ConnectionStringName)
            );

            return new BoilerplatePostmarkTestDbContext(builder.Options);
        }
    }
}