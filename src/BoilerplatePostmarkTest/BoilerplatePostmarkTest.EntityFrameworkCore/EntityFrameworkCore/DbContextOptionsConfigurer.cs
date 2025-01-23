using Microsoft.EntityFrameworkCore;

namespace BoilerplatePostmarkTest.EntityFrameworkCore
{
    public static class DbContextOptionsConfigurer
    {
        public static void Configure(
            DbContextOptionsBuilder<BoilerplatePostmarkTestDbContext> dbContextOptions, 
            string connectionString
            )
        {
            /* This is the single point to configure DbContextOptions for BoilerplatePostmarkTestDbContext */
            dbContextOptions.UseSqlServer(connectionString);
        }
    }
}
